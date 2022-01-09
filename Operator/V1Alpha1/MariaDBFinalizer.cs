using System;
using System.Threading.Tasks;
using DotnetKubernetesClient;
using Jitesoft.MariaDBOperator.V1Alpha1.Builders;
using Jitesoft.MariaDBOperator.V1Alpha1.Entities;
using k8s.Models;
using KubeOps.Operator.Finalizer;
using Microsoft.Extensions.Logging;

namespace Jitesoft.MariaDBOperator.Finalizers.V1Alpha1;

public class MariaDBFinalizer : IResourceFinalizer<MariaDB>
{
    private readonly ILogger<MariaDBFinalizer> _logger;
    private readonly IKubernetesClient _client;

    public MariaDBFinalizer(ILogger<MariaDBFinalizer> logger, IKubernetesClient client)
    {
        _logger = logger;
        _client = client;
    }

    /// <summary>Finalize a resource that is pending for deletion.</summary>
    /// <param name="entity">The kubernetes entity that needs to be finalized.</param>
    /// <returns>A task for when the operation is done.</returns>
    public async Task FinalizeAsync(MariaDB entity)
    {
        _logger.LogInformation(
            "Starting finalization of resource {Name} in namespace {Namespace}",
            entity.Name(),
            entity.Namespace()
        );

        var dep = await _client.Get<V1Deployment>(
            DeploymentBuilder.GetDeploymentName(entity),
            entity.Namespace()
        );

        if (dep == null)
        {
            _logger.LogError(
                "Failed to find deployment for resource {Name} in namespace {Namespace}",
                entity.Name(),
                entity.Namespace()
            );
            throw new Exception("Failed to find deployment.");
        }

        await _client.Delete(dep);
        _logger.LogInformation(
            "Removed deployment for {Name} in namespace {Namespace}",
            entity.Name(),
            entity.Namespace()
        );

        // Always remove volume _after_ as it is connected to the deployment.
        if (entity.Status.HasVolume)
        {
            _logger.LogDebug("Entity has pvc");
            var pvc = await _client.Get<V1PersistentVolumeClaim>(
                VolumeBuilder.GetPvcName(entity.Name()),
                entity.Namespace()
            );

            if (pvc == null)
            {
                _logger.LogError(
                    "Failed to find PVC for resource {Name} in namespace {Namespace}",
                    entity.Name(),
                    entity.Namespace()
                );
            }
            else
            {
                await _client.Delete(pvc);
            }
        }


    }
}
