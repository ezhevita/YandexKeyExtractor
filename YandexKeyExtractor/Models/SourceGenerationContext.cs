using System.Text.Json.Serialization;

namespace YandexKeyExtractor.Models;

[JsonSerializable(typeof(BackupInfoResponse))]
[JsonSerializable(typeof(BackupResponse))]
[JsonSerializable(typeof(CountryResponse))]
[JsonSerializable(typeof(PhoneNumberResponse))]
[JsonSerializable(typeof(StatusResponse))]
[JsonSerializable(typeof(TrackResponse))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class SourceGenerationContext : JsonSerializerContext
{
}
