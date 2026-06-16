# Reliable Services Platform (Azure Service Fabric)

This is a mini workload-orchestration control plane deployed to a local Service Fabric cluster. It mirrors the core architecture of a platform team's charter:

- **WorkloadGateway**: A stateless front door (ASP.NET Core REST API) that computes deterministic partitions and routes requests to backend partitions.
- **WorkloadManager**: A stateful service that persists workloads in Reliable Collections and drives a pure state machine to advance them from `Submitted` to `Completed`.
- **CapacityService**: A stateful service tracking available capacity units across nodes.

## High-Level Design
```text
Client (REST) 
      -> WorkloadGateway (Stateless, 1 per node)
      -> WorkloadManager (Stateful, 5 Partitions)
      -> CapacityService (Stateful, 3 Partitions)
```

---

## 🚀 How to Start the Application (macOS)

Because this is a macOS environment running a Docker-based Service Fabric container (`OneBox`), we use the `.NET CLI` to compile the code and the `sfctl` tool to deploy the package.

### 1. Package the Application

Run the following commands from the root of the project to compile the binaries and structure the `ApplicationPackageRoot` folder:

```bash
# Publish binaries
dotnet publish src/CapacityService/CapacityService.csproj -c Release -o ApplicationPackageRoot/CapacityServicePkg/Code
dotnet publish src/WorkloadManager/WorkloadManager.csproj -c Release -o ApplicationPackageRoot/WorkloadManagerPkg/Code
dotnet publish src/WorkloadGateway/WorkloadGateway.csproj -c Release -o ApplicationPackageRoot/WorkloadGatewayPkg/Code

# Copy the ServiceManifests
cp src/CapacityService/PackageRoot/ServiceManifest.xml ApplicationPackageRoot/CapacityServicePkg/
cp src/WorkloadManager/PackageRoot/ServiceManifest.xml ApplicationPackageRoot/WorkloadManagerPkg/
cp src/WorkloadGateway/PackageRoot/ServiceManifest.xml ApplicationPackageRoot/WorkloadGatewayPkg/
```

### 2. Deploy to the Cluster

Ensure you have the Service Fabric CLI installed (`pip3 install sfctl`).

```bash
# Connect to your local cluster
sfctl cluster select --endpoint http://localhost:19080

# Upload the application package
sfctl application upload --path ApplicationPackageRoot --show-progress

# Provision the application type
sfctl application provision --application-type-build-path ApplicationPackageRoot

# Instantiate the application
sfctl application create --app-name fabric:/ReliableServicesPlatform --app-type ReliableServicesPlatformType --app-version 1.0.0
```
Check [http://localhost:19080/Explorer](http://localhost:19080/Explorer) to confirm deployment!

---

## 🛠 How to Use the API

**Check the Gateway Health:**
```bash
curl http://localhost:8080/health
```

**Submit a new Workload:**
Submit a job that requires 20 capacity units. This will return a generated `workloadId`.
```bash
curl -X POST http://localhost:8080/workloads \
     -H "Content-Type: application/json" \
     -d '{"RequiredCapacityUnits": 20}'
```

**Track the Workload's Progress:**
Using the `workloadId` returned above, watch the state machine advance:
```bash
curl http://localhost:8080/workloads/<YOUR-WORKLOAD-ID>
```

---

## 💥 Failover Demonstration

To observe the Service Fabric runtime automatically failing over the primary replica without data loss:
1. Submit a workload.
2. Open Service Fabric Explorer (`http://localhost:19080`).
3. Expand **Applications -> ReliableServicesPlatform -> fabric:/ReliableServicesPlatform/WorkloadManager**.
4. Expand the **Partitions**, pick any partition, and locate the node marked as **Primary**.
5. Click the three dots next to that Primary node and select **Restart Replica**.
6. Issue another `curl GET` to the workload ID. Observe that the workload has progressed correctly, utilizing the state persisted in the Reliable Dictionary!

---

## 🛑 How to Stop & Cleanup

To remove the application from your cluster and clean up the image store:

```bash
# Delete the running application instance
sfctl application delete --application-id ReliableServicesPlatform

# Unprovision the application type from the cluster
sfctl application unprovision --application-type-name ReliableServicesPlatformType --application-type-version 1.0.0

# Delete the package from the cluster's image store
sfctl store delete --content-path ApplicationPackageRoot
```
