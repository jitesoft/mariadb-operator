using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Jitesoft.MariaDBOperator.Resources;

public class MariaDBStatus
{
    [JsonPropertyName("currentState")]
    public int CurrentState { get; set; } = (int)Status.Unknown;
}
