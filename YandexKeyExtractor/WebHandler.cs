using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;
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

	public async Task CheckCode(string smsCode, string trackID)
	{
		var checkCodeResponse = await PostUrlEncodedAndReceiveJson(
			new Uri("bundle/yakey_backup/check_code/", UriKind.Relative),
			new Dictionary<string, string>(2) {["code"] = smsCode, ["track_id"] = trackID},
			static context => context.StatusResponse);

		ValidateResponse(checkCodeResponse);
	}

	public async Task<string> GetBackupData(string phone, string trackID)
	{
		var backupResponse = await PostUrlEncodedAndReceiveJson(
			new Uri("bundle/yakey_backup/download", UriKind.Relative),
			new Dictionary<string, string>(2) {["number"] = phone, ["track_id"] = trackID},
			static context => context.BackupResponse);

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
			new Dictionary<string, string>(2) {["phone_number"] = phoneNumber, ["country"] = country},
			static context => context.PhoneNumberResponse);

		var phone = phoneNumberResponse?.PhoneNumber?.StandardizedNumber ?? $"+{phoneNumber}";

		return phone;
	}

	public async Task<string> SendSMSCodeAndGetTrackID(string phone, string country)
	{
		var trackResponse = await PostUrlEncodedAndReceiveJson(
			new Uri("bundle/yakey_backup/send_code/", UriKind.Relative),
			new Dictionary<string, string>(3) {["display_language"] = "en", ["number"] = phone, ["country"] = country},
			static context => context.TrackResponse);

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
		var countryResponse = await _client.GetFromJsonAsync(
			new Uri("suggest/country", UriKind.Relative), SourceGenerationContext.Default.CountryResponse);

		return countryResponse?.Country?.FirstOrDefault();
	}

	public async Task ValidateBackupInfo(string phone, string trackID, string country)
	{
		var backupInfoResponse = await PostUrlEncodedAndReceiveJson(
			new Uri("bundle/yakey_backup/info/", UriKind.Relative),
			new Dictionary<string, string>(3) {["number"] = phone, ["track_id"] = trackID, ["country"] = country},
			static context => context.BackupInfoResponse);

		ValidateResponse(backupInfoResponse);

		if (backupInfoResponse.Info?.Updated == null)
		{
			throw new NoValidBackupException();
		}
	}

	private async Task<T?> PostUrlEncodedAndReceiveJson<T>(Uri url, Dictionary<string, string> data,
		Func<SourceGenerationContext, JsonTypeInfo<T>> typeInfoProvider)
	{
		using var content = new FormUrlEncodedContent(data);
		using var responseMessage = await _client.PostAsync(url, content);
		responseMessage.EnsureSuccessStatusCode();

		return (await responseMessage.Content.ReadFromJsonAsync(typeInfoProvider(SourceGenerationContext.Default)))!;
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
