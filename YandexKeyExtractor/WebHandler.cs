using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Flurl.Http;
using Flurl.Serialization.TextJson;
using YandexKeyExtractor.Models;

namespace YandexKeyExtractor;

public sealed class WebHandler : IDisposable
{
	private WebHandler(IFlurlClient client) => Client = client;
	private IFlurlClient Client { get; }

	public void Dispose()
	{
		Client.Dispose();
	}

	public async Task<bool> CheckCode(string? smsCode, string? trackID)
	{
		var checkCodeResponse = await Client.Request("/bundle/yakey_backup/check_code/")
			.PostUrlEncodedAsync(
				new
				{
					code = smsCode,
					track_id = trackID
				}
			)
			.ReceiveJson<StatusResponse?>();

		return ValidateResponse(checkCodeResponse, nameof(checkCodeResponse));
	}

	public static WebHandler Create()
	{
		JsonSerializerOptions jsonSettings = new()
		{
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
		};

		var client = new FlurlClient(
			new HttpClient
			{
				DefaultRequestHeaders = {UserAgent = {new ProductInfoHeaderValue("okhttp", "2.7.5")}},
				BaseAddress = new Uri("https://registrator.mobile.yandex.net/1/")
			}
		).Configure(settings => settings.WithTextJsonSerializer(jsonSettings));

		return new WebHandler(client);
	}

	public async Task<string?> GetBackupData(string phone, string? trackID)
	{
		var backupResponse = await Client.Request("/bundle/yakey_backup/download")
			.PostUrlEncodedAsync(
				new
				{
					number = phone,
					track_id = trackID
				}
			)
			.ReceiveJson<BackupResponse?>();

		if (!ValidateResponse(backupResponse, nameof(backupResponse)))
		{
			return null;
		}

		if (string.IsNullOrEmpty(backupResponse.Backup))
		{
			Console.WriteLine("Fatal error - Couldn't find valid backup!");

			return null;
		}

		return backupResponse.Backup;
	}

	public async Task<string> GetPhoneNumberInfo(string? phoneNumber, string country)
	{
		var phoneNumberResponse = await Client.Request("/bundle/validate/phone_number/")
			.PostUrlEncodedAsync(
				new
				{
					phone_number = phoneNumber,
					country
				}
			).ReceiveJson<PhoneNumberResponse?>();

		ValidateResponse(phoneNumberResponse, nameof(phoneNumberResponse));

		var phone = phoneNumberResponse?.PhoneNumber?.StandardizedNumber ?? '+' + phoneNumber;

		return phone;
	}

	public async Task<string?> SendSMSCodeAndGetTrackID(string phone, string country)
	{
		var trackResponse = await Client.Request("/bundle/yakey_backup/send_code/")
			.PostUrlEncodedAsync(
				new
				{
					display_language = "en",
					number = phone,
					country
				}
			)
			.ReceiveJson<TrackResponse?>();

		if (!ValidateResponse(trackResponse, nameof(trackResponse)))
		{
			return null;
		}

		var trackID = trackResponse.TrackID;
		if (string.IsNullOrEmpty(trackID))
		{
			Console.WriteLine("Track ID is empty!");

			return null;
		}

		return trackID;
	}

	public async Task<string> TryGetCountry()
	{
		var countryResponse = await Client.Request("/suggest/country")
			.GetAsync()
			.ReceiveJson<CountryResponse?>();

		ValidateResponse(countryResponse, nameof(countryResponse));

		var country = countryResponse?.Country?.FirstOrDefault() ?? "ru";

		return country;
	}

	public async Task<bool> ValidateBackupInfo(string phone, string? trackID, string country)
	{
		var backupInfoResponse = await Client.Request("/bundle/yakey_backup/info/")
			.PostUrlEncodedAsync(
				new
				{
					number = phone,
					track_id = trackID,
					country
				}
			)
			.ReceiveJson<BackupInfoResponse?>();

		if (!ValidateResponse(backupInfoResponse, nameof(backupInfoResponse)))
		{
			return false;
		}

		if (backupInfoResponse.Info?.Updated == null)
		{
			Console.WriteLine("Fatal error - Couldn't find valid backup!");

			return false;
		}

		return true;
	}


	private static bool ValidateResponse<T>([NotNullWhen(true)] T? response,
		[CallerArgumentExpression("response")] string responseName = "") where T : StatusResponse
	{
		if (response == null)
		{
			Console.WriteLine(responseName + " failed!");

			return false;
		}

		if (!response.IsSuccess)
		{
			Console.WriteLine(responseName + $" failed with error {response.Status}!");
			if (response.Errors != null)
			{
				Console.WriteLine("Errors: " + string.Join(',', response.Errors));
			}

			return false;
		}

		return true;
	}
}
