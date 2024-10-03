using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace YandexKeyExtractor.Models;

public class CountryResponse : StatusResponse
{
	[JsonPropertyName("country")]
	public IReadOnlyCollection<string>? Country { get; set; }
}
