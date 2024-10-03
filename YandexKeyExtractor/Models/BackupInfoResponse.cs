using System.Text.Json.Serialization;

namespace YandexKeyExtractor.Models;

public class BackupInfoResponse : StatusResponse
{
	[JsonPropertyName("backup_info")]
	public BackupInfo? Info { get; set; }
}
