using System.Diagnostics;
using System.Text;
using Format2hd.Core;

// フォーマット処理を実機の代わりにイメージ（メモリ上のディスク）へ流し、出来た FAT12 を検査する。
// 使い方：format2hd.Check <出力フォルダ> [7z.exe のパス]
if (args.Length < 1)
{
    Console.Error.WriteLine("使い方：format2hd.Check <出力フォルダ> [7z.exe のパス]");
    return 2;
}
Console.OutputEncoding = Encoding.UTF8;
string outDir = args[0];
string? sevenZip = args.Length > 1 ? args[1] : null;
Directory.CreateDirectory(outDir);
var now = new DateTime(2026, 10, 28, 12, 34, 56, 780);
int failures = 0;

foreach (var format in DiskFormat.All)
{
    string tag = format.Name.Replace(" ", "_").Replace(".", "");

    // 1. 不良なし
    {
        var dev = new ImageDevice(format);
        var r = Formatter.Run(dev, format, "test disk", now, null, CancellationToken.None);
        Expect(r.Failure == null && r.BadSectors.Count == 0, $"{format.Name} 不良なし：成功する");
        Expect(dev.Writes == format.DataStart, $"{format.Name} 不良なし：システム領域 {format.DataStart} セクタを書いた（{dev.Writes}）");
        CheckFat(format, dev.Image, r, "TEST DISK");
        Save(dev, $"{tag}_ok.img", r);
    }

    // 2. データ領域に不良（読めないセクタ 2 個＋フォーマットできないトラック 1 本）
    {
        var dev = new ImageDevice(format);
        int a = format.DataStart + 100, b = format.TotalSectors - 1;
        dev.BadLbas.UnionWith([a, b]);
        dev.BadFormatTracks.Add(10);
        var r = Formatter.Run(dev, format, "", now, null, CancellationToken.None);
        int expected = 2 + format.SectorsPerTrack;
        Expect(r.Failure == null, $"{format.Name} データ領域の不良：成功扱い");
        Expect(r.BadSectors.Count == expected && r.BadClusters == expected, $"{format.Name} データ領域の不良：{expected} 個を記録・登録（{r.BadSectors.Count} 個・{r.BadClusters} クラスタ）");
        Expect(r.BadSectors.Any(s => s.Lba == a) && r.BadSectors.Any(s => s.Lba == b), $"{format.Name} データ領域の不良：該当 LBA を特定");
        Expect(dev.FormatCalls == format.TrackCount - 1 + Formatter.MaxAttempts, $"{format.Name} データ領域の不良：フォーマットできないトラックは {Formatter.MaxAttempts} 回試行（呼び出し {dev.FormatCalls}）");
        CheckFat(format, dev.Image, r, "");
        Save(dev, $"{tag}_bad.img", r);
    }

    // 3. システム領域に不良 → 失敗、それ以降のトラックは触らない
    {
        var dev = new ImageDevice(format);
        dev.BadLbas.Add(format.DataStart - 1);
        var r = Formatter.Run(dev, format, "", now, null, CancellationToken.None);
        Expect(r.Failure != null, $"{format.Name} システム領域の不良：失敗（{r.Failure}）");
        Expect(dev.Writes == 0, $"{format.Name} システム領域の不良：何も書かない");
        Expect(dev.FormatCalls == format.Chr(format.DataStart - 1).Cylinder * 2 + format.Chr(format.DataStart - 1).Head + 1, $"{format.Name} システム領域の不良：そのトラックで打ち切る（{dev.FormatCalls} トラック）");
    }

    // 4. ライトプロテクト → 例外
    {
        var dev = new ImageDevice(format) { WriteProtected = true };
        try
        {
            Formatter.Run(dev, format, "", now, null, CancellationToken.None);
            Expect(false, $"{format.Name} ライトプロテクト：例外になる");
        }
        catch (IOException ex)
        {
            Expect(dev.FormatCalls == 0, $"{format.Name} ライトプロテクト：例外になる（{ex.Message}）");
        }
    }

    // 5. ドライブが形式を受け付けない → 例外
    {
        var dev = new ImageDevice(format) { RejectFormat = true };
        try
        {
            Formatter.Run(dev, format, "", now, null, CancellationToken.None);
            Expect(false, $"{format.Name} 非対応ドライブ：例外になる");
        }
        catch (IOException ex)
        {
            Expect(dev.FormatCalls == 1, $"{format.Name} 非対応ドライブ：1 回目で例外（{ex.Message.Split('\n')[0]}）");
        }
    }
}

// ラベルの検査
Expect(FatBuilder.NormalizeLabel("abc_1") == "ABC_1", "ラベル：大文字にする");
foreach (var bad in new[] { "123456789012", " A", "A.B", "あ" })
{
    try { FatBuilder.NormalizeLabel(bad); Expect(false, $"ラベル「{bad}」を拒否"); }
    catch (ArgumentException) { Expect(true, $"ラベル「{bad}」を拒否"); }
}

Console.WriteLine(failures == 0 ? "すべて OK" : $"NG {failures} 件");
return failures == 0 ? 0 : 1;

void Expect(bool ok, string what)
{
    Console.WriteLine($"{(ok ? "OK" : "NG")}  {what}");
    if (!ok) failures++;
}

// 自前の読み取りで BPB・FAT・ラベルを確かめる
void CheckFat(DiskFormat f, byte[] img, FormatResult r, string label)
{
    var s = img.AsSpan();
    string name = f.Name;
    Expect(s[0] == 0xEB && s[2] == 0x90 && s[0x1FE] == 0x55 && s[0x1FF] == 0xAA, $"{name} ブートセクタの JMP と 55AA");
    Expect(BitConverter.ToUInt16(img, 0x0B) == f.SectorSize && BitConverter.ToUInt16(img, 0x13) == f.TotalSectors
        && img[0x15] == f.Media && BitConverter.ToUInt16(img, 0x16) == f.SectorsPerFat && BitConverter.ToUInt16(img, 0x18) == f.SectorsPerTrack
        && BitConverter.ToUInt16(img, 0x11) == f.RootEntries,
        $"{name} BPB（{BitConverter.ToUInt16(img, 0x0B)} バイト／セクタ・{BitConverter.ToUInt16(img, 0x13)} セクタ・メディア {img[0x15]:X2}）");
    int fatBytes = f.SectorsPerFat * f.SectorSize;
    var fat1 = s.Slice(f.SectorSize, fatBytes);
    var fat2 = s.Slice(f.SectorSize + fatBytes, fatBytes);
    Expect(fat1.SequenceEqual(fat2), $"{name} FAT1 と FAT2 が一致");
    Expect(FatBuilder.GetFat12(fat1, 0) == (0xF00 | f.Media) && FatBuilder.GetFat12(fat1, 1) == 0xFFF, $"{name} FAT の先頭 2 エントリ");
    var badLbas = r.BadSectors.Select(b => b.Lba).ToHashSet();
    int mismatch = 0;
    for (int c = 2; c < f.Clusters + 2; c++)
    {
        int expected = badLbas.Contains(c - 2 + f.DataStart) ? FatBuilder.BadCluster : 0;
        if (FatBuilder.GetFat12(fat1, c) != expected) mismatch++;
    }
    Expect(mismatch == 0, $"{name} 全 {f.Clusters} クラスタの FAT エントリが不良セクタと対応");
    int root = (1 + 2 * f.SectorsPerFat) * f.SectorSize;
    string volName = Encoding.ASCII.GetString(img, 0x2B, 11);
    if (label.Length > 0)
        Expect(Encoding.ASCII.GetString(img, root, 11) == label.PadRight(11) && img[root + 11] == 0x08 && volName == label.PadRight(11), $"{name} ボリュームラベル「{label}」");
    else
        Expect(img[root] == 0 && volName == "NO NAME    ", $"{name} ラベルなし");
}

void Save(ImageDevice dev, string file, FormatResult r)
{
    string path = Path.Combine(outDir, file);
    File.WriteAllBytes(path, dev.Image);
    Console.WriteLine($"---- {path}");
    Console.Write(ResultReport.Build(r, "TEST:"));
    if (sevenZip == null) return;
    var psi = new ProcessStartInfo(sevenZip, ["l", "-slt", path]) { RedirectStandardOutput = true, StandardOutputEncoding = Encoding.UTF8 };
    using var p = Process.Start(psi)!;
    string output = p.StandardOutput.ReadToEnd();
    p.WaitForExit();
    var lines = output.Split('\n').Select(l => l.Trim()).Where(l => l.StartsWith("Type") || l.StartsWith("Free Space") || l.StartsWith("Cluster Size") || l.StartsWith("Volume") || l.StartsWith("Physical Size") || l.StartsWith("ERROR") || l.StartsWith("Warning"));
    foreach (var l in lines) Console.WriteLine($"  7z: {l}");
    Expect(p.ExitCode == 0 && output.Contains("Type = FAT"), $"7-Zip が FAT として開ける（終了コード {p.ExitCode}）");
    Expect(output.Contains($"Free Space = {r.AvailableBytes}"), $"7-Zip の空き容量が使用可能容量 {r.AvailableBytes:N0} と一致");
}

/// <summary>メモリ上のディスク。読めないセクタ・フォーマットできないトラックを仕込める。</summary>
sealed class ImageDevice(DiskFormat format) : IFloppyDevice
{
    public byte[] Image { get; } = new byte[format.TotalSectors * format.SectorSize];
    public HashSet<int> BadLbas { get; } = [];
    public HashSet<int> BadFormatTracks { get; } = [];
    public bool WriteProtected { get; init; }
    public bool RejectFormat { get; init; }
    public int FormatCalls { get; private set; }
    public int Writes { get; private set; }

    public void WaitReady() { }
    public bool IsWriteProtected() => WriteProtected;

    public void FormatTrack(DiskFormat f, int cylinder, int head)
    {
        FormatCalls++;
        if (RejectFormat) throw new UfiException($"FORMAT UNIT C{cylinder} H{head}", 0x5, 0x26, 0);
        int track = cylinder * DiskFormat.Heads + head;
        if (BadFormatTracks.Contains(track)) throw new UfiException($"FORMAT UNIT C{cylinder} H{head}", 0x3, 0x0C, 0);
        Image.AsSpan(track * f.SectorsPerTrack * f.SectorSize, f.SectorsPerTrack * f.SectorSize).Fill(0xF6);
    }

    public void Write(DiskFormat f, int lba, byte[] data, int sectors)
    {
        Check(f, lba, sectors, "WRITE", 0x0C);
        data.CopyTo(Image, lba * f.SectorSize);
        Writes += sectors;
    }

    public byte[] Read(DiskFormat f, int lba, int sectors)
    {
        Check(f, lba, sectors, "READ", 0x11);
        return Image.AsSpan(lba * f.SectorSize, sectors * f.SectorSize).ToArray();
    }

    void Check(DiskFormat f, int lba, int sectors, string op, int asc)
    {
        if (!ReferenceEquals(f, format)) throw new InvalidOperationException("形式が違う");
        for (int i = lba; i < lba + sectors; i++)
        {
            int track = i / f.SectorsPerTrack;
            if (BadLbas.Contains(i) || BadFormatTracks.Contains(track))
                throw new UfiException($"{op} LBA{lba}", 0x3, asc, 0);
        }
    }
}
