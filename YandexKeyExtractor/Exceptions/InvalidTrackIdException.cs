using System;

namespace YandexKeyExtractor.Exceptions;

public class InvalidTrackIdException : Exception
{
	public InvalidTrackIdException() : base("Invalid track ID.")
	{
	}
}
