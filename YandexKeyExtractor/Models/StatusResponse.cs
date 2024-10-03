using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace YandexKeyExtractor.Models;

public class StatusResponse
{
	[JsonPropertyName("status")]
	public string? Status { get; set; }

	[JsonPropertyName("errors")]
	public IReadOnlyCollection<string>? Errors { get; set; }

	public bool IsSuccess => Status == "ok";
}
