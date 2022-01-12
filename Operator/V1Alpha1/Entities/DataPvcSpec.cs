using System.Text.Json.Serialization;

namespace Jitesoft.MariaDBOperator.V1Alpha1.Entities;

public class DataPvcSpec
{
    public const string DefaultAccessMode = "ReadWriteOnce";
    public static string GetPvcName(string resourceName) => $"{resourceName}-pvc";

    [JsonPropertyName("size")]
    public string Size { get; set; } = "10G";

    [JsonPropertyName("storageType")]
    public string? StorageType { get; set; } = null;
}
