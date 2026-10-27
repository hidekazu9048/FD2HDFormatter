using System.Diagnostics;

namespace FD2HDFormatter.Core;

public sealed record BadSector(int Lba, int Cylinder, int Head, int R, string Error);

/// <summary>Done は済んだ工程数。工程は全トラック＋最後のシステム領域の書き込み。</summary>
public sealed record FormatProgress(int Done, int Total, string Message);

public sealed class FormatResult
{
    public required DiskFormat Format { get; init; }
    public required string Label { get; init; }
    public required uint Serial { get; init; }
    public List<BadSector> BadSectors { get; } = [];
    public int BadClusters { get; set; }
    /// <summary>システム領域が壊れているなどでディスクとして使えない理由。null なら成功。</summary>
    public string? Failure { get; set; }
    public TimeSpan Elapsed { get; set; }

    public long ClusterSize => Format.SectorSize;
    public long TotalBytes => (long)Format.Clusters * ClusterSize;
    public long BadBytes => BadClusters * ClusterSize;
    public long AvailableBytes => TotalBytes - BadBytes;
}

/// <summary>トラックごとに フォーマット → 読み取り検査 を行い、不良セクタを FAT に登録してシステム領域を書く。</summary>
public static class Formatter
{
    public const int MaxAttempts = 3;

    /// <summary>
    /// 媒体の傷による失敗は再試行し、それでも駄目なセクタは不良として記録して続ける。
    /// システム領域に不良が出たら、その時点で FormatResult.Failure を入れて返す。
    /// ディスク未挿入・ライトプロテクト・通信断など続けても意味がない失敗は投げる。
    /// </summary>
    public static FormatResult Run(IFloppyDevice device, DiskFormat format, string label, DateTime now,
        IProgress<FormatProgress>? progress, CancellationToken cancel)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new FormatResult { Format = format, Label = FatBuilder.NormalizeLabel(label), Serial = FatBuilder.MakeSerial(now) };
        int total = format.TrackCount + 1;

        device.WaitReady();
        if (device.IsWriteProtected())
            throw new IOException("ディスクがライトプロテクトされています。ライトプロテクトのつまみを戻してください。");

        for (int t = 0; t < format.TrackCount; t++)
        {
            cancel.ThrowIfCancellationRequested();
            int cylinder = t / DiskFormat.Heads, head = t % DiskFormat.Heads;
            progress?.Report(new FormatProgress(t, total, $"シリンダ {cylinder} / {format.Cylinders - 1}  ヘッド {head} をフォーマット中"));

            string? formatError = FormatTrack(device, format, cylinder, head);
            if (formatError != null)
            {
                for (int r = 1; r <= format.SectorsPerTrack; r++)
                    result.BadSectors.Add(new BadSector(format.Lba(cylinder, head, r), cylinder, head, r, $"トラックをフォーマットできない：{formatError}"));
            }
            else
            {
                VerifyTrack(device, format, cylinder, head, result.BadSectors);
            }

            var systemBad = result.BadSectors.FirstOrDefault(b => b.Lba < format.DataStart);
            if (systemBad != null)
            {
                result.Failure = $"システム領域（シリンダ {systemBad.Cylinder} ヘッド {systemBad.Head} セクタ {systemBad.R}）が不良のため、このディスクは使えません。";
                result.Elapsed = stopwatch.Elapsed;
                return result;
            }
        }

        cancel.ThrowIfCancellationRequested();
        progress?.Report(new FormatProgress(format.TrackCount, total, "システム領域（ブートセクタ・FAT・ルートディレクトリ）を書き込み中"));
        var (image, badClusters) = FatBuilder.Build(format, result.BadSectors.Select(b => b.Lba), result.Label, result.Serial, now);
        result.BadClusters = badClusters;
        result.Failure = WriteSystemArea(device, format, image);
        result.Elapsed = stopwatch.Elapsed;
        progress?.Report(new FormatProgress(total, total, result.Failure == null ? "完了" : "失敗"));
        return result;
    }

    /// <summary>成功なら null、媒体起因で MaxAttempts 回失敗したら理由を返す。</summary>
    static string? FormatTrack(IFloppyDevice device, DiskFormat format, int cylinder, int head)
    {
        string? error = null;
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                device.FormatTrack(format, cylinder, head);
                return null;
            }
            catch (UfiException ex) when (ex.IsMediumError)
            {
                error = ex.Message;
            }
            catch (UfiException ex) when (ex.IsInvalidParameter)
            {
                throw new IOException($"ドライブが {format.Name}（{format.Geometry}）のフォーマットを受け付けません。" +
                    (format.SectorSize == 1024 ? "3モード対応のドライブか確認してください。" : "") + $"\n{ex.Message}", ex);
            }
        }
        return error;
    }

    /// <summary>トラックを一括で読み、駄目ならセクタごとに再試行して読めないセクタを記録する。</summary>
    static void VerifyTrack(IFloppyDevice device, DiskFormat format, int cylinder, int head, List<BadSector> bad)
    {
        int lba = format.Lba(cylinder, head, 1);
        if (TryRead(device, format, lba, format.SectorsPerTrack, out _))
            return;
        for (int r = 1; r <= format.SectorsPerTrack; r++)
        {
            UfiException? last = null;
            bool ok = false;
            for (int attempt = 1; attempt <= MaxAttempts && !ok; attempt++)
                ok = TryRead(device, format, lba + r - 1, 1, out last);
            if (!ok)
                bad.Add(new BadSector(lba + r - 1, cylinder, head, r, last!.Message));
        }
    }

    static bool TryRead(IFloppyDevice device, DiskFormat format, int lba, int sectors, out UfiException? error)
    {
        try
        {
            device.Read(format, lba, sectors);
            error = null;
            return true;
        }
        catch (UfiException ex) when (ex.IsMediumError)
        {
            error = ex;
            return false;
        }
    }

    /// <summary>トラック単位で書いて読み返し照合する。成功なら null、媒体起因の失敗なら理由を返す。</summary>
    static string? WriteSystemArea(IFloppyDevice device, DiskFormat format, byte[] image)
    {
        int ss = format.SectorSize;
        for (int lba = 0; lba < format.DataStart; lba += format.SectorsPerTrack)
        {
            int count = Math.Min(format.SectorsPerTrack, format.DataStart - lba);
            var chunk = image.AsSpan(lba * ss, count * ss).ToArray();
            try
            {
                device.Write(format, lba, chunk, count);
                var readBack = device.Read(format, lba, count);
                if (!readBack.AsSpan().SequenceEqual(chunk))
                    return $"システム領域（LBA {lba}〜{lba + count - 1}）の照合が一致しません。";
            }
            catch (UfiException ex) when (ex.IsMediumError)
            {
                return $"システム領域（LBA {lba}〜{lba + count - 1}）を書き込めません：{ex.Message}";
            }
        }
        return null;
    }
}
