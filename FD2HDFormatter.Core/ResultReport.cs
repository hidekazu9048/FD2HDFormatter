using System.Text;

namespace FD2HDFormatter.Core;

/// <summary>結果欄に出す文面（MS-DOS の FORMAT の表示に寄せる）。</summary>
public static class ResultReport
{
    public static string Build(FormatResult r, string drive)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"ドライブ        {drive}");
        sb.AppendLine($"形式            {r.Format.Name}（{r.Format.Geometry}）");
        sb.AppendLine($"所要時間        {(int)r.Elapsed.TotalMinutes} 分 {r.Elapsed.Seconds:D2} 秒");
        if (r.Failure != null)
        {
            sb.AppendLine($"失敗の理由      {r.Failure}");
        }
        else
        {
            sb.AppendLine($"ボリュームラベル {(r.Label.Length > 0 ? r.Label : "なし")}");
            sb.AppendLine($"シリアル番号    {r.Serial >> 16:X4}-{r.Serial & 0xFFFF:X4}");
            sb.AppendLine();
            sb.AppendLine($"{r.TotalBytes,13:N0} バイト  全ディスク容量");
            sb.AppendLine($"{r.BadBytes,13:N0} バイト  不良セクタ");
            sb.AppendLine($"{r.AvailableBytes,13:N0} バイト  使用可能ディスク容量");
            sb.AppendLine();
            sb.AppendLine($"{r.ClusterSize,13:N0} バイト  割り当て単位");
            sb.AppendLine($"{r.Format.Clusters,13:N0} 個      割り当て単位の総数");
            sb.AppendLine($"{r.Format.Clusters - r.BadClusters,13:N0} 個      使用可能な割り当て単位");
        }
        sb.AppendLine();
        if (r.BadSectors.Count == 0)
        {
            sb.AppendLine("不良セクタ      なし");
        }
        else
        {
            sb.AppendLine($"不良セクタ      {r.BadSectors.Count} 個");
            foreach (var b in r.BadSectors)
                sb.AppendLine($"  シリンダ {b.Cylinder,2} ヘッド {b.Head} セクタ {b.R,2}（LBA {b.Lba,4}）  {b.Error}");
        }
        return sb.ToString();
    }
}
