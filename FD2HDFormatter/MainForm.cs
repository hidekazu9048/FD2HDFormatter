using System.Reflection;
using FD2HDFormatter.Core;

namespace FD2HDFormatter;

public partial class MainForm : Form
{
    CancellationTokenSource? _cancel;

    public MainForm()
    {
        InitializeComponent();
        Icon = Program.AppIcon;
        var version = Assembly.GetExecutingAssembly().GetName().Version!;
        Text = $"FD2HDFormatter {version.ToString(3)} - フロッピーディスクフォーマッタ（IBM PC / PC-98 2HD）";
        RefreshDrives();
    }

    DiskFormat SelectedFormat =>
        rdoPc98125.Checked ? DiskFormat.Pc98_125 :
        rdoPc98144.Checked ? DiskFormat.Pc98_144 :
        DiskFormat.Ibm144;

    void RefreshDrives()
    {
        cmbDrive.Items.Clear();
        foreach (var d in UfiDevice.FindFloppyDrives())
            cmbDrive.Items.Add($"{d}:");
        if (cmbDrive.Items.Count > 0)
            cmbDrive.SelectedIndex = 0;
        lblProgress.Text = cmbDrive.Items.Count > 0 ? "" : "フロッピードライブが見つかりません。USB FDD をつないで［再検索］を押してください。";
        UpdateButtons();
    }

    void UpdateButtons()
    {
        bool busy = _cancel != null;
        btnStart.Enabled = !busy && cmbDrive.SelectedItem != null;
        btnCancel.Enabled = busy;
        btnRefresh.Enabled = cmbDrive.Enabled = grpFormat.Enabled = txtLabel.Enabled = !busy;
    }

    void ShowStatus(string text, Color color)
    {
        lblStatus.Text = text;
        lblStatus.ForeColor = color;
    }

    void cmbDrive_SelectedIndexChanged(object? sender, EventArgs e) => UpdateButtons();

    void btnRefresh_Click(object sender, EventArgs e) => RefreshDrives();

    void btnCancel_Click(object sender, EventArgs e) => _cancel?.Cancel();

    async void btnStart_Click(object sender, EventArgs e)
    {
        if (cmbDrive.SelectedItem is not string driveText)
            return;
        char drive = driveText[0];
        var format = SelectedFormat;
        string label;
        try
        {
            label = FatBuilder.NormalizeLabel(txtLabel.Text);
        }
        catch (ArgumentException ex)
        {
            MessageBox.Show(this, ex.Message, "ボリュームラベル", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtLabel.Focus();
            return;
        }

        var answer = MessageBox.Show(this,
            $"{drive}: のディスクを {format.Name}（{format.Geometry}）でフォーマットします。\nディスクの内容はすべて消えます。よろしいですか？",
            "フォーマットの確認", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.OK)
            return;

        _cancel = new CancellationTokenSource();
        var token = _cancel.Token;
        UpdateButtons();
        progressBar.Maximum = format.TrackCount + 1;
        progressBar.Value = 0;
        txtResult.Clear();
        ShowStatus("フォーマット中...", SystemColors.ControlText);
        var progress = new Progress<FormatProgress>(p =>
        {
            progressBar.Value = p.Done;
            lblProgress.Text = $"{p.Done * 100 / p.Total,3}%  {p.Message}";
        });

        try
        {
            var result = await Task.Run(() =>
            {
                using var device = UfiDevice.Open(drive);
                device.LockAndDismount();
                return Formatter.Run(device, format, label, DateTime.Now, progress, token);
            });
            txtResult.Text = ResultReport.Build(result, $"{drive}:");

            // MessageBox はアイコンの種類に応じた Windows 標準のサウンド（情報／エラー）を鳴らす
            if (result.Failure != null)
            {
                ShowStatus("失敗しました", Color.Firebrick);
                lblProgress.Text = "フォーマットに失敗しました。";
                MessageBox.Show(this, $"フォーマットに失敗しました。\n\n{result.Failure}", "FD2HDFormatter", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (result.BadSectors.Count > 0)
            {
                ShowStatus($"完了しました（不良セクタ {result.BadSectors.Count} 個・{result.BadBytes:N0} バイトを使用不可に登録）", Color.DarkOrange);
                MessageBox.Show(this, $"完了しました。\n\n不良セクタが {result.BadSectors.Count} 個ありました。\n{result.BadBytes:N0} バイトを使用不可として登録し、{result.AvailableBytes:N0} バイトが使えます。",
                    "FD2HDFormatter", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                ShowStatus("完了しました（不良セクタなし）", Color.ForestGreen);
                MessageBox.Show(this, $"完了しました。\n\n不良セクタはありません。{result.AvailableBytes:N0} バイトが使えます。",
                    "FD2HDFormatter", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (OperationCanceledException)
        {
            ShowStatus("中止しました", Color.DimGray);
            lblProgress.Text = "中止しました。ディスクは途中までしかフォーマットされていないので、使う前にもう一度フォーマットしてください。";
        }
        catch (Exception ex)
        {
            ShowStatus("失敗しました", Color.Firebrick);
            lblProgress.Text = "フォーマットに失敗しました。";
            txtResult.Text = $"ドライブ        {drive}:{Environment.NewLine}形式            {format.Name}（{format.Geometry}）{Environment.NewLine}失敗の理由      {ex.Message}";
            MessageBox.Show(this, $"フォーマットに失敗しました。\n\n{ex.Message}", "FD2HDFormatter", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _cancel.Dispose();
            _cancel = null;
            UpdateButtons();
        }
    }

    void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_cancel != null)
        {
            MessageBox.Show(this, "フォーマット中です。中止してから閉じてください。", "FD2HDFormatter", MessageBoxButtons.OK, MessageBoxIcon.Information);
            e.Cancel = true;
        }
    }
}
