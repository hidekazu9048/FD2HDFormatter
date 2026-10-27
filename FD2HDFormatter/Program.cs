namespace FD2HDFormatter;

static class Program
{
    /// <summary>各ウィンドウのタイトルバーに出すアプリのアイコン（Resources\app.ico）。</summary>
    internal static readonly Icon AppIcon = new(typeof(Program).Assembly.GetManifestResourceStream("app.ico")!);

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
