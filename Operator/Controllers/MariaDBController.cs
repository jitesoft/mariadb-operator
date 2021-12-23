using System.Collections.ObjectModel;
using k8s;
using k8s.Models;
using k8s.Operators;
using Jitesoft.MariaDBOperator.Resources;
using Microsoft.Extensions.Logging;

namespace Jitesoft.MariaDBOperator.Controllers;

public class MariaDBController : Controller<MariaDB>
{
    public MariaDBController(OperatorConfiguration configuration, IKubernetes client, ILoggerFactory loggerFactory) :
        base(configuration, client, loggerFactory)
    {
    }

    private static string GetDeploymentName(MariaDB resource) => $"{resource.Name()}-deployment";

    private static V1Container GetMariaDBContainerSpec(MariaDBSpec spec)
    {
        return new V1Container
        {
            Name = "mariadb",
            Image = spec.Image,
            Ports = new Collection<V1ContainerPort>
            {
                new(spec.Port, name: "mysql"),
            },
            Resources = new V1ResourceRequirements
            {
                Limits = spec.Resources?.Limits ??
                         new Dictionary<string, ResourceQuantity>
                         {
                             { "cpu", new ResourceQuantity("500m") },
                             { "memory", new ResourceQuantity("512M") },
                         },
                Requests = spec.Resources?.Requests ?? new Dictionary<string, ResourceQuantity>
                {
                    { "cpu", new ResourceQuantity("200m") },
                    { "memory", new ResourceQuantity("256M") },
                }
            },
            Env = new Collection<V1EnvVar>
            {
                // Backwards compatible!
                new("MYSQL_USER", spec.DbUser),
                new("MYSQL_DATABASE", spec.DbName),
                new("MYSQL_PASSWORD", valueFrom: new V1EnvVarSource
                    {
                        SecretKeyRef = new V1SecretKeySelector
                        {
                            Key = spec.DbPasswordSecretKey,
                            Name = spec.DbPasswordSecretName,
                        }
                    }
                ),
                // Correct ones!
                new("MARIADB_RANDOM_ROOT_PASSWORD", "yes"),
                new("MARIADB_USER", spec.DbUser),
                new("MARIADB_DATABASE", spec.DbName),
                new("MARIADB_PASSWORD", valueFrom: new V1EnvVarSource
                    {
                        SecretKeyRef = new V1SecretKeySelector
                        {
                            Key = spec.DbPasswordSecretKey,
                            Name = spec.DbPasswordSecretName,
                        }
                    }
                ),
            }
        };
    }

    private static V1DeploymentSpec GetDeploymentSpec(MariaDBSpec spec, IDictionary<string, string> labels)
    {
        return new V1DeploymentSpec
        {
            Replicas = 1,
            Selector = new V1LabelSelector(null, labels),
            Template = new V1PodTemplateSpec
            {
                Metadata = new V1ObjectMeta
                {
                    Labels = labels,
                },
                Spec = new V1PodSpec
                {
                    Containers = new Collection<V1Container>
                    {
                        GetMariaDBContainerSpec(spec),
                    }
                }
            }
        };
    }

    private static V1Deployment GetDeploymentResource(MariaDB resource)
    {
        var labels = new Dictionary<string, string>
        {
            { "controlled-by", "mariadb-operator" },
            { "app", "mariadb" },
            { "kind", "database" },
            { "name", resource.Name() }
        };

        return new V1Deployment
        {
            Metadata = new V1ObjectMeta
            {
                Name = GetDeploymentName(resource),
                NamespaceProperty = resource.Namespace(),
                Labels = labels,
            },
            Spec = GetDeploymentSpec(resource.Spec, labels),
        };
    }

    private async Task Create(MariaDB resource, CancellationToken cancellationToken)
    {
        var deployment = GetDeploymentResource(resource);

        _logger.LogDebug("Setting resource with name {Name} to Creating status", resource.Name());
        resource.Status.CurrentState = (int)Status.Creating;

        await UpdateStatusAsync(resource, "mariadb-operator", cancellationToken);

        _logger.LogInformation("Creating deployment with name {Name} in namespace {Namespace}", deployment.Name(), deployment.Namespace());
        var result = await _client.CreateNamespacedDeploymentAsync(deployment, resource.Namespace(), cancellationToken: cancellationToken);

        _logger.LogDebug("Setting resource to Stable");
        resource.Status.CurrentState = (int)Status.Stable;
        await UpdateStatusAsync(resource, "mariadb-operator", cancellationToken);
    }

    private async Task Update(MariaDB resource, CancellationToken cancellationToken)
    {
        var deployment = GetDeploymentResource(resource);
        _logger.LogDebug("Setting resource with name {Name} to Updating status", resource.Name());
        resource.Status.CurrentState = (int)Status.Updating;

        await UpdateStatusAsync(resource, "mariadb-operator", cancellationToken);

        _logger.LogInformation("Replacing deployment with name {Name} in namespace {Namespace}", resource.Name(), resource.Namespace());
        var result = await _client.ReplaceNamespacedDeploymentAsync(deployment, GetDeploymentName(resource), resource.Namespace(), cancellationToken: cancellationToken);

        _logger.LogDebug("Setting resource to Stable");
        resource.Status.CurrentState = (int)Status.Stable;
        await UpdateStatusAsync(resource, "mariadb-operator", cancellationToken);
    }

    protected override async Task AddOrModifyAsync(MariaDB resource, CancellationToken cancellationToken)
    {
        try
        {
            resource.Status ??= new MariaDBStatus();
            switch (resource.Status.CurrentState)
            {
                case (int)Status.Unknown:
                    await Create(resource, cancellationToken);
                    break;
                case (int)Status.Stable:
                    await Update(resource, cancellationToken);
                    break;
                case (int)Status.Creating:
                case (int)Status.Updating:
                case (int)Status.Starting:
                case (int)Status.Deleting:
                    _logger.LogInformation("Event to modify or create received for {Resource}, but the resource is in a none-edit state", resource.Name());
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resource.Status.CurrentState), "Invalid state");
            }
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation("Operation canceled");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "{Exception}", ex.Message);
        }
    }

    protected override async Task DeleteAsync(MariaDB resource, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Removing resource with name {Name} in namespace {Namespace}", resource.Name(), resource.Namespace());
        await _client.DeleteNamespacedDeploymentAsync(GetDeploymentName(resource), resource.Namespace(), cancellationToken: cancellationToken);
        _logger.LogInformation("Resource deleted");
    }
}
