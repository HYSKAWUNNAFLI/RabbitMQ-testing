using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace BackendManager.Services
{
    public class ProcessManager
    {
        private readonly IHubContext<ConsoleHub> _hubContext;
        private readonly ConcurrentDictionary<string, Process> _runningProcesses = new();

        public ProcessManager(IHubContext<ConsoleHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task StartModule(string moduleName)
        {
            if (_runningProcesses.ContainsKey(moduleName))
            {
                await _hubContext.Clients.All.SendAsync("ReceiveLog", moduleName, "[System] Module is already running.");
                return;
            }

            var projectPath = Path.Combine("..", moduleName);
            if (!Directory.Exists(projectPath))
            {
                // In Docker, the projects will be at /app, and the backend is running at /app/BackendManager
                projectPath = Path.Combine("/app", moduleName);
                if (!Directory.Exists(projectPath))
                {
                    // Fallback to local
                    projectPath = Path.Combine("..", moduleName);
                }
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{projectPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

            process.OutputDataReceived += async (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveLog", moduleName, e.Data);
                }
            };

            process.ErrorDataReceived += async (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveLog", moduleName, $"[ERROR] {e.Data}");
                }
            };

            process.Exited += async (sender, e) =>
            {
                _runningProcesses.TryRemove(moduleName, out _);
                await _hubContext.Clients.All.SendAsync("ReceiveLog", moduleName, "[System] Module stopped.");
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            _runningProcesses.TryAdd(moduleName, process);
            await _hubContext.Clients.All.SendAsync("ReceiveLog", moduleName, "[System] Module started.");
        }

        public async Task StopModule(string moduleName)
        {
            if (_runningProcesses.TryGetValue(moduleName, out var process))
            {
                try
                {
                    process.Kill(true);
                    _runningProcesses.TryRemove(moduleName, out _);
                    await _hubContext.Clients.All.SendAsync("ReceiveLog", moduleName, "[System] Module stopped by user.");
                }
                catch (Exception ex)
                {
                    await _hubContext.Clients.All.SendAsync("ReceiveLog", moduleName, $"[ERROR] Failed to stop: {ex.Message}");
                }
            }
        }
    }
}
