using System.Text.Json.Serialization;

namespace YandexKeyExtractor.Models {
	public class StatusResponse {
		[JsonPropertyName("status")]
		public string? Status { get; set; }

		[JsonPropertyName("errors")]
		public string[]? Errors { get; set; }

		public bool IsSuccess => Status == "ok";
	}
}
