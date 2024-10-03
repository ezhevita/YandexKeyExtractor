using System;
using System.Collections.Generic;

namespace YandexKeyExtractor.Exceptions;

public class ResponseFailedException : Exception
{
	public string ResponseName { get; }
	public string? Status { get; }
	public IReadOnlyCollection<string>? Errors { get; }

	public ResponseFailedException(string responseName) : base($"{responseName} failed.")
	{
		ResponseName = responseName;
	}

	public ResponseFailedException(string responseName, string? status, IReadOnlyCollection<string>? errors) : this(responseName)
	{
		Status = status;
		Errors = errors;
	}
}
