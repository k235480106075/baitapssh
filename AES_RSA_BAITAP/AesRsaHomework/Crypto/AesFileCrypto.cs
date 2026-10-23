using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace AesRsaHomework.Crypto;

/// <summary>
/// AES-256-GCM helpers. Files are encrypted in 1 MiB authenticated chunks so large files
/// can be processed without loading the entire file into memory.
/// File format: 8-byte magic, then repeated [Int32 length][12-byte nonce][ciphertext][16-byte tag],
/// followed by an Int32 length of zero. Each chunk authenticates its sequence number and length.
/// </summary>
public static class AesFileCrypto
{
    public const int KeySizeBytes = 32;
    public const int NonceSizeBytes = 12;
    public const int TagSizeBytes = 16;
    public const int ChunkSizeBytes = 1024 * 1024;

    private static readonly byte[] FileMagic = Encoding.ASCII.GetBytes("AESSAFE1");
    private static readonly byte[] TextAad = Encoding.ASCII.GetBytes("AES-TEXT-V1");

    public static byte[] GenerateKey() => RandomNumberGenerator.GetBytes(KeySizeBytes);

    public static void SaveKey(string path, byte[] key)
    {
        ValidateKey(key);
        File.WriteAllText(path, Convert.ToBase64String(key), new UTF8Encoding(false));
    }

    public static byte[] LoadKey(string path)
    {
        string content = File.ReadAllText(path).Trim();
        byte[] key;
        try
        {
            key = Convert.FromBase64String(content);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Tệp khóa không chứa Base64 hợp lệ.", ex);
        }
        ValidateKey(key);
        return key;
    }

    public static string EncryptText(string plaintext, byte[] key)
    {
        ValidateKey(key);
        byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        byte[] cipherBytes = new byte[plainBytes.Length];
        byte[] tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag, TextAad);

        byte[] result = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, nonce.Length + tag.Length, cipherBytes.Length);
        return Convert.ToBase64String(result);
    }

    public static string DecryptText(string encodedCiphertext, byte[] key)
    {
        ValidateKey(key);
        byte[] packed;
        try
        {
            packed = Convert.FromBase64String(encodedCiphertext.Trim());
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Bản mã văn bản không phải Base64 hợp lệ.", ex);
        }

        if (packed.Length < NonceSizeBytes + TagSizeBytes)
            throw new CryptographicException("Bản mã quá ngắn hoặc không đúng định dạng.");

        byte[] nonce = packed[..NonceSizeBytes];
        byte[] tag = packed[NonceSizeBytes..(NonceSizeBytes + TagSizeBytes)];
        byte[] cipherBytes = packed[(NonceSizeBytes + TagSizeBytes)..];
        byte[] plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes, TextAad);
        return Encoding.UTF8.GetString(plainBytes);
    }

    public static void EncryptFile(string inputPath, string outputPath, byte[] key)
    {
        ValidateKey(key);
        string sourceFull = Path.GetFullPath(inputPath);
        string destinationFull = Path.GetFullPath(outputPath);
        if (string.Equals(sourceFull, destinationFull, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Tệp đầu ra không được trùng với tệp đầu vào.");

        using var input = new FileStream(sourceFull, FileMode.Open, FileAccess.Read, FileShare.Read,
            ChunkSizeBytes, FileOptions.SequentialScan);
        using var output = new FileStream(destinationFull, FileMode.Create, FileAccess.Write, FileShare.None,
            ChunkSizeBytes, FileOptions.SequentialScan);
        using var aes = new AesGcm(key, TagSizeBytes);

        output.Write(FileMagic);
        byte[] plainBuffer = new byte[ChunkSizeBytes];
        long chunkIndex = 0;

        while (true)
        {
            int read = ReadChunk(input, plainBuffer);
            WriteInt32(output, read);
            if (read == 0)
                break;

            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
            byte[] cipher = new byte[read];
            byte[] tag = new byte[TagSizeBytes];
            byte[] aad = BuildChunkAad(chunkIndex, read);
            aes.Encrypt(nonce, plainBuffer.AsSpan(0, read), cipher, tag, aad);

            output.Write(nonce);
            output.Write(cipher);
            output.Write(tag);
            chunkIndex++;
        }
        output.Flush(true);
    }

    public static void DecryptFile(string inputPath, string outputPath, byte[] key)
    {
        ValidateKey(key);
        string sourceFull = Path.GetFullPath(inputPath);
        string destinationFull = Path.GetFullPath(outputPath);
        if (string.Equals(sourceFull, destinationFull, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Tệp đầu ra không được trùng với tệp đầu vào.");

        using var input = new FileStream(sourceFull, FileMode.Open, FileAccess.Read, FileShare.Read,
            ChunkSizeBytes, FileOptions.SequentialScan);
        using var output = new FileStream(destinationFull, FileMode.Create, FileAccess.Write, FileShare.None,
            ChunkSizeBytes, FileOptions.SequentialScan);
        using var aes = new AesGcm(key, TagSizeBytes);

        byte[] magic = ReadExactly(input, FileMagic.Length);
        if (!CryptographicOperations.FixedTimeEquals(magic, FileMagic))
            throw new CryptographicException("Tệp không phải định dạng AES-SAFE của ứng dụng.");

        long chunkIndex = 0;
        while (true)
        {
            int length = ReadInt32(input);
            if (length == 0)
                break;
            if (length < 0 || length > ChunkSizeBytes)
                throw new CryptographicException("Kích thước khối trong tệp mã hóa không hợp lệ.");

            byte[] nonce = ReadExactly(input, NonceSizeBytes);
            byte[] cipher = ReadExactly(input, length);
            byte[] tag = ReadExactly(input, TagSizeBytes);
            byte[] plain = new byte[length];
            byte[] aad = BuildChunkAad(chunkIndex, length);
            aes.Decrypt(nonce, cipher, tag, plain, aad);
            output.Write(plain);
            chunkIndex++;
        }

        if (input.ReadByte() != -1)
            throw new CryptographicException("Tệp mã hóa có dữ liệu thừa sau dấu kết thúc.");
        output.Flush(true);
    }

    public static bool CompareFilesByteByByte(string firstPath, string secondPath)
    {
        using var first = new FileStream(firstPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            ChunkSizeBytes, FileOptions.SequentialScan);
        using var second = new FileStream(secondPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            ChunkSizeBytes, FileOptions.SequentialScan);
        byte[] a = new byte[ChunkSizeBytes];
        byte[] b = new byte[ChunkSizeBytes];

        while (true)
        {
            int readA = ReadChunk(first, a);
            int readB = ReadChunk(second, b);
            if (readA != readB)
                return false;
            if (readA == 0)
                return true;
            if (!a.AsSpan(0, readA).SequenceEqual(b.AsSpan(0, readB)))
                return false;
        }
    }

    public static string GetSha256Hex(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            ChunkSizeBytes, FileOptions.SequentialScan);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static void ValidateKey(byte[] key)
    {
        if (key is null || key.Length != KeySizeBytes)
            throw new CryptographicException("AES-256 yêu cầu khóa nhị phân dài đúng 32 byte.");
    }

    private static byte[] BuildChunkAad(long chunkIndex, int length)
    {
        byte[] aad = new byte[FileMagic.Length + sizeof(long) + sizeof(int)];
        FileMagic.CopyTo(aad, 0);
        BinaryPrimitives.WriteInt64LittleEndian(aad.AsSpan(FileMagic.Length, sizeof(long)), chunkIndex);
        BinaryPrimitives.WriteInt32LittleEndian(aad.AsSpan(FileMagic.Length + sizeof(long), sizeof(int)), length);
        return aad;
    }

    private static int ReadChunk(Stream input, byte[] buffer)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int read = input.Read(buffer, total, buffer.Length - total);
            if (read == 0)
                break;
            total += read;
        }
        return total;
    }

    private static void WriteInt32(Stream stream, int value)
    {
        Span<byte> bytes = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
        stream.Write(bytes);
    }

    private static int ReadInt32(Stream stream)
    {
        byte[] bytes = ReadExactly(stream, sizeof(int));
        return BinaryPrimitives.ReadInt32LittleEndian(bytes);
    }

    private static byte[] ReadExactly(Stream stream, int count)
    {
        byte[] buffer = new byte[count];
        int total = 0;
        while (total < count)
        {
            int read = stream.Read(buffer, total, count - total);
            if (read == 0)
                throw new EndOfStreamException("Tệp kết thúc giữa chừng hoặc bị cắt cụt.");
            total += read;
        }
        return buffer;
    }
}
