using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace YandexKeyExtractor {
	internal static class Program {
		private static async Task Main() {
			Console.WriteLine("Initializing...");
			using WebHandler handler = WebHandler.Create();

			string country = await handler.TryGetCountry().ConfigureAwait(false);

			PromptInput(out string phoneNumber, nameof(phoneNumber));

			phoneNumber = phoneNumber.TrimStart('+');
			string phone = await handler.GetPhoneNumberInfo(phoneNumber, country).ConfigureAwait(false);

			string? trackID = await handler.SendSMSCodeAndGetTrackID(phone, country).ConfigureAwait(false);
			if (string.IsNullOrEmpty(trackID)) {
				return;
			}

			PromptInput(out string smsCode, nameof(smsCode));

			if (!await handler.CheckCode(smsCode, trackID).ConfigureAwait(false)) {
				return;
			}

			if (!await handler.ValidateBackupInfo(phone, trackID, country).ConfigureAwait(false)) {
				return;
			}

			string? backup = await handler.GetBackupData(phone, trackID).ConfigureAwait(false);
			if (string.IsNullOrEmpty(backup)) {
				return;
			}

			PromptInput(out string backupPassword, nameof(backupPassword));

			Console.WriteLine("Decrypting...");
			string? message = Decryptor.Decrypt(backup, backupPassword);
			if (string.IsNullOrEmpty(message)) {
				Console.WriteLine("Decryption failed!");

				return;
			}

			Console.WriteLine("Successfully decrypted!");
			await File.WriteAllTextAsync("result.txt", message).ConfigureAwait(false);
			Console.WriteLine($"Written {message.Split('\n').Length} authenticators to result file");
		}

		private static void PromptInput(out string result, [CallerArgumentExpression("result")] string argumentName = "") {
			Console.WriteLine($"Enter {argumentName}:");
			string? input = Console.ReadLine();
			while (string.IsNullOrEmpty(input)) {
				Console.WriteLine($"{argumentName} is invalid, try again:");
				input = Console.ReadLine();
			}

			result = input;
		}
	}
}
