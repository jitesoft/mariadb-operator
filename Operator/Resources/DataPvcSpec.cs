using System.Text.Json.Serialization;
using k8s.Models;

namespace Jitesoft.MariaDBOperator.crd;

public class DataPvcSpec
{
    public const string DefaultAccessMode = "ReadWriteOnce";
    public static string GetPvcName(string resourceName) => $"{resourceName}-pvc";

    [JsonPropertyName(("size"))]
    public string Size { get; set; } = "10G";

    [JsonPropertyName(("storageType"))]
    public string? StorageType { get; set; } = null;

    public const string DataVolumeName = "mariadb-data";
    public const string MariaDbDataPath = "/var/lib/mysql";

    /// <summary>
    /// Helper method to convert the data in the spec into a PVC
    /// ready to send over the api!
    /// </summary>
    /// <param name="resourceName">Name of the MariaDB resource.</param>
    /// <param name="namespaceParameter">Namespace the PersistentVolumeClaim will be created in.</param>
    /// <returns></returns>
    public V1PersistentVolumeClaim ToPvc(string resourceName, string namespaceParameter, string accessMode = DefaultAccessMode)
    {
        var claimMetaData = new V1ObjectMeta
        {
            Name = GetPvcName(resourceName),
            NamespaceProperty = namespaceParameter,
            Annotations = new Dictionary<string, string>
            {
                { "used-by", resourceName },
            },
        };

        var claimSpec = new V1PersistentVolumeClaimSpec
        {
            Resources = new()
            {
                Requests = new Dictionary<string, ResourceQuantity>
                {
                    { "storage", new(Size) },
                },
            },
            AccessModes = { accessMode },
            StorageClassName = StorageType,
        };


        return new(spec: claimSpec, metadata: claimMetaData);
    }
}
