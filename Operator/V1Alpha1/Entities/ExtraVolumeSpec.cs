using System.Text.Json.Serialization;
using k8s.Models;

namespace Jitesoft.MariaDBOperator.V1Alpha1.Entities;

public class ExtraVolumeSpec
{
    /// <summary>
    /// Volume to mount to primary container.
    /// </summary>
    [JsonPropertyName("volume")]
    public V1Volume Volume { get; set; } = null!;

    /// <summary>
    /// Mount for volume in primary container.
    /// </summary>
    [JsonPropertyName("mount")]
    public V1VolumeMount Mount { get; set; } = null!;
}
