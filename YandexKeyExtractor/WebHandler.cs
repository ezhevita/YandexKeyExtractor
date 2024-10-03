using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using YandexKeyExtractor.Exceptions;
using YandexKeyExtractor.Models;

namespace YandexKeyExtractor;

public sealed class WebHandler : IDisposable
{
	private readonly HttpClient _client = new()
	{
		DefaultRequestHeaders = {UserAgent = {new ProductInfoHeaderValue("okhttp", "2.7.5")}},
		BaseAddress = new Uri("https://registrator.mobile.yandex.net/1/")
	};

	private readonly JsonSerializerOptions _jsonSettings = new()
	{
		TypeInfoResolver = SourceGenerationContext.Default
	};

	public async Task CheckCode(string smsCode, string trackID)
	{
		var checkCodeResponse = await PostUrlEncodedAndReceiveJson<StatusResponse>(
			new Uri("bundle/yakey_backup/check_code/", UriKind.Relative),
			new Dictionary<string, string>(2) {["code"] = smsCode, ["track_id"] = trackID});

		ValidateResponse(checkCodeResponse);
	}

	public async Task<string> GetBackupData(string phone, string trackID)
	{
		var backupResponse = await PostUrlEncodedAndReceiveJson<BackupResponse>(
			new Uri("bundle/yakey_backup/download", UriKind.Relative),
			new Dictionary<string, string>(2) {["number"] = phone, ["track_id"] = trackID});

		ValidateResponse(backupResponse);

		if (string.IsNullOrEmpty(backupResponse.Backup))
		{
			throw new NoValidBackupException();
		}

		return backupResponse.Backup;
	}

	public async Task<string> GetPhoneNumberInfo(string phoneNumber, string country)
	{
		var phoneNumberResponse = await PostUrlEncodedAndReceiveJson<PhoneNumberResponse>(
			new Uri("bundle/validate/phone_number/", UriKind.Relative),
			new Dictionary<string, string>(2) {["phone_number"] = phoneNumber, ["country"] = country});

		var phone = phoneNumberResponse?.PhoneNumber?.StandardizedNumber ?? $"+{phoneNumber}";

		return phone;
	}

	public async Task<string> SendSMSCodeAndGetTrackID(string phone, string country)
	{
		var trackResponse = await PostUrlEncodedAndReceiveJson<TrackResponse>(
			new Uri("bundle/yakey_backup/send_code/", UriKind.Relative),
			new Dictionary<string, string>(3) {["display_language"] = "en", ["number"] = phone, ["country"] = country});

		ValidateResponse(trackResponse);

		var trackID = trackResponse.TrackID;
		if (string.IsNullOrEmpty(trackID))
		{
			throw new InvalidTrackIdException();
		}

		return trackID;
	}

	public async Task<string?> TryGetCountry()
	{
		var countryResponse = await _client.GetFromJsonAsync<CountryResponse>(new Uri("suggest/country", UriKind.Relative));

		return countryResponse?.Country?.FirstOrDefault();
	}

	public async Task ValidateBackupInfo(string phone, string trackID, string country)
	{
		var backupInfoResponse = await PostUrlEncodedAndReceiveJson<BackupInfoResponse>(
			new Uri("bundle/yakey_backup/info/", UriKind.Relative),
			new Dictionary<string, string>(3) {["number"] = phone, ["track_id"] = trackID, ["country"] = country});

		ValidateResponse(backupInfoResponse);

		if (backupInfoResponse.Info?.Updated == null)
		{
			throw new NoValidBackupException();
		}
	}

	private async Task<T?> PostUrlEncodedAndReceiveJson<T>(Uri url, Dictionary<string, string> data)
	{
		using var content = new FormUrlEncodedContent(data);
		using var responseMessage = await _client.PostAsync(url, content);
		responseMessage.EnsureSuccessStatusCode();

		return (await responseMessage.Content.ReadFromJsonAsync<T>(_jsonSettings))!;
	}

	private static void ValidateResponse<T>([NotNull] T? response,
		[CallerArgumentExpression(nameof(response))] string responseName = "") where T : StatusResponse
	{
		if (response == null)
		{
			throw new ResponseFailedException(responseName);
		}

		if (!response.IsSuccess)
		{
			throw new ResponseFailedException(responseName, response.Status, response.Errors);
		}
	}

	public void Dispose()
	{
		_client.Dispose();
	}
}
