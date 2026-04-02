using BackendManager.Services;
using Microsoft.AspNetCore.SignalR;

namespace BackendManager.Services
{
    public class ConsoleHub : Hub
    {
        // Hubs are transient, so we resolve the singleton ProcessManager via DI in Program.cs
        // Actually, we can inject it via constructor, but then there's a circular dependency if ProcessManager takes IHubContext<ConsoleHub>.
        // Wait, ProcessManager uses IHubContext<ConsoleHub>, which is fine.
        // ConsoleHub doesn't necessarily need ProcessManager here if we use a Controller, or we can use DI in Hub methods via method arguments.
    }
}
