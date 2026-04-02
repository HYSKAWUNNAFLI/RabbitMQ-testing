using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace BackendManager.Services
{
    public class ProcessManager : IHostedService
    {
        private readonly IHubContext<ConsoleHub> _hubContext;
        private readonly RabbitMqModuleCatalog _moduleCatalog;
        private readonly ILogger<ProcessManager> _logger;
        private readonly ConcurrentDictionary<string, RunningModule> _runningModules = new(StringComparer.OrdinalIgnoreCase);

        public ProcessManager(
            IHubContext<ConsoleHub> hubContext,
            RabbitMqModuleCatalog moduleCatalog,
            ILogger<ProcessManager> logger)
        {
            _hubContext = hubContext;
            _moduleCatalog = moduleCatalog;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task<ModuleCommandResult> StartModule(string moduleName)
        {
            if (!_moduleCatalog.IsSupported(moduleName))
            {
                return ModuleCommandResult.NotFound($"Module {moduleName} is not supported.");
            }

            var runningModule = new RunningModule(moduleName);
            if (!_runningModules.TryAdd(moduleName, runningModule))
            {
                await WriteLogAsync(moduleName, "[System] Module is already running.");
                return ModuleCommandResult.Conflict($"Module {moduleName} is already running.");
            }

            await WriteLogAsync(moduleName, "[System] Module started.");

            runningModule.ExecutionTask = Task.Run(
                () => RunModuleAsync(runningModule),
                CancellationToken.None);

            return ModuleCommandResult.Accepted($"Module {moduleName} started.");
        }

        public async Task<ModuleCommandResult> StopModule(string moduleName)
        {
            if (!_moduleCatalog.IsSupported(moduleName))
            {
                return ModuleCommandResult.NotFound($"Module {moduleName} is not supported.");
            }

            if (!_runningModules.TryGetValue(moduleName, out var runningModule))
            {
                await WriteLogAsync(moduleName, "[System] Module is not running.");
                return ModuleCommandResult.NotFound($"Module {moduleName} is not running.");
            }

            await WriteLogAsync(moduleName, "[System] Stop requested.");
            runningModule.CancellationTokenSource.Cancel();

            try
            {
                await runningModule.ExecutionTask;
            }
            catch (OperationCanceledException) when (runningModule.CancellationTokenSource.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Module {ModuleName} stopped with an exception.", moduleName);
            }

            return ModuleCommandResult.Accepted($"Module {moduleName} stop requested.");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            var runningModules = _runningModules.Values.ToArray();
            foreach (var runningModule in runningModules)
            {
                runningModule.CancellationTokenSource.Cancel();
            }

            try
            {
                await Task.WhenAll(runningModules.Select(module => module.ExecutionTask)).WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "An exception occurred while stopping background modules.");
            }
        }

        private async Task RunModuleAsync(RunningModule runningModule)
        {
            try
            {
                var context = new ModuleExecutionContext(
                    runningModule.Name,
                    message => WriteLogAsync(runningModule.Name, message),
                    runningModule.CancellationTokenSource.Token);

                await _moduleCatalog.RunModuleAsync(runningModule.Name, context);
            }
            catch (OperationCanceledException) when (runningModule.CancellationTokenSource.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Module {ModuleName} failed.", runningModule.Name);
                await WriteLogAsync(runningModule.Name, $"[ERROR] {ex.Message}");
            }
            finally
            {
                if (_runningModules.TryRemove(runningModule.Name, out var removedModule))
                {
                    removedModule.CancellationTokenSource.Dispose();
                }
                else
                {
                    runningModule.CancellationTokenSource.Dispose();
                }

                await WriteLogAsync(runningModule.Name, "[System] Module stopped.");
            }
        }

        private async Task WriteLogAsync(string moduleName, string message)
        {
            _logger.LogInformation("{ModuleName}: {Message}", moduleName, message);

            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveLog", moduleName, message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast log for {ModuleName}.", moduleName);
            }
        }

        private sealed class RunningModule
        {
            public RunningModule(string name)
            {
                Name = name;
                CancellationTokenSource = new CancellationTokenSource();
            }

            public string Name { get; }
            public CancellationTokenSource CancellationTokenSource { get; }
            public Task ExecutionTask { get; set; } = Task.CompletedTask;
        }
    }
}
