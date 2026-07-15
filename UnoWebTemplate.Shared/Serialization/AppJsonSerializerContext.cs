using System.Text.Json;
using System.Text.Json.Serialization;
using UnoWebTemplate.Shared.Models;

namespace UnoWebTemplate.Shared.Serialization
{
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(StatusResponse))]
    [JsonSerializable(typeof(JsonElement))]
    public partial class AppJsonSerializerContext : JsonSerializerContext
    {
    }
}
