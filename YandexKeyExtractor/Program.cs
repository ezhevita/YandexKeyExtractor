using System;
using System.IO;
using YandexKeyExtractor;

Console.WriteLine("Initializing...");
using var handler = WebHandler.Create();

var country = await handler.TryGetCountry();

PromptInput(out var phoneNumber, "phone number");

phoneNumber = phoneNumber.TrimStart('+');
var phone = await handler.GetPhoneNumberInfo(phoneNumber, country);

var trackID = await handler.SendSMSCodeAndGetTrackID(phone, country);
if (string.IsNullOrEmpty(trackID))
{
	return;
}

PromptInput(out var smsCode, "SMS code");

if (!await handler.CheckCode(smsCode, trackID))
{
	return;
}

if (!await handler.ValidateBackupInfo(phone, trackID, country))
{
	return;
}

var backup = await handler.GetBackupData(phone, trackID);
if (string.IsNullOrEmpty(backup))
{
	return;
}

PromptInput(out var backupPassword, "backup password");

Console.WriteLine("Decrypting...");
var message = Decryptor.Decrypt(backup, backupPassword);
if (string.IsNullOrEmpty(message))
{
	Console.WriteLine("Decryption failed! Most likely the password is wrong");

	return;
}

Console.WriteLine("Successfully decrypted!");
await File.WriteAllTextAsync("result.txt", message);
Console.WriteLine($"Written {message.Split('\n').Length} authenticators to the file (result.txt)");

return;

static void PromptInput(out string result, string argumentName = "")
{
	Console.WriteLine($"Enter {argumentName}:");
	var input = Console.ReadLine();
	while (string.IsNullOrEmpty(input))
	{
		Console.WriteLine($"{argumentName} is invalid, try again:");
		input = Console.ReadLine();
	}

	result = input;
}
