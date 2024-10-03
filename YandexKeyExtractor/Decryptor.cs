using System;
using System.Text;
using CryptSharp.Utility;
using NaCl;

namespace YandexKeyExtractor;

internal static class Decryptor
{
	private const int maxStackallocSize = 4096;

	public static string? Decrypt(string encryptedText, string password)
	{
		var base64Text = NormalizeBase64(encryptedText);

		var textBytes = Convert.FromBase64String(base64Text);

		const byte SaltLength = 16;
		var textData = textBytes.AsSpan()[..^SaltLength];
		var salt = textBytes[^SaltLength..];

		var generatedPassword = SCrypt.ComputeDerivedKey(
			Encoding.UTF8.GetBytes(password),
			salt,
			cost: 32768,
			blockSize: 20,
			parallel: 1,
			maxThreads: null,
			derivedKeyLength: 32
		);

		using XSalsa20Poly1305 secureBox = new(generatedPassword);

		const byte NonceLength = 24;
		var nonce = textData[..NonceLength];
		var dataWithMac = textData[NonceLength..];

		var message = dataWithMac.Length <= maxStackallocSize
			? stackalloc byte[dataWithMac.Length]
			: new byte[dataWithMac.Length];

		const byte MacLength = 16;
		var data = dataWithMac[MacLength..];
		var mac = dataWithMac[..MacLength];

		return secureBox.TryDecrypt(message, data, mac, nonce)
			? new string(Encoding.UTF8.GetString(message).TrimEnd('\0'))
			: null;
	}

	private static string NormalizeBase64(string encryptedText)
	{
		var suffixLength = (encryptedText.Length % 4) switch
		{
			2 => 2,
			3 => 1,
			_ => 0
		};

		var newLength = encryptedText.Length + suffixLength;
		var normalized = newLength <= maxStackallocSize / sizeof(char)
			? stackalloc char[newLength]
			: new char[newLength];

		encryptedText.CopyTo(normalized);
		normalized.Replace('-', '+');
		normalized.Replace('_', '/');

		if (suffixLength > 0)
		{
			normalized[^suffixLength..].Fill('=');
		}

		return new string(normalized);
	}
}
