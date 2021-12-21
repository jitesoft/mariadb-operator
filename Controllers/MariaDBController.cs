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
                Requests =
                {
                    { "cpu", new ResourceQuantity("200m") },
                    { "memory", new ResourceQuantity("256M") },
                }
            },
            Env = new Collection<V1EnvVar>
            {
                new V1EnvVar("MARIADB_RANDOM_ROOT_PASSWORD", "yes"),
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
                    SecurityContext = new V1PodSecurityContext
                    {
                        RunAsNonRoot = true,
                    },
                    Containers = new Collection<V1Container>
                    {
                        GetMariaDBContainerSpec(spec),
                    }
                }
            }
        };
    }


    protected override async Task AddOrModifyAsync(MariaDB resource, CancellationToken cancellationToken)
    {
        try
        {
            var labels = new Dictionary<string, string>
            {
                { "controlled-by", "mariadb-operator" },
                { "app", "mariadb" },
                { "kind", "database" },
                { "name", resource.Name() }
            };

            var deployment = new V1Deployment
            {
                Metadata = new V1ObjectMeta
                {
                    Name = $"{resource.Name()}-deployment",
                    NamespaceProperty = resource.Namespace(),
                    Labels = labels,
                },
                Spec = GetDeploymentSpec(resource.Spec, labels),
            };


            await UpdateResourceAsync(resource, cancellationToken);
            //this._client.CreateNamespacedDeploymentAsync(deployment, cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogInformation("Operation canceled");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex.Message);
        }
    }

    protected override Task DeleteAsync(MariaDB resource, CancellationToken cancellationToken)
    {
        return base.DeleteAsync(resource, cancellationToken);
    }
}
