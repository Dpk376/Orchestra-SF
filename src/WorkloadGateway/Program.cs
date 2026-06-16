using System.Diagnostics;
using System.Fabric;
using Microsoft.ServiceFabric.Services.Runtime;

namespace WorkloadGateway;

internal static class Program
{
    private static void Main()
    {
        try
        {
            ServiceRuntime.RegisterServiceAsync("WorkloadGatewayType",
                context => new WorkloadGateway(context)).GetAwaiter().GetResult();

            ServiceEventSource.Current.ServiceMessage(null!, $"Service host process {Process.GetCurrentProcess().Id} registered service type WorkloadGatewayType");
            Thread.Sleep(Timeout.Infinite);
        }
        catch (Exception e)
        {
            ServiceEventSource.Current.ServiceMessage(null!, $"Service host initialization failed: {e}");
            throw;
        }
    }
}
