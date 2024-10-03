using System.Text.Json.Serialization;

namespace YandexKeyExtractor.Models;

public class PhoneNumberInfo
{
	[JsonPropertyName("e164")]
	public string? StandardizedNumber { get; set; }
}
