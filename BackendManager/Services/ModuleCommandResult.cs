using Microsoft.AspNetCore.Http;

namespace BackendManager.Services
{
    public sealed record ModuleCommandResult(int StatusCode, string Message)
    {
        public static ModuleCommandResult Accepted(string message)
            => new(StatusCodes.Status202Accepted, message);

        public static ModuleCommandResult Conflict(string message)
            => new(StatusCodes.Status409Conflict, message);

        public static ModuleCommandResult NotFound(string message)
            => new(StatusCodes.Status404NotFound, message);
    }
}
