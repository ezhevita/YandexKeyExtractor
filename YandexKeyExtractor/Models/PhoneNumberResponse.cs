using System.Text.Json.Serialization;

namespace YandexKeyExtractor.Models;

public class PhoneNumberResponse : StatusResponse
{
	[JsonPropertyName("number")]
	public PhoneNumberInfo? PhoneNumber { get; set; }
}
