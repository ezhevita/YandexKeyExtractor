using System;

namespace YandexKeyExtractor.Exceptions;

public class NoValidBackupException : Exception
{
	public NoValidBackupException() : base(Localization.NoValidBackup)
	{
	}
}
