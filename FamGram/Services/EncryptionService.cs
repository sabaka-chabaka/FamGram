using System.Security.Cryptography;
using System.Text;

namespace FamGram.Services;

public class EncryptionService(IConfiguration config)
{
    private readonly byte[] _masterKey = GetMasterKey(config);

    private static byte[] GetMasterKey(IConfiguration config)
    {
        var keyB64 = config["Encryption:MasterKey"];
 
        if (!string.IsNullOrEmpty(keyB64))
        {
            var key = Convert.FromBase64String(keyB64);
            if (key.Length != 32)
                throw new InvalidOperationException(
                    $"Encryption:MasterKey must be exactly 32 bytes (got {key.Length}). " +
                    "Generate one with: openssl rand -base64 32");
            return key;
        }
 
        throw new InvalidOperationException(
            "Encryption:MasterKey is not configured. " +
            "Generate a key with: openssl rand -base64 32 " +
            "and set it in appsettings.json or the ENCRYPTION__MASTERKEY environment variable.");
    }
    
    public byte[] GenerateChatKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return key;
    }
    
    public string WrapChatKey(byte[] chatKey) => Encrypt(chatKey, _masterKey);
 
    public byte[] UnwrapChatKey(string wrapped) => DecryptBytes(wrapped, _masterKey);
    
    public string EncryptMessage(string plaintext, byte[] chatKey)
    {
        var data = Encoding.UTF8.GetBytes(plaintext);
        return Encrypt(data, chatKey);
    }
    
    public string DecryptMessage(string wireFormat, byte[] chatKey)
    {
        var plain = DecryptBytes(wireFormat, chatKey);
        return Encoding.UTF8.GetString(plain);
    }
    
    private static string Encrypt(byte[] plaintext, byte[] key)
    {
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        var tag   = new byte[AesGcm.TagByteSizes.MaxSize];
        var cipher = new byte[plaintext.Length];
 
        RandomNumberGenerator.Fill(nonce);
 
        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(nonce, plaintext, cipher, tag);
 
        var packed = new byte[nonce.Length + tag.Length + cipher.Length];
        nonce.CopyTo(packed, 0);
        tag.CopyTo(packed, nonce.Length);
        cipher.CopyTo(packed, nonce.Length + tag.Length);
 
        return Convert.ToBase64String(packed);
    }
 
    private static byte[] DecryptBytes(string wireBase64, byte[] key)
    {
        var packed = Convert.FromBase64String(wireBase64);
 
        const int nonceLen = 12;
        const int tagLen   = 16;
 
        var nonce  = packed[..nonceLen];
        var tag    = packed[nonceLen..(nonceLen + tagLen)];
        var cipher = packed[(nonceLen + tagLen)..];
        var plain  = new byte[cipher.Length];
 
        using var aes = new AesGcm(key, tagLen);
        aes.Decrypt(nonce, cipher, tag, plain);
 
        return plain;
    }
}