namespace BackendManager.Services
{
    public sealed class ModuleExecutionContext
    {
        private readonly Func<string, Task> _writeLogAsync;

        public ModuleExecutionContext(
            string moduleName,
            Func<string, Task> writeLogAsync,
            CancellationToken cancellationToken)
        {
            ModuleName = moduleName;
            _writeLogAsync = writeLogAsync;
            CancellationToken = cancellationToken;
        }

        public string ModuleName { get; }
        public CancellationToken CancellationToken { get; }

        public Task WriteLogAsync(string message)
        {
            return _writeLogAsync(message);
        }
    }
}
