namespace TimezoneConverter;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        // Keep layout in 96-DPI coordinates so absolute positions stay readable
        Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
        Application.Run(new MainForm());
    }
}
