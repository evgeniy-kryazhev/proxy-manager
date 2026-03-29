using System.Security.Cryptography;
using ProxyManager.Application.Abstractions;

namespace ProxyManager.Infrastructure.Runtime;

public sealed class CryptographicPasswordGenerator : IPasswordGenerator
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_=+[]{}";

    public string GenerateStrongPassword()
    {
        Span<byte> random = stackalloc byte[24];
        RandomNumberGenerator.Fill(random);

        var chars = new char[24];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[random[i] % Alphabet.Length];
        }

        return new string(chars);
    }
}
