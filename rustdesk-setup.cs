using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;

public class VersionData
{
    public string stable { get; set; }
    public string nightly { get; set; }
}

class Program
{
    static string versionsUrl = "https://rustmark.pages.dev/versions.json";
    static string rustdeskCfg = "##CONFIG##"; // Output of an installed Rustdesk network config, remove the "=" at the begining
    static string rustdeskPw = "##PASSWD##"; // Default "permanent password"

    static string tempDir = Path.GetTempPath();
    static string rustdeskExe = "rustdesk.exe";
    static string programFilesDir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

    // HttpClient instance for better management
    static HttpClient httpClient = new HttpClient();

    // Log file path
    static string logFilePath = "c:\\rustdesk-setup.log";

    static void Main(string[] args)
    {
        // Redirect console output to log file
        RedirectConsoleOutput();

        try
        {
            Initialize();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Main method: {ex.Message}");
        }
        finally
        {
            // Optionally, reset console output to default
            ResetConsoleOutput();
        }
    }

    static void RedirectConsoleOutput()
    {
        try
        {
            StreamWriter writer = new StreamWriter(logFilePath, append: true);
            writer.AutoFlush = true; // Ensure content is flushed immediately to the file

            // Redirect standard output and error to the log file
            Console.SetOut(writer);
            Console.SetError(writer);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error redirecting console output: {ex.Message}");
        }
    }

    static void ResetConsoleOutput()
    {
        try
        {
            // Reset standard output and error to defaults (console)
            var standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);
            Console.SetError(standardOutput);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error resetting console output: {ex.Message}");
        }
    }

    static void Initialize()
    {
        string rustdeskUrl = GetLatestRustdeskUrl(versionsUrl, useStableVersion: true);
        Console.WriteLine($"Using Rustdesk Stable URL: {rustdeskUrl}");

        if (string.IsNullOrEmpty(rustdeskUrl))
        {
            Console.WriteLine("Failed to retrieve Rustdesk URL. Exiting.");
            return;
        }

        DownloadAndInstallRustdesk(rustdeskUrl, tempDir, rustdeskExe);
        string rustdeskDir = Path.Combine(programFilesDir, "RustDesk");
        string runMe = Path.Combine(rustdeskDir, rustdeskExe);
        string rustdeskId = GetRustdeskId(runMe, rustdeskDir);
        ConfigureAndRunRustdesk(rustdeskId, runMe);

        Console.Clear();
        Console.WriteLine($"{Environment.MachineName} : {rustdeskId}");
        SaveRustdeskInfo(rustdeskId);
        DisplayPopup(rustdeskId);

        Cleanup(tempDir, rustdeskExe);
    }

    private static string GetLatestRustdeskUrl(string url, bool useStableVersion)
    {
        try
        {
            var response = httpClient.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();
            string json = response.Content.ReadAsStringAsync().Result;

            var data = JsonSerializer.Deserialize<VersionData>(json);

            return useStableVersion ? data.stable : data.nightly;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error fetching Rustdesk URL: {e.Message}");
            return null;
        }
    }

    static void DownloadAndInstallRustdesk(string url, string tempDir, string exeName)
    {
        string exePath = Path.Combine(tempDir, exeName);
        Console.WriteLine("Downloading latest stable Rustdesk build...");

        try
        {
            var response = httpClient.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();

            using (var fileStream = File.OpenWrite(exePath))
            {
                response.Content.CopyToAsync(fileStream).Wait();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading or installing Rustdesk: {ex.Message}");
            return;
        }

        Console.WriteLine("Installing stable Rustdesk build...");
        var installProcess = Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = "--silent-install",
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });
        installProcess.WaitForExit();

        Console.WriteLine("Waiting 20 Seconds...");
        System.Threading.Thread.Sleep(20000); // Wait for 20 seconds

        Console.WriteLine("Removing Desktop Shortcut...");
        try
        {
            File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "RustDesk.lnk"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting desktop shortcut: {ex.Message}");
        }
    }

    static string GetRustdeskId(string runMe, string rustdeskDir)
    {
        Console.WriteLine(runMe);
        Console.WriteLine("Getting Rustdesk ID...");
        var processStartInfo = new ProcessStartInfo
        {
            FileName = runMe,
            Arguments = "--get-id",
            WorkingDirectory = rustdeskDir,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        using (var process = Process.Start(processStartInfo))
        {
            if (process != null)
            {
                return process.StandardOutput.ReadToEnd().Trim();
            }
            else
            {
                throw new Exception("Failed to start Rustdesk process.");
            }
        }
    }

    static void ConfigureAndRunRustdesk(string rustdeskId, string runMe)
    {
        Console.WriteLine("Configuring and starting Rustdesk...");
        var process = new Process();
        process.StartInfo.FileName = runMe;
        process.StartInfo.Arguments = $"--config {rustdeskCfg} --password {rustdeskPw}";
        process.StartInfo.UseShellExecute = false;
        process.Start();
        process.WaitForExit();
    }

    static void SaveRustdeskInfo(string rustdeskId)
    {
        File.WriteAllText(@"c:\rustdesk.txt", $"Computer: {Environment.MachineName}\nID: {rustdeskId}");
    }

    static void DisplayPopup(string rustdeskId)
    {
        try
        {
            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint utype);
            string message = $"Computer: {Environment.MachineName}\nID: {rustdeskId}";
            string caption = "Rustdesk Installer";
            MessageBox((IntPtr)0, message, caption, 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in DisplayPopup method: {ex.Message}");
        }
    }

    static void Cleanup(string tempDir, string exeName)
    {
        try
        {
            File.Delete(Path.Combine(tempDir, exeName));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting temporary file: {ex.Message}");
        }
    }

    static void HideWindow()
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        IntPtr consoleWindow = GetConsoleWindow();
        ShowWindow(consoleWindow, SW_HIDE);
    }
}
