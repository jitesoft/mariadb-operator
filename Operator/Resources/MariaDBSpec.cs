using System.Text.Json.Serialization;
using k8s.Models;
using Newtonsoft.Json;

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
    [JsonProperty("image")]
    [JsonPropertyName("image")]
    public string Image { get; set; } = "mariadb:10.7";

    /// <summary>
    /// Port to expose in deployment.
    /// </summary>
    [JsonProperty("port")]
    [JsonPropertyName("port")]
    public int Port { get; set; } = 3306;

    /// <summary>
    /// Optional resource limits and requests.
    /// Defaults to limit.cpu: 500m, limit.memory: 512M, requests.cpu: 200m, requests.memory: 256M
    /// </summary>
    [JsonProperty("resources")]
    [JsonPropertyName("resources")]
    public V1ResourceRequirements? Resources { get; set; } = new();

    /// <summary>
    /// Database user to create on first startup.
    /// </summary>
    [JsonProperty("dbUser")]
    [JsonPropertyName("dbUser")]
    public string DbUser { get; set; } = null!;

    /// <summary>
    /// Name of database to create on first startup.
    /// </summary>
    [JsonProperty("dbName")]
    [JsonPropertyName("dbName")]
    public string DbName { get; set; } = null!;

    /// <summary>
    /// Name of secret where password for dbUser is stored.
    /// </summary>
    [JsonProperty("dbPasswordSecretName")]
    [JsonPropertyName("dbPasswordSecretName")]
    public string DbPasswordSecretName { get; set; } = null!;

    /// <summary>
    /// Key in DbPasswordSecretName which the password is stored in.
    /// </summary>
    [JsonProperty("dbPasswordSecretKey")]
    [JsonPropertyName("dbPasswordSecretKey")]
    public string DbPasswordSecretKey { get; set; } = "password";
}
