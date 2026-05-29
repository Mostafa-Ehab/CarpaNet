using System.Text.Json.Serialization;
using CarpaNet.Storage;

namespace CarpaNet.Auth;

/// <summary>
/// JSON serialization context for session types.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(CreateSessionRequest))]
[JsonSerializable(typeof(SessionResponse))]
[JsonSerializable(typeof(GetSessionResponse))]
[JsonSerializable(typeof(SessionData))]
public partial class SessionJsonContext : JsonSerializerContext
{
}
