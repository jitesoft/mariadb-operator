using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Jitesoft.MariaDBOperator.crd;
using k8s.Models;

namespace Jitesoft.MariaDBOperator.Resources;

/// <summary>
/// Spec for a single MariaDB instance.
/// </summary>
public class MariaDBSpec
{
    /// <summary>
    /// Image to use in primary container.
    /// Defaults to mariadb:10.7
    /// </summary>
    [JsonPropertyName("image")]
    public string Image { get; set; } = "mariadb:10.7";

    /// <summary>
    /// Port to expose in deployment.
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; } = 3306;

    /// <summary>
    /// Optional resource limits and requests.
    /// Defaults to limit.cpu: 500m, limit.memory: 512M, requests.cpu: 200m, requests.memory: 256M
    /// </summary>
    [JsonPropertyName("resources")]
    public V1ResourceRequirements? Resources { get; set; } = new();

    /// <summary>
    /// Database user to create on first startup.
    /// </summary>
    [JsonPropertyName("dbUser")]
    public string DbUser { get; set; } = null!;

    /// <summary>
    /// Name of database to create on first startup.
    /// </summary>
    [JsonPropertyName("dbName")]
    public string DbName { get; set; } = null!;

    /// <summary>
    /// Name of secret where password for dbUser is stored.
    /// </summary>
    [JsonPropertyName("dbPasswordSecretName")]
    public string DbPasswordSecretName { get; set; } = null!;

    /// <summary>
    /// Key in DbPasswordSecretName which the password is stored in.
    /// </summary>
    [JsonPropertyName("dbPasswordSecretKey")]
    public string DbPasswordSecretKey { get; set; } = "password";

    /// <summary>
    /// Additional mounts to add to the primary container.
    /// </summary>
    [JsonPropertyName("additionalVolumes")]
    public IList<ExtraVolumeSpec> AdditionalVolumes { get; set; } = new List<ExtraVolumeSpec>();

    /// <summary>
    /// Additional environment variables to add to the primary container.
    /// </summary>
    [JsonPropertyName("additionalEnvironmentVariables")]
    public IList<V1EnvVar> AdditionalEnvironmentVariables { get; set; } = new List<V1EnvVar>();

    /// <summary>
    /// Persistent volume claim to use as data storage.
    /// If null, no claim will be created and the storage will be in memory.
    /// </summary>
    [JsonPropertyName("dataVolumeClaim")]
    public DataPvcSpec? DataVolumeClaim { get; set; } = null;

    /// <summary>
    /// Optional extra annotations to add to deployment.
    /// </summary>
    [JsonPropertyName("annotations")]
    public IDictionary<string, string> Annotations { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Optional extra labels to add to deployment.
    /// </summary>
    [JsonPropertyName("labels")]
    public IDictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();

}
