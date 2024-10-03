using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using YandexKeyExtractor;
using YandexKeyExtractor.Exceptions;

CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ru-RU");
Console.WriteLine(Localization.Initializing);
using var handler = new WebHandler();

string backup;
try
{
	backup = await RetrieveBackup(handler);
} catch (ResponseFailedException e)
{
	if (e.Status == null)
	{
		Console.WriteLine(Localization.ResponseFailed, e.ResponseName);
	} else
	{
		Console.WriteLine(Localization.ResponseFailedWithDetails, e.Status, e.ResponseName, string.Join(", ", e.Errors ?? []));
	}

	return;
} catch (NoValidBackupException)
{
	Console.WriteLine(Localization.NoValidBackup);

	return;
} catch (Exception e)
{
	Console.WriteLine(Localization.UnknownErrorOccurred, e.Message);

	throw;
}

PromptInput(out var backupPassword, Localization.BackupPasswordVariableName);

Console.WriteLine(Localization.Decrypting);
var message = Decryptor.Decrypt(backup, backupPassword);
if (string.IsNullOrEmpty(message))
{
	Console.WriteLine(Localization.DecryptionFailed);

	return;
}

await File.WriteAllTextAsync("result.txt", message);
Console.WriteLine(Localization.Success, message.AsSpan().Count('\n') + 1);

return;

static async Task<string> RetrieveBackup(WebHandler handler)
{
	var country = await handler.TryGetCountry() ?? "ru";

	PromptInput(out var phoneNumber, Localization.PhoneNumberVariableName);

	phoneNumber = phoneNumber.TrimStart('+');
	var phone = await handler.GetPhoneNumberInfo(phoneNumber, country);

	var trackID = await handler.SendSMSCodeAndGetTrackID(phone, country);

	PromptInput(out var smsCode, Localization.SmsCodeVariableName);

	await handler.CheckCode(smsCode, trackID);
	await handler.ValidateBackupInfo(phone, trackID, country);

	var backup = await handler.GetBackupData(phone, trackID);

	return backup;
}

static void PromptInput(out string result, string argumentName)
{
	Console.WriteLine(Localization.PromptVariable, argumentName.ToLower(CultureInfo.CurrentCulture));
	var input = Console.ReadLine();
	while (string.IsNullOrEmpty(input))
	{
		Console.WriteLine(Localization.InvalidVariableValue, argumentName);
		input = Console.ReadLine();
	}

	result = input;
}
