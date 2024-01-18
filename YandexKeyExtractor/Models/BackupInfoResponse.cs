using System.Text.Json.Serialization;

namespace YandexKeyExtractor.Models;

public class BackupInfoResponse : StatusResponse
{
	[JsonPropertyName("backup_info")]
	public BackupInfo? Info { get; set; }

	public class BackupInfo
	{
		[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
		[JsonPropertyName("updated")]
		public uint Updated { get; set; }
	}
}
