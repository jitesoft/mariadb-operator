using System;
using System.Threading.Tasks;
using DotnetKubernetesClient;
using Jitesoft.MariaDBOperator.Finalizers.V1Alpha1;
using Jitesoft.MariaDBOperator.V1Alpha1;
using Jitesoft.MariaDBOperator.V1Alpha1.Builders;
using k8s.Models;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Finalizer;
using KubeOps.Operator.Rbac;
using Microsoft.Extensions.Logging;
using MariaDB = Jitesoft.MariaDBOperator.V1Alpha1.Entities.MariaDB;

namespace Jitesoft.MariaDBOperator.Controllers;

[EntityRbac(typeof(MariaDB), Verbs = RbacVerb.All)]
[EntityRbac(typeof(V1PersistentVolumeClaim), Verbs = RbacVerb.All)]
[EntityRbac(typeof(V1Deployment), Verbs = RbacVerb.All)]
public class MariaDBController : IResourceController<MariaDB>
{
    private readonly ILogger<MariaDBController> _logger;
    private readonly IFinalizerManager<MariaDB> _finalizeManager;
    private readonly IKubernetesClient _client;

    public MariaDBController(ILogger<MariaDBController> logger, IFinalizerManager<MariaDB> finalizeManager, IKubernetesClient client)
    {
        _logger = logger;
        _finalizeManager = finalizeManager;
        _client = client;
    }

    private async Task<MariaDB> Create(MariaDB entity)
    {
        var deployment = DeploymentBuilder.Build(entity);
        if (entity.Spec.DataVolumeClaim != null)
        {
            var pvc = VolumeBuilder.Build(entity);
            if (pvc == null)
            {
                _logger.LogError("Failed to build volume");
                throw new Exception("Failed to build volume from spec");
            }

            await _client.Create(pvc);
            entity.Status.HasVolume = true;
        }

        await _client.Create(deployment);
        return entity;
    }

    private async Task<MariaDB> Update(MariaDB entity)
    {
        if (entity.Status.HasVolume == false && entity.Spec.DataVolumeClaim != null)
        {
            _logger.LogInformation(
                "Creating new volume claim for resource {Name}",
                entity.Name()
            );

            var pvc = VolumeBuilder.Build(entity);
            if (pvc == null)
            {
                _logger.LogError("Failed to build volume");
                throw new Exception("Failed to build volume from spec");
            }

            await _client.Create(pvc);
            entity.Status.HasVolume = true;
        }
        else if (entity.Status.HasVolume && entity.Spec.DataVolumeClaim == null)
        {
            var pvc = await _client.Get<V1PersistentVolumeClaim>(VolumeBuilder.GetPvcName(entity.Name()), entity.Namespace());
            if (pvc == null)
            {
                _logger.LogError("Failed to find existing volume");
                throw new Exception("Failed to remove existing volume");
            }

            await _client.Delete(pvc);
            entity.Status.HasVolume = false;
        }

        var deployment = DeploymentBuilder.Build(entity);
        await _client.Update(deployment);
        return entity;
    }

    private async Task<MariaDB> UpdateStatus(MariaDB entity)
    {
        entity.Status.StateTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        entity.Status.StateTsMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await _client.UpdateStatus(entity);
        return entity;
    }

    public async Task<ResourceControllerResult?> ReconcileAsync(MariaDB entity)
    {
        switch (Enum.Parse<Status>(entity.Status.CurrentState))
        {
            case Status.Stable:
                entity.Status.CurrentState = Status.Updating.ToString();
                entity = await UpdateStatus(entity);
                entity = await Update(entity);
                entity.Status.CurrentState = Status.Stable.ToString();
                await UpdateStatus(entity);
                break;
            case Status.Unknown:
                entity.Status.CurrentState = Status.Creating.ToString();
                entity = await UpdateStatus(entity);
                entity = await Create(entity);
                entity.Status.CurrentState = Status.Stable.ToString();
                entity = await UpdateStatus(entity);
                await _finalizeManager.RegisterFinalizerAsync<MariaDBFinalizer>(entity);
                break;
            case Status.Creating:
            case Status.Starting:
            case Status.Updating:
                _logger.LogInformation(
                    "Resource {Id} is currently in a non-editable state",
                    entity.Name()
                );

                var timeDiff = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - entity.Status.StateTs;
                if (timeDiff > 60 * 5)
                {
                    _logger.LogInformation(
                        "Resource {Name} have been in a non-editable state for {Seconds} seconds, setting state to broken",
                        entity.Name(),
                        timeDiff
                    );

                    entity.Status.CurrentState = Status.Broken.ToString();
                    await UpdateStatus(entity);
                    return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(10));
                }
                else
                {
                    _logger.LogInformation(
                        "Resource {Name} have been in a non-editable state for {Seconds} seconds, waiting for {Time} more seconds",
                            entity.Name(),
                        timeDiff,
                        ((60 * 5) - timeDiff)
                    );

                    return ResourceControllerResult.RequeueEvent(TimeSpan.FromSeconds(Math.Max(10, timeDiff)));
                }
            case Status.Broken:
                _logger.LogInformation("Broken resource {Name} encountered", entity.Name());
                _logger.LogInformation("Will remove any active deployment, but leave PVCs");
                await DeleteBrokenAsync(entity);
                break;
            case Status.Stopped:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return await Task.FromResult<ResourceControllerResult?>(null);
    }

    private async Task DeleteBrokenAsync(MariaDB entity)
    {
        _logger.LogInformation(
            "Removing deployment for {Name} in namespace {Namespace}",
            entity.Name(),
            entity.Namespace()
        );

        entity.Status.CurrentState = Status.Stopped.ToString();
        // Set status to stopped. If it breaks here, it's unrecoverable for now.
        entity = await UpdateStatus(entity);

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
    }

    public Task DeletedAsync(MariaDB entity)
    {
        _logger.LogInformation(
            "{Name} in namespace {Namespace} deleted",
            entity.Name(),
            entity.Namespace()
        );

        return Task.CompletedTask;
    }

    public Task StatusModifiedAsync(MariaDB entity)
    {
        return Task.CompletedTask;
    }
}
