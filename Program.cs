using System;
using System.IO;
using System.Windows.Forms;

namespace LolKey;

static class Program
{
    [STAThread]
    static void Main()
    {
        try
        {
            string appLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");
            File.AppendAllText(appLog, $"[INFO] Anti Ăn Toxic (AUT) started at {DateTime.Now}\n");

            ApplicationConfiguration.Initialize();

            // Load saved configurations
            ConfigManager.Load();
            File.AppendAllText(appLog, $"[INFO] Config loaded at {DateTime.Now}\n");

            // Start Keyboard Hook Engine
            KeyboardEngine.Start();
            File.AppendAllText(appLog, $"[INFO] KeyboardEngine started at {DateTime.Now}\n");

            try
            {
                // Run Windows Forms Application
                Application.Run(new MainForm());
            }
            finally
            {
                // Stop Keyboard Hook Engine on exit
                KeyboardEngine.Stop();
            }
        }
        catch (Exception ex)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.log");
                File.WriteAllText(logPath, ex.ToString());
            }
            catch { }
        }
    }
}
