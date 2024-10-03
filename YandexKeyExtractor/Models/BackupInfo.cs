using System.Text.Json.Serialization;

namespace YandexKeyExtractor.Models;

public class BackupInfo
{
	[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
	[JsonPropertyName("updated")]
	public uint Updated { get; set; }
}
