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

    private static V1Container GetMariaDBContainerSpec(MariaDB.MariaDBSpec spec)
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

    private static V1DeploymentSpec GetDeploymentSpec(MariaDB.MariaDBSpec spec, IDictionary<string, string> labels)
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
                Name = $"{resource.Name()}-deployment",
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
        resource.Status.CurrentStateEnum = Status.Creating;

        await UpdateStatusAsync(resource, cancellationToken);

        _logger.LogInformation("Creating deployment with name {Name} in namespace {Namespace}", deployment.Name(), deployment.Namespace());
        var result = await _client.CreateNamespacedDeploymentAsync(deployment, resource.Namespace(), cancellationToken: cancellationToken);

        _logger.LogDebug("Setting resource to Stable");
        resource.Status.CurrentStateEnum = Status.Stable;
        await UpdateStatusAsync(resource, cancellationToken);
    }

    private async Task Update(MariaDB resource, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Setting resource with name {Name} to Updating status", resource.Name());
        resource.Status.CurrentStateEnum = Status.Updating;

        await UpdateStatusAsync(resource, cancellationToken);

        _logger.LogInformation("Replacing deployment with name {Name} in namespace {Namespace}", resource.Name(), resource.Namespace());
        var result = await UpdateResourceAsync(resource, cancellationToken);

        _logger.LogDebug("Setting resource to Stable");
        resource.Status.CurrentStateEnum = Status.Stable;
        await UpdateStatusAsync(resource, cancellationToken);
    }

    protected override async Task AddOrModifyAsync(MariaDB resource, CancellationToken cancellationToken)
    {
        try
        {
            resource.Status ??= new MariaDB.MariaDBStatus();
            switch (resource.Status.CurrentStateEnum)
            {
                case Status.Unknown:
                    await Create(resource, cancellationToken);
                    break;
                case Status.Stable:
                    await Update(resource, cancellationToken);
                    break;
                case Status.Creating:
                case Status.Updating:
                case Status.Starting:
                case Status.Deleting:
                    _logger.LogInformation("Event to modify or create received for {Resource}, but the resource is in a none-edit state", resource.Name());
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation("Operation canceled");
        }
        catch (Exception ex)
        {
            _logger.LogCritical("{Exception}", ex.Message);
        }
    }

    protected override async Task DeleteAsync(MariaDB resource, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Removing resource with name {Name} in namespace {Namespace}", resource.Name(), resource.Namespace());
        await _client.DeleteNamespacedDeploymentAsync($"{resource.Name()}-deployment", resource.Namespace(), cancellationToken: cancellationToken);
        await base.DeleteAsync(resource, cancellationToken);
        _logger.LogInformation("Resource deleted");
    }
}
