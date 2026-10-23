using System.Diagnostics;
using System.Security.Cryptography;

namespace AesRsaHomework.Crypto;

public static class RsaCrypto
{
    public static RSA GenerateKeyPair(int keySize = 2048)
    {
        if (keySize < 2048 || keySize % 256 != 0)
            throw new ArgumentOutOfRangeException(nameof(keySize), "Bài tập dùng RSA từ 2048 bit trở lên.");
        return RSA.Create(keySize);
    }

    public static RSA ImportPemFile(string path)
    {
        string pem = File.ReadAllText(path);
        RSA rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(pem);
            return rsa;
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }

    public static byte[] EncryptBytes(byte[] plaintext, RSA publicKey)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ArgumentNullException.ThrowIfNull(publicKey);
        int maxBytes = GetOaepSha256MaximumPlaintext(publicKey);
        if (plaintext.Length > maxBytes)
            throw new CryptographicException($"Dữ liệu dài {plaintext.Length} byte, vượt giới hạn RSA-OAEP-SHA256 là {maxBytes} byte.");
        return publicKey.Encrypt(plaintext, RSAEncryptionPadding.OaepSHA256);
    }

    public static byte[] DecryptBytes(byte[] ciphertext, RSA privateKey)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        ArgumentNullException.ThrowIfNull(privateKey);
        EnsurePrivateKey(privateKey);
        return privateKey.Decrypt(ciphertext, RSAEncryptionPadding.OaepSHA256);
    }

    public static byte[] SignData(byte[] data, RSA privateKey)
    {
        EnsurePrivateKey(privateKey);
        return privateKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    }

    public static bool VerifyData(byte[] data, byte[] signature, RSA publicKey)
    {
        return publicKey.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    }

    public static string Sha256Hex(byte[] data) => Convert.ToHexString(SHA256.HashData(data));

    public static int GetOaepSha256MaximumPlaintext(RSA rsa)
    {
        int modulusBytes = (rsa.KeySize + 7) / 8;
        return modulusBytes - 2 * 32 - 2;
    }

    public static string DescribeKey(RSA rsa)
    {
        RSAParameters publicParameters = rsa.ExportParameters(false);
        var lines = new List<string>
        {
            "Loại khóa: RSA",
            $"Kích thước: {rsa.KeySize} bit",
            $"n (Modulus): {publicParameters.Modulus?.Length ?? 0} byte",
            $"n (hex): {ToHex(publicParameters.Modulus)}",
            $"e (Exponent): {ToHex(publicParameters.Exponent)}",
            $"Giới hạn bản rõ OAEP-SHA256: {GetOaepSha256MaximumPlaintext(rsa)} byte"
        };

        try
        {
            RSAParameters privateParameters = rsa.ExportParameters(true);
            if (privateParameters.D is { Length: > 0 })
            {
                lines.Add("Thành phần private key:");
                lines.Add($"d (hex): {ToHex(privateParameters.D)}");
                lines.Add($"p (hex): {ToHex(privateParameters.P)}");
                lines.Add($"q (hex): {ToHex(privateParameters.Q)}");
                lines.Add($"dP (hex): {ToHex(privateParameters.DP)}");
                lines.Add($"dQ (hex): {ToHex(privateParameters.DQ)}");
                lines.Add($"qInv (hex): {ToHex(privateParameters.InverseQ)}");
            }
            else
            {
                lines.Add("Private key: không có trong khóa đã nạp.");
            }
        }
        catch (CryptographicException)
        {
            lines.Add("Private key: không có trong khóa đã nạp.");
        }
        return string.Join(Environment.NewLine, lines);
    }

    public static (double EncryptMsPerOperation, double DecryptMsPerOperation, int Iterations) Benchmark(
        RSA publicKey, RSA privateKey, byte[] sample, int iterations = 100)
    {
        if (iterations < 1)
            throw new ArgumentOutOfRangeException(nameof(iterations));

        byte[] cipherForDecrypt = EncryptBytes(sample, publicKey);
        byte[] testCipher = Array.Empty<byte>();
        var encryptWatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
            testCipher = EncryptBytes(sample, publicKey);
        encryptWatch.Stop();

        var decryptWatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
            _ = DecryptBytes(cipherForDecrypt, privateKey);
        decryptWatch.Stop();

        if (!CryptographicOperations.FixedTimeEquals(DecryptBytes(testCipher, privateKey), sample))
            throw new CryptographicException("Kiểm tra sau benchmark thất bại.");

        return (encryptWatch.Elapsed.TotalMilliseconds / iterations,
            decryptWatch.Elapsed.TotalMilliseconds / iterations, iterations);
    }

    public static bool HasPrivateKey(RSA rsa)
    {
        try
        {
            RSAParameters parameters = rsa.ExportParameters(true);
            return parameters.D is { Length: > 0 };
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static void EnsurePrivateKey(RSA rsa)
    {
        if (!HasPrivateKey(rsa))
            throw new CryptographicException("Thao tác này yêu cầu private key.");
    }

    private static string ToHex(byte[]? bytes)
    {
        return bytes is null ? "(không có)" : Convert.ToHexString(bytes);
    }
}
