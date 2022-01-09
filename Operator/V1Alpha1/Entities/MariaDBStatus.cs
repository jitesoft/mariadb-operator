using System.Text.Json.Serialization;

namespace Jitesoft.MariaDBOperator.V1Alpha1.Entities;

public class MariaDBStatus
{
    [JsonPropertyName("currentState")]
    public string CurrentState { get; set; } = Status.Unknown.ToString();

    [JsonPropertyName("hasVolume")]
    public bool HasVolume { get; set; } = false;

    [JsonPropertyName("stateTsMs")]
    public long StateTsMs { get; set; } = 0;

    [JsonPropertyName("stateTs")]
    public long StateTs { get; set; } = 0;
}
