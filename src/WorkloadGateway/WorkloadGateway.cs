using System.Fabric;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace WorkloadGateway;

internal sealed class WorkloadGateway : StatelessService
{
    public WorkloadGateway(StatelessServiceContext context)
        : base(context)
    { }

    protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
    {
        return new ServiceInstanceListener[]
        {
            new ServiceInstanceListener(serviceContext =>
                new KestrelCommunicationListener(serviceContext, "GatewayEndpoint", (url, listener) =>
                {
                    ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                    var builder = WebApplication.CreateBuilder();

                    builder.Services.AddSingleton<StatelessServiceContext>(serviceContext);
                    builder.Services.AddControllers();

                    var app = builder.Build();

                    app.MapControllers();

                    return app;
                }))
        };
    }
}

internal sealed class ServiceEventSource
{
    public static readonly ServiceEventSource Current = new ServiceEventSource();
    public void ServiceMessage(ServiceContext context, string message, params object[] args)
    {
        Console.WriteLine($"[WorkloadGateway] {context.ServiceName} - {string.Format(message, args)}");
    }
}
