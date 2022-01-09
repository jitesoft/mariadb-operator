using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using Jitesoft.MariaDBOperator.V1Alpha1.Entities;
using k8s.Models;
using Microsoft.VisualBasic;

namespace Jitesoft.MariaDBOperator.V1Alpha1.Builders;

public static class VolumeBuilder
{
    public const string DataVolumeName = "mariadb-data";
    public const string MariaDbDataPath = "/var/lib/mysql";

    public static string GetPvcName(string resourceName) => $"{resourceName}-pvc";

    public static V1PersistentVolumeClaim? Build(MariaDB resource)
    {
        var volumeData = resource?.Spec?.DataVolumeClaim;
        if (volumeData == null)
        {
            return null;
        }

        var claimMetaData = new V1ObjectMeta
        {
            Name = GetPvcName(resource.Name()),
            NamespaceProperty = resource.Namespace(),
            Annotations = new Dictionary<string, string>
            {
                { "used-by", resource.Name() },
            },
        };

        var claimSpec = new V1PersistentVolumeClaimSpec
        {
            Resources = new()
            {
                Requests = new Dictionary<string, ResourceQuantity>
                {
                    { "storage", new(volumeData.Size) },
                },
            },
            AccessModes = new Collection<string> { "ReadWriteOnce" },
            StorageClassName = volumeData.StorageType,
        };


        return new(spec: claimSpec, metadata: claimMetaData);
    }

}
