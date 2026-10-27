namespace FD2HDFormatter;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        lblDrive = new Label();
        cmbDrive = new ComboBox();
        btnRefresh = new Button();
        grpFormat = new GroupBox();
        rdoIbm144 = new RadioButton();
        rdoPc98125 = new RadioButton();
        rdoPc98144 = new RadioButton();
        lblLabel = new Label();
        txtLabel = new TextBox();
        lblLabelHint = new Label();
        btnStart = new Button();
        btnCancel = new Button();
        progressBar = new ProgressBar();
        lblProgress = new Label();
        grpResult = new GroupBox();
        lblStatus = new Label();
        txtResult = new TextBox();
        grpFormat.SuspendLayout();
        grpResult.SuspendLayout();
        SuspendLayout();
        //
        // lblDrive
        //
        lblDrive.AutoSize = true;
        lblDrive.Location = new Point(12, 15);
        lblDrive.Name = "lblDrive";
        lblDrive.Size = new Size(46, 15);
        lblDrive.TabIndex = 0;
        lblDrive.Text = "ドライブ";
        //
        // cmbDrive
        //
        cmbDrive.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbDrive.Location = new Point(110, 12);
        cmbDrive.Name = "cmbDrive";
        cmbDrive.Size = new Size(80, 23);
        cmbDrive.TabIndex = 1;
        cmbDrive.SelectedIndexChanged += cmbDrive_SelectedIndexChanged;
        //
        // btnRefresh
        //
        btnRefresh.Location = new Point(198, 11);
        btnRefresh.Name = "btnRefresh";
        btnRefresh.Size = new Size(90, 25);
        btnRefresh.TabIndex = 2;
        btnRefresh.Text = "再検索(&R)";
        btnRefresh.UseVisualStyleBackColor = true;
        btnRefresh.Click += btnRefresh_Click;
        //
        // grpFormat
        //
        grpFormat.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        grpFormat.Controls.Add(rdoIbm144);
        grpFormat.Controls.Add(rdoPc98125);
        grpFormat.Controls.Add(rdoPc98144);
        grpFormat.Location = new Point(12, 45);
        grpFormat.Name = "grpFormat";
        grpFormat.Size = new Size(576, 102);
        grpFormat.TabIndex = 3;
        grpFormat.TabStop = false;
        grpFormat.Text = "フォーマットの種類";
        //
        // rdoIbm144
        //
        rdoIbm144.AutoSize = true;
        rdoIbm144.Checked = true;
        rdoIbm144.Location = new Point(15, 22);
        rdoIbm144.Name = "rdoIbm144";
        rdoIbm144.Size = new Size(400, 19);
        rdoIbm144.TabIndex = 0;
        rdoIbm144.TabStop = true;
        rdoIbm144.Text = "IBM PC 1.44MB（512 バイト×18 セクタ×80 シリンダ）";
        rdoIbm144.UseVisualStyleBackColor = true;
        //
        // rdoPc98125
        //
        rdoPc98125.AutoSize = true;
        rdoPc98125.Location = new Point(15, 47);
        rdoPc98125.Name = "rdoPc98125";
        rdoPc98125.Size = new Size(400, 19);
        rdoPc98125.TabIndex = 1;
        rdoPc98125.Text = "PC-98 1.25MB（1024 バイト×8 セクタ×77 シリンダ）";
        rdoPc98125.UseVisualStyleBackColor = true;
        //
        // rdoPc98144
        //
        rdoPc98144.AutoSize = true;
        rdoPc98144.Location = new Point(15, 72);
        rdoPc98144.Name = "rdoPc98144";
        rdoPc98144.Size = new Size(400, 19);
        rdoPc98144.TabIndex = 2;
        rdoPc98144.Text = "PC-98 1.44MB（512 バイト×18 セクタ×80 シリンダ）";
        rdoPc98144.UseVisualStyleBackColor = true;
        //
        // lblLabel
        //
        lblLabel.AutoSize = true;
        lblLabel.Location = new Point(12, 160);
        lblLabel.Name = "lblLabel";
        lblLabel.Size = new Size(86, 15);
        lblLabel.TabIndex = 4;
        lblLabel.Text = "ボリュームラベル";
        //
        // txtLabel
        //
        txtLabel.CharacterCasing = CharacterCasing.Upper;
        txtLabel.Location = new Point(110, 157);
        txtLabel.MaxLength = 11;
        txtLabel.Name = "txtLabel";
        txtLabel.Size = new Size(140, 23);
        txtLabel.TabIndex = 5;
        //
        // lblLabelHint
        //
        lblLabelHint.AutoSize = true;
        lblLabelHint.Location = new Point(258, 160);
        lblLabelHint.Name = "lblLabelHint";
        lblLabelHint.Size = new Size(200, 15);
        lblLabelHint.TabIndex = 6;
        lblLabelHint.Text = "（省略可・半角英数字 11 文字まで）";
        //
        // btnStart
        //
        btnStart.Location = new Point(12, 192);
        btnStart.Name = "btnStart";
        btnStart.Size = new Size(130, 30);
        btnStart.TabIndex = 7;
        btnStart.Text = "フォーマット開始(&S)";
        btnStart.UseVisualStyleBackColor = true;
        btnStart.Click += btnStart_Click;
        //
        // btnCancel
        //
        btnCancel.Enabled = false;
        btnCancel.Location = new Point(150, 192);
        btnCancel.Name = "btnCancel";
        btnCancel.Size = new Size(80, 30);
        btnCancel.TabIndex = 8;
        btnCancel.Text = "中止(&C)";
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.Click += btnCancel_Click;
        //
        // progressBar
        //
        progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        progressBar.Location = new Point(12, 232);
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(576, 22);
        progressBar.TabIndex = 9;
        //
        // lblProgress
        //
        lblProgress.AutoSize = true;
        lblProgress.Location = new Point(12, 260);
        lblProgress.Name = "lblProgress";
        lblProgress.Size = new Size(0, 15);
        lblProgress.TabIndex = 10;
        //
        // grpResult
        //
        grpResult.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        grpResult.Controls.Add(lblStatus);
        grpResult.Controls.Add(txtResult);
        grpResult.Location = new Point(12, 285);
        grpResult.Name = "grpResult";
        grpResult.Size = new Size(576, 275);
        grpResult.TabIndex = 11;
        grpResult.TabStop = false;
        grpResult.Text = "結果";
        //
        // lblStatus
        //
        lblStatus.AutoSize = true;
        lblStatus.Font = new Font("Yu Gothic UI", 11F, FontStyle.Bold);
        lblStatus.Location = new Point(12, 22);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(70, 20);
        lblStatus.TabIndex = 0;
        lblStatus.Text = "未実行";
        //
        // txtResult
        //
        txtResult.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtResult.Font = new Font("MS Gothic", 9.75F);
        txtResult.Location = new Point(12, 50);
        txtResult.Multiline = true;
        txtResult.Name = "txtResult";
        txtResult.ReadOnly = true;
        txtResult.ScrollBars = ScrollBars.Both;
        txtResult.Size = new Size(552, 213);
        txtResult.TabIndex = 1;
        txtResult.WordWrap = false;
        //
        // MainForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(600, 572);
        Controls.Add(lblDrive);
        Controls.Add(cmbDrive);
        Controls.Add(btnRefresh);
        Controls.Add(grpFormat);
        Controls.Add(lblLabel);
        Controls.Add(txtLabel);
        Controls.Add(lblLabelHint);
        Controls.Add(btnStart);
        Controls.Add(btnCancel);
        Controls.Add(progressBar);
        Controls.Add(lblProgress);
        Controls.Add(grpResult);
        MinimumSize = new Size(560, 520);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "FD2HDFormatter";
        FormClosing += MainForm_FormClosing;
        grpFormat.ResumeLayout(false);
        grpFormat.PerformLayout();
        grpResult.ResumeLayout(false);
        grpResult.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label lblDrive;
    private ComboBox cmbDrive;
    private Button btnRefresh;
    private GroupBox grpFormat;
    private RadioButton rdoIbm144;
    private RadioButton rdoPc98125;
    private RadioButton rdoPc98144;
    private Label lblLabel;
    private TextBox txtLabel;
    private Label lblLabelHint;
    private Button btnStart;
    private Button btnCancel;
    private ProgressBar progressBar;
    private Label lblProgress;
    private GroupBox grpResult;
    private Label lblStatus;
    private TextBox txtResult;
}
