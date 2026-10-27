using System.Text;

namespace FD2HDFormatter.Core;

/// <summary>FAT12 のシステム領域（ブートセクタ・FAT×2・ルートディレクトリ）を組み立てる。</summary>
public static class FatBuilder
{
    public const int BadCluster = 0xFF7;
    const string AllowedLabelSymbols = " !#$%&'()-@^_`{}~";

    // IBM PC 用：メッセージを INT 10h で出し、INT 16h でキーを待って INT 19h で再起動（0x7C00 に読まれる前提）
    static readonly byte[] IbmBootCode =
    [
        0xFC,             // 3E cld
        0x31, 0xC0,       // 3F xor ax,ax
        0x8E, 0xD8,       // 41 mov ds,ax
        0xBE, 0x5A, 0x7C, // 43 mov si,7C5Ah
        0xAC,             // 46 lodsb
        0x08, 0xC0,       // 47 or al,al
        0x74, 0x09,       // 49 jz 54h
        0xB4, 0x0E,       // 4B mov ah,0Eh
        0xBB, 0x07, 0x00, // 4D mov bx,7
        0xCD, 0x10,       // 50 int 10h
        0xEB, 0xF2,       // 52 jmp 46h
        0x31, 0xC0,       // 54 xor ax,ax
        0xCD, 0x16,       // 56 int 16h
        0xCD, 0x19,       // 58 int 19h
    ];                    // 5A メッセージ

    // PC-98 用：読み込み先の番地に依存しないよう call/pop で位置を得て、テキスト VRAM（A000h、属性 A200h）へ直接書いて停止
    static readonly byte[] Pc98BootCode =
    [
        0xFC,                               // 3E cld
        0x0E,                               // 3F push cs
        0x1F,                               // 40 pop ds
        0xE8, 0x00, 0x00,                   // 41 call 44h
        0x5E,                               // 44 pop si
        0x83, 0xC6, 0x1D,                   // 45 add si,1Dh（→ 61h）
        0xB8, 0x00, 0xA0,                   // 48 mov ax,0A000h
        0x8E, 0xC0,                         // 4B mov es,ax
        0x31, 0xFF,                         // 4D xor di,di
        0xB4, 0x00,                         // 4F mov ah,0
        0xAC,                               // 51 lodsb
        0x08, 0xC0,                         // 52 or al,al
        0x74, 0x09,                         // 54 jz 5Fh
        0x26, 0xC6, 0x85, 0x00, 0x20, 0xE1, // 56 mov byte es:[di+2000h],0E1h（白・表示）
        0xAB,                               // 5C stosw
        0xEB, 0xF2,                         // 5D jmp 51h
        0xEB, 0xFE,                         // 5F jmp $
    ];                                      // 61 メッセージ

    const int BootCodeOffset = 0x3E;
    const string IbmMessage = "\r\nNon-system disk or disk error\r\nReplace and press any key when ready\r\n";
    const string Pc98Message = "Non-system disk. Insert a system disk and press RESET.";

    /// <summary>ボリュームラベルを大文字にして検査する。空なら空文字を返す（ラベルなし）。</summary>
    public static string NormalizeLabel(string label)
    {
        label = label.TrimEnd().ToUpperInvariant();
        if (label.Length > 11)
            throw new ArgumentException("ボリュームラベルは 11 文字以内にしてください。");
        if (label.StartsWith(' '))
            throw new ArgumentException("ボリュームラベルの先頭に空白は使えません。");
        foreach (char ch in label)
            if (!(ch is >= 'A' and <= 'Z' or >= '0' and <= '9' || AllowedLabelSymbols.Contains(ch)))
                throw new ArgumentException($"ボリュームラベルに「{ch}」は使えません（半角英数字と {AllowedLabelSymbols.Trim()} のみ）。");
        return label;
    }

    /// <summary>MS-DOS と同じく日付・時刻から作るボリュームシリアル番号。</summary>
    public static uint MakeSerial(DateTime now)
    {
        int hundredths = now.Millisecond / 10;
        uint high = (uint)(((now.Month << 8) | now.Day) + ((now.Second << 8) | hundredths)) & 0xFFFF;
        uint low = (uint)(now.Year + ((now.Hour << 8) | now.Minute)) & 0xFFFF;
        return high << 16 | low;
    }

    /// <summary>データ領域の LBA をクラスタ番号へ。</summary>
    public static int ClusterOf(DiskFormat format, int lba) => lba - format.DataStart + 2;

    /// <summary>
    /// システム領域（DataStart セクタ分）を作る。badLbas のうちデータ領域にあるものはそのクラスタを不良（FF7h）として FAT に登録する。
    /// 戻り値の BadClusters は登録したクラスタ数。
    /// </summary>
    public static (byte[] Image, int BadClusters) Build(DiskFormat format, IEnumerable<int> badLbas, string label, uint serial, DateTime now)
    {
        label = NormalizeLabel(label);
        int ss = format.SectorSize;
        var image = new byte[format.DataStart * ss];

        WriteBootSector(format, image.AsSpan(0, ss), label, serial);

        var fat = new byte[format.SectorsPerFat * ss];
        SetFat12(fat, 0, 0xF00 | format.Media);
        SetFat12(fat, 1, 0xFFF);
        var badClusters = new SortedSet<int>();
        foreach (int lba in badLbas)
            if (lba >= format.DataStart && lba < format.TotalSectors)
                badClusters.Add(ClusterOf(format, lba));
        foreach (int cluster in badClusters)
            SetFat12(fat, cluster, BadCluster);
        for (int i = 0; i < DiskFormat.FatCount; i++)
            fat.CopyTo(image, (DiskFormat.ReservedSectors + i * format.SectorsPerFat) * ss);

        if (label.Length > 0)
        {
            var entry = image.AsSpan((DiskFormat.ReservedSectors + DiskFormat.FatCount * format.SectorsPerFat) * ss, DiskFormat.DirEntrySize);
            Encoding.ASCII.GetBytes(label.PadRight(11)).CopyTo(entry);
            entry[11] = 0x08; // ボリュームラベル属性
            ushort time = (ushort)(now.Hour << 11 | now.Minute << 5 | now.Second / 2);
            ushort date = (ushort)((now.Year - 1980) << 9 | now.Month << 5 | now.Day);
            BitConverter.TryWriteBytes(entry[0x16..], time);
            BitConverter.TryWriteBytes(entry[0x18..], date);
        }
        return (image, badClusters.Count);
    }

    static void WriteBootSector(DiskFormat format, Span<byte> s, string label, uint serial)
    {
        s[0] = 0xEB; s[1] = 0x3C; s[2] = 0x90; // jmp 3Eh
        Encoding.ASCII.GetBytes("MSDOS5.0").CopyTo(s[3..]);
        BitConverter.TryWriteBytes(s[0x0B..], (ushort)format.SectorSize);
        s[0x0D] = 1; // セクタ／クラスタ
        BitConverter.TryWriteBytes(s[0x0E..], (ushort)DiskFormat.ReservedSectors);
        s[0x10] = DiskFormat.FatCount;
        BitConverter.TryWriteBytes(s[0x11..], (ushort)format.RootEntries);
        BitConverter.TryWriteBytes(s[0x13..], (ushort)format.TotalSectors);
        s[0x15] = format.Media;
        BitConverter.TryWriteBytes(s[0x16..], (ushort)format.SectorsPerFat);
        BitConverter.TryWriteBytes(s[0x18..], (ushort)format.SectorsPerTrack);
        BitConverter.TryWriteBytes(s[0x1A..], (ushort)DiskFormat.Heads);
        // 0x1C 隠しセクタ数・0x20 総セクタ数（32 ビット）は 0
        s[0x24] = 0x00; // ドライブ番号
        s[0x26] = 0x29; // 拡張ブートシグネチャ
        BitConverter.TryWriteBytes(s[0x27..], serial);
        Encoding.ASCII.GetBytes((label.Length > 0 ? label : "NO NAME").PadRight(11)).CopyTo(s[0x2B..]);
        Encoding.ASCII.GetBytes("FAT12   ").CopyTo(s[0x36..]);

        var code = format.Pc98 ? Pc98BootCode : IbmBootCode;
        var message = Encoding.ASCII.GetBytes((format.Pc98 ? Pc98Message : IbmMessage) + "\0");
        code.CopyTo(s[BootCodeOffset..]);
        message.CopyTo(s[(BootCodeOffset + code.Length)..]);
        s[0x1FE] = 0x55; s[0x1FF] = 0xAA;
    }

    public static void SetFat12(Span<byte> fat, int cluster, int value)
    {
        int o = cluster * 3 / 2;
        if ((cluster & 1) == 0)
        {
            fat[o] = (byte)value;
            fat[o + 1] = (byte)((fat[o + 1] & 0xF0) | ((value >> 8) & 0x0F));
        }
        else
        {
            fat[o] = (byte)((fat[o] & 0x0F) | ((value << 4) & 0xF0));
            fat[o + 1] = (byte)(value >> 4);
        }
    }

    public static int GetFat12(ReadOnlySpan<byte> fat, int cluster)
    {
        int o = cluster * 3 / 2;
        int v = fat[o] | fat[o + 1] << 8;
        return (cluster & 1) == 0 ? v & 0xFFF : v >> 4;
    }
}
