using System.Text.Json.Serialization;

namespace YandexKeyExtractor.Models;

public class TrackResponse : StatusResponse
{
	[JsonPropertyName("track_id")]
	public string? TrackID { get; set; }
}
