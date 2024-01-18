using System.Text.Json.Serialization;

namespace YandexKeyExtractor.Models;

public class BackupResponse : BackupInfoResponse
{
	[JsonPropertyName("backup")]
	public string? Backup { get; set; }
}
