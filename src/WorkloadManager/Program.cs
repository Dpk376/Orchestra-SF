using System.Diagnostics;
using System.Fabric;
using Microsoft.ServiceFabric.Services.Runtime;

namespace WorkloadManager;

internal static class Program
{
    private static void Main()
    {
        try
        {
            ServiceRuntime.RegisterServiceAsync("WorkloadManagerType",
                context => new WorkloadManager(context)).GetAwaiter().GetResult();

            ServiceEventSource.Current.ServiceMessage(null!, $"Service host process {Process.GetCurrentProcess().Id} registered service type WorkloadManagerType");
            Thread.Sleep(Timeout.Infinite);
        }
        catch (Exception e)
        {
            ServiceEventSource.Current.ServiceMessage(null!, $"Service host initialization failed: {e}");
            throw;
        }
    }
}
