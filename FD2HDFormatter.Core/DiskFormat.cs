namespace FD2HDFormatter.Core;

/// <summary>2HD のフォーマット形式。どれも両面・1 クラスタ＝1 セクタ・FAT12・FAT 2 個。</summary>
public sealed class DiskFormat
{
    public const int Heads = 2, FatCount = 2, ReservedSectors = 1, DirEntrySize = 32;

    public required string Name { get; init; }
    public required int Cylinders { get; init; }
    public required int SectorsPerTrack { get; init; }
    public required int SectorSize { get; init; }
    public required int RootEntries { get; init; }
    public required int SectorsPerFat { get; init; }
    public required byte Media { get; init; }
    /// <summary>PC-98 用。ブートセクタのプログラムを PC-98 向けにする。</summary>
    public required bool Pc98 { get; init; }

    public int TrackCount => Cylinders * Heads;
    public int TotalSectors => TrackCount * SectorsPerTrack;
    public int RootSectors => RootEntries * DirEntrySize / SectorSize;
    /// <summary>データ領域の先頭 LBA。ここより前（ブートセクタ・FAT・ルートディレクトリ）がシステム領域。</summary>
    public int DataStart => ReservedSectors + FatCount * SectorsPerFat + RootSectors;
    public int Clusters => TotalSectors - DataStart;
    public string Geometry => $"{SectorSize} バイト×{SectorsPerTrack} セクタ×{Cylinders} シリンダ×{Heads} 面";

    /// <summary>UFI の論理ブロック番号。R は 1 始まり。</summary>
    public int Lba(int cylinder, int head, int r) => (cylinder * Heads + head) * SectorsPerTrack + (r - 1);

    public (int Cylinder, int Head, int R) Chr(int lba) =>
        (lba / SectorsPerTrack / Heads, lba / SectorsPerTrack % Heads, lba % SectorsPerTrack + 1);

    public static readonly DiskFormat Ibm144 = new()
    {
        Name = "IBM PC 1.44MB", Cylinders = 80, SectorsPerTrack = 18, SectorSize = 512,
        RootEntries = 224, SectorsPerFat = 9, Media = 0xF0, Pc98 = false,
    };

    public static readonly DiskFormat Pc98_125 = new()
    {
        Name = "PC-98 1.25MB", Cylinders = 77, SectorsPerTrack = 8, SectorSize = 1024,
        RootEntries = 192, SectorsPerFat = 2, Media = 0xFE, Pc98 = true,
    };

    public static readonly DiskFormat Pc98_144 = new()
    {
        Name = "PC-98 1.44MB", Cylinders = 80, SectorsPerTrack = 18, SectorSize = 512,
        RootEntries = 224, SectorsPerFat = 9, Media = 0xF0, Pc98 = true,
    };

    public static IReadOnlyList<DiskFormat> All { get; } = [Ibm144, Pc98_125, Pc98_144];
}
