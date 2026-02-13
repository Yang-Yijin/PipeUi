using System.Diagnostics;

namespace PipeUi.Services
{
    public class PythonBridge : IDisposable
    {
        private Process? _process;

        public int Port { get; private set; } = 5050;
        public bool IsRunning => _process != null && !_process.HasExited;
        public string Url => $"http://127.0.0.1:{Port}";

        public void Start(string projectRoot, int port = 5050)
        {
            if (IsRunning) return;

            Port = port;
            string script = Path.Combine(projectRoot, "python_voltage", "app.py");
            string python = FindPython();

            _process = Process.Start(new ProcessStartInfo
            {
                FileName = python,
                Arguments = $"\"{script}\" {port}",
                WorkingDirectory = Path.Combine(projectRoot, "python_voltage"),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            })!;

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        public void Stop()
        {
            if (_process == null) return;
            try { if (!_process.HasExited) _process.Kill(true); }
            catch { }
            _process.Dispose();
            _process = null;
        }

        public void Dispose() => Stop();

        private static string FindPython()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string[] paths =
            {
                Path.Combine(home, "anaconda3", "python.exe"),
                Path.Combine(home, "miniconda3", "python.exe"),
                Path.Combine(home, "AppData", "Local", "Programs", "Python", "Python312", "python.exe"),
                Path.Combine(home, "AppData", "Local", "Programs", "Python", "Python311", "python.exe"),
            };
            foreach (var p in paths)
                if (File.Exists(p)) return p;
            return "python";
        }
    }
}
