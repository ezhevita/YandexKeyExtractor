using System.Text.Json.Serialization;

namespace YandexKeyExtractor.Models {
	public class CountryResponse : StatusResponse {
		[JsonPropertyName("country")]
		public string[]? Country { get; set; }
	}
}
