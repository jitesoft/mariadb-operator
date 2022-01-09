using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AutoMapper.Internal;
using Jitesoft.MariaDBOperator.V1Alpha1.Entities;
using k8s.Models;

namespace Jitesoft.MariaDBOperator.V1Alpha1.Builders;

public static class DeploymentBuilder
{
    public static V1Deployment Build(MariaDB resource)
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
            Spec = GetDeploymentSpec(resource, labels),
        };
    }

    public static string GetDeploymentName(MariaDB resource) => $"{resource.Name()}-deployment";

    private static V1Container GetMariaDBContainerSpec(MariaDB resource)
    {
        var spec = resource.Spec;

        var envVariables = new List<V1EnvVar>()
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
        };
        envVariables.AddRange(spec.AdditionalEnvironmentVariables);

        var container = new V1Container
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
            Env = envVariables,
        };

        container.VolumeMounts = new Collection<V1VolumeMount>();
        spec.AdditionalVolumes.ToList().ForEach(v => container.VolumeMounts.Add(v.Mount));

        if (spec.DataVolumeClaim != null)
        {
            container.VolumeMounts.Add(new()
            {
                Name = VolumeBuilder.DataVolumeName,
                MountPath = VolumeBuilder.MariaDbDataPath,
                ReadOnlyProperty = false,
            });
        }

        return container;
    }

    private static V1DeploymentSpec GetDeploymentSpec(MariaDB resource, IDictionary<string, string> labels)
    {
        resource.Spec.Labels.ForAll(kvp => labels.TryAdd(kvp.Key, kvp.Value));
        var deploymentSpec = new V1DeploymentSpec
        {
            Replicas = 1,
            Selector = new V1LabelSelector(null, labels),
            Template = new V1PodTemplateSpec
            {
                Metadata = new V1ObjectMeta
                {
                    Labels = labels,
                    Annotations = resource.Spec.Annotations,
                },
                Spec = new V1PodSpec
                {
                    Containers = new Collection<V1Container>
                    {
                        GetMariaDBContainerSpec(resource),
                    }
                }
            }
        };

        deploymentSpec.Template.Spec.Volumes = new Collection<V1Volume>();
        resource.Spec.AdditionalVolumes.ToList().ForEach(
            v => deploymentSpec.Template.Spec.Volumes.Add(v.Volume)
        );

        if (resource.Spec.DataVolumeClaim != null)
        {
            deploymentSpec.Template.Spec.Volumes.Add(
                new()
                {
                    Name = VolumeBuilder.DataVolumeName,
                    PersistentVolumeClaim = new(
                        DataPvcSpec.GetPvcName(resource.Name()),
                        false
                    ),
                }
            );
        }

        return deploymentSpec;
    }

}
