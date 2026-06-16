using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Shared.Contracts;
using Shared.Models;
using System.Security.Cryptography;
using System.Text;

namespace WorkloadGateway.Controllers;

[ApiController]
[Route("")]
public class WorkloadsController : ControllerBase
{
    [HttpPost("workloads")]
    public async Task<IActionResult> SubmitWorkload([FromBody] SubmitRequest request, CancellationToken cancellationToken)
    {
        if (request.RequiredCapacityUnits <= 0)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                { nameof(request.RequiredCapacityUnits), new[] { "RequiredCapacityUnits must be greater than 0." } }
            }));
        }

        var id = Guid.NewGuid().ToString("N");
        
        long randomKey = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0);
        var randomProxy = ServiceProxy.Create<IWorkloadManager>(
            new Uri("fabric:/ReliableServicesPlatform/WorkloadManager"),
            new ServicePartitionKey(randomKey));

        var createdId = await randomProxy.SubmitAsync(request.RequiredCapacityUnits, cancellationToken);
        return Ok(new { workloadId = createdId });
    }

    [HttpGet("workloads/{id}")]
    public async Task<IActionResult> GetWorkload(string id, CancellationToken cancellationToken)
    {
        var proxy = GetManagerProxy(id);
        var workload = await proxy.GetAsync(id, cancellationToken);
        
        if (workload == null)
        {
            return NotFound();
        }

        return Ok(workload);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "Healthy", message = "WorkloadGateway is reachable." });
    }

    private static IWorkloadManager GetManagerProxy(string workloadId)
    {
        return ServiceProxy.Create<IWorkloadManager>(
            new Uri("fabric:/ReliableServicesPlatform/WorkloadManager"),
            new ServicePartitionKey(PartitionKey(workloadId)));
    }

    private static long PartitionKey(string workloadId)
    {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(workloadId));
        return BitConverter.ToInt64(bytes, 0);
    }
}

/// <summary>
/// The request payload for submitting a new workload.
/// </summary>
public class SubmitRequest
{
    /// <summary>
    /// The number of capacity units required. Must be strictly positive.
    /// </summary>
    public int RequiredCapacityUnits { get; set; }
}
