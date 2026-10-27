using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace FD2HDFormatter.Core;

/// <summary>UFI コマンドが CHECK CONDITION で終わった。</summary>
public sealed class UfiException(string command, int senseKey, int asc, int ascq)
    : IOException($"{command} 失敗：{Describe(senseKey, asc)}（SK={senseKey:X} ASC={asc:X2} ASCQ={ascq:X2}）")
{
    public int SenseKey { get; } = senseKey;
    public int Asc { get; } = asc;
    public int Ascq { get; } = ascq;

    /// <summary>媒体の傷などセクタ・トラック単位で起こる失敗。再試行・不良セクタとしての記録の対象。</summary>
    public bool IsMediumError => SenseKey is 0x3 or 0x4 or 0xB;

    /// <summary>ドライブがフォーマット記述子（容量・セクタ長）を受け付けなかった。</summary>
    public bool IsInvalidParameter => SenseKey == 0x5 && Asc is 0x24 or 0x26;

    static string Describe(int senseKey, int asc) => asc switch
    {
        0x04 => "ドライブの準備ができていない",
        0x0C => "書き込みエラー",
        0x10 => "ID の CRC エラー",
        0x11 => "読み取りエラー",
        0x14 => "セクタが見つからない",
        0x24 or 0x26 => "コマンドのパラメータが不正",
        0x27 => "ライトプロテクトされている",
        0x28 => "ディスクが交換された",
        0x31 => "フォーマットが壊れている",
        0x3A => "ディスクが入っていない",
        _ => senseKey switch
        {
            0x3 => "媒体エラー",
            0x4 => "ハードウェアエラー",
            0xB => "コマンドが中断された",
            _ => "エラー",
        },
    };
}

/// <summary>フォーマット処理が使うドライブ操作。実機は UfiDevice、検証ではイメージファイルで代用する。</summary>
public interface IFloppyDevice
{
    void WaitReady();
    bool IsWriteProtected();
    void FormatTrack(DiskFormat format, int cylinder, int head);
    void Write(DiskFormat format, int lba, byte[] data, int sectors);
    byte[] Read(DiskFormat format, int lba, int sectors);
}

/// <summary>
/// USB フロッピードライブへ UFI（SCSI 準拠）コマンドを直接送る。
/// Windows の sfloppy は 1.44MB しか扱わないので、1.25MB を含めて IOCTL_SCSI_PASS_THROUGH で FORMAT UNIT を送る。
/// </summary>
public sealed class UfiDevice : IFloppyDevice, IDisposable
{
    const uint GenericRead = 0x80000000, GenericWrite = 0x40000000, ShareReadWrite = 3, OpenExisting = 3;
    const uint IoctlScsiPassThrough = 0x0004D004;
    const uint FsctlLockVolume = 0x00090018, FsctlDismountVolume = 0x00090020;
    const int CdbLength = 12;
    // SCSI_PASS_THROUGH（x64）のあとにセンスデータ、その後ろに転送データを置く
    const int PassThroughSize = 56, SenseOffset = 56, SenseLength = 32, DataOffset = 88;

    readonly SafeFileHandle _handle;

    public char Drive { get; }

    UfiDevice(char drive, SafeFileHandle handle)
    {
        Drive = drive;
        _handle = handle;
    }

    /// <summary>実体が \Device\Floppy* のドライブ文字（USB メモリ等は含めない）。</summary>
    public static List<char> FindFloppyDrives()
    {
        var drives = new List<char>();
        foreach (var d in DriveInfo.GetDrives())
            if (d.DriveType == DriveType.Removable && IsFloppy(d.Name[0]))
                drives.Add(char.ToUpperInvariant(d.Name[0]));
        return drives;
    }

    static bool IsFloppy(char drive)
    {
        var target = new StringBuilder(512);
        if (QueryDosDevice($"{drive}:", target, target.Capacity) == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"{drive}: のデバイス名を取得できません。");
        return target.ToString().StartsWith(@"\Device\Floppy", StringComparison.OrdinalIgnoreCase);
    }

    public static UfiDevice Open(char drive)
    {
        if (IntPtr.Size != 8)
            throw new PlatformNotSupportedException("64 ビットプロセスでのみ動作します。");
        if (!IsFloppy(drive))
            throw new InvalidOperationException($"{drive}: はフロッピードライブではありません。");
        var handle = CreateFile($@"\\.\{drive}:", GenericRead | GenericWrite, ShareReadWrite, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero);
        if (handle.IsInvalid)
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"{drive}: を開けません。");
        return new UfiDevice(drive, handle);
    }

    /// <summary>ファイルシステムを外して排他にする。ハンドルを閉じると解除され、次のアクセスで Windows が読み直す。</summary>
    public void LockAndDismount()
    {
        if (!DeviceIoControl(_handle, FsctlLockVolume, null, 0, null, 0, out _, IntPtr.Zero))
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"{Drive}: をロックできません。エクスプローラ等で開いていないか確認してください。");
        if (!DeviceIoControl(_handle, FsctlDismountVolume, null, 0, null, 0, out _, IntPtr.Zero))
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"{Drive}: のマウントを解除できません。");
    }

    /// <summary>ディスク交換直後の UNIT ATTENTION や回転待ちを吸収して、使える状態になるまで待つ。</summary>
    public void WaitReady()
    {
        for (int i = 0; ; i++)
        {
            try
            {
                Execute("TEST UNIT READY", [0x00], null, 0, 10);
                return;
            }
            catch (UfiException ex) when (i < 10 && (ex.SenseKey == 0x6 || (ex.SenseKey == 0x2 && ex.Asc == 0x04)))
            {
                Thread.Sleep(300);
            }
        }
    }

    /// <summary>フレキシブルディスクページ（モードパラメータヘッダ 8 バイト＋ページ 32 バイト）。</summary>
    public byte[] ModeSenseFlexibleDisk()
    {
        var data = Execute("MODE SENSE", [0x5A, 0x00, 0x05, 0, 0, 0, 0, 0, 0xFC], null, 0xFC, 10);
        if (data.Length < 40 || (data[8] & 0x3F) != 0x05)
            throw new IOException("フレキシブルディスクページを読めません。UFI のフロッピードライブではない可能性があります。");
        return data;
    }

    public bool IsWriteProtected() => (ModeSenseFlexibleDisk()[3] & 0x80) != 0;

    /// <summary>1トラックをフォーマットする。記述子の総ブロック数・ブロック長でドライブが 1.44MB／1.25MB（360rpm）を切り替える。</summary>
    public void FormatTrack(DiskFormat format, int cylinder, int head)
    {
        byte[] cdb = [0x04, 0x17, (byte)cylinder, 0, 0, 0, 0, 0, 12];
        // 欠陥リストヘッダ：FOV | DCRT | SingleTrack | Side、記述子長 8
        // フォーマット記述子：総ブロック数、ブロック長
        int blocks = format.TotalSectors, length = format.SectorSize;
        byte[] parameters =
        [
            0x00, (byte)(0xB0 | head), 0x00, 0x08,
            (byte)(blocks >> 24), (byte)(blocks >> 16), (byte)(blocks >> 8), (byte)blocks,
            0x00, (byte)(length >> 16), (byte)(length >> 8), (byte)length,
        ];
        Execute($"FORMAT UNIT C{cylinder} H{head}", cdb, parameters, 0, 60);
    }

    public void Write(DiskFormat format, int lba, byte[] data, int sectors) =>
        Execute($"WRITE LBA{lba}", ReadWriteCdb(0x2A, lba, sectors), data, 0, 30);

    public byte[] Read(DiskFormat format, int lba, int sectors)
    {
        int expected = sectors * format.SectorSize;
        var data = Execute($"READ LBA{lba}", ReadWriteCdb(0x28, lba, sectors), null, expected, 30);
        if (data.Length != expected)
            throw new IOException($"READ LBA{lba}：{expected} バイトのはずが {data.Length} バイトしか返らない。");
        return data;
    }

    static byte[] ReadWriteCdb(byte op, int lba, int blocks) =>
        [op, 0, (byte)(lba >> 24), (byte)(lba >> 16), (byte)(lba >> 8), (byte)lba, 0, (byte)(blocks >> 8), (byte)blocks];

    byte[] Execute(string command, byte[] cdb, byte[]? dataOut, int dataInLength, int timeoutSeconds)
    {
        int dataLength = dataOut?.Length ?? dataInLength;
        var buffer = new byte[DataOffset + dataLength];
        BitConverter.TryWriteBytes(buffer.AsSpan(0), (ushort)PassThroughSize);
        buffer[6] = CdbLength;
        buffer[7] = SenseLength;
        buffer[8] = (byte)(dataOut != null ? 0 : 1); // SCSI_IOCTL_DATA_OUT / DATA_IN
        BitConverter.TryWriteBytes(buffer.AsSpan(12), dataLength);
        BitConverter.TryWriteBytes(buffer.AsSpan(16), timeoutSeconds);
        BitConverter.TryWriteBytes(buffer.AsSpan(24), (long)DataOffset);
        BitConverter.TryWriteBytes(buffer.AsSpan(32), SenseOffset);
        cdb.CopyTo(buffer, 36);
        dataOut?.CopyTo(buffer, DataOffset);

        if (!DeviceIoControl(_handle, IoctlScsiPassThrough, buffer, buffer.Length, buffer, buffer.Length, out _, IntPtr.Zero))
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"{command} を送れません。");
        if (buffer[2] != 0)
            throw new UfiException(command, buffer[SenseOffset + 2] & 0x0F, buffer[SenseOffset + 12], buffer[SenseOffset + 13]);
        int transferred = BitConverter.ToInt32(buffer, 12);
        return dataOut != null ? [] : buffer.AsSpan(DataOffset, transferred).ToArray();
    }

    public void Dispose() => _handle.Dispose();

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DeviceIoControl(SafeFileHandle handle, uint code, byte[]? input, int inputLength, byte[]? output, int outputLength, out int returned, IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern uint QueryDosDevice(string deviceName, StringBuilder target, int max);
}
