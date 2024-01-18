using System;
using System.Text;
using CryptSharp.Utility;
using NaCl;

namespace YandexKeyExtractor;

public static class Decryptor
{
	public static string? Decrypt(string encryptedText, string password)
	{
		var base64Text = NormalizeBase64(encryptedText);

		ReadOnlySpan<byte> textBytes = Convert.FromBase64String(base64Text).AsSpan();

		const byte SaltLength = 16;
		var textData = textBytes[..^SaltLength];
		var textSalt = textBytes[^SaltLength..];

		var generatedPassword = SCrypt.ComputeDerivedKey(
			Encoding.UTF8.GetBytes(password), textSalt.ToArray(), 32768, 20, 1, null, 32
		);

		using XSalsa20Poly1305 secureBox = new(generatedPassword);

		const byte NonceLength = 24;
		var nonce = textData[..NonceLength];
		var dataWithMac = textData[NonceLength..];


		var message = dataWithMac.Length <= 4096 ? stackalloc byte[dataWithMac.Length] : new byte[dataWithMac.Length];

		const byte MacLength = 16;
		var data = dataWithMac[MacLength..];
		var mac = dataWithMac[..MacLength];

		return secureBox.TryDecrypt(message, data, mac, nonce)
			? new string(Encoding.UTF8.GetString(message).TrimEnd('\0'))
			: null;
	}

	private static string NormalizeBase64(string encryptedText)
	{
		return encryptedText.Replace('-', '+').Replace('_', '/') + (encryptedText.Length % 4) switch
		{
			2 => "==",
			3 => "=",
			_ => ""
		};
	}
}
