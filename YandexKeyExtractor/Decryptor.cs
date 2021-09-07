using System;
using System.Text;
using CryptSharp.Utility;
using NaCl;

namespace YandexKeyExtractor {
	public static class Decryptor {
		public static string? Decrypt(string encryptedText, string password) {
			string base64Text = NormalizeBase64(encryptedText);

			ReadOnlySpan<byte> textBytes = Convert.FromBase64String(base64Text).AsSpan();

			const byte saltLength = 16;
			ReadOnlySpan<byte> textData = textBytes[..^saltLength];
			ReadOnlySpan<byte> textSalt = textBytes[^saltLength..];

			byte[]? generatedPassword = SCrypt.ComputeDerivedKey(Encoding.UTF8.GetBytes(password), textSalt.ToArray(), 32768, 20, 1, null, 32);

			using XSalsa20Poly1305 secureBox = new(generatedPassword);

			const byte nonceLength = 24;
			ReadOnlySpan<byte> nonce = textData[..nonceLength];
			ReadOnlySpan<byte> dataWithMac = textData[nonceLength..];

			
			Span<byte> message = dataWithMac.Length <= 4096 ? stackalloc byte[dataWithMac.Length] : new byte[dataWithMac.Length];

			const byte macLength = 16;
			ReadOnlySpan<byte> data = dataWithMac[macLength..];
			ReadOnlySpan<byte> mac = dataWithMac[..macLength];

			return secureBox.TryDecrypt(message, data, mac, nonce) ? new string(Encoding.UTF8.GetString(message).TrimEnd('\0')) : null;
		}

		private static string NormalizeBase64(string encryptedText) {
			return encryptedText.Replace('-', '+').Replace('_', '/') + (encryptedText.Length % 4) switch {
				2 => "==",
				3 => "=",
				_ => ""
			};
		}
	}
}
