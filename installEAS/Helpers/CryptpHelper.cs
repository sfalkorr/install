using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace installEAS.Helpers;

public static class Crypt
{
    public static string DecryptString(string cipherText)
    {
        var initVectorBytes = Encoding.ASCII.GetBytes("7BGF9$5tbgf23bhn1");
        var saltValueBytes  = Encoding.ASCII.GetBytes("S@lyanka");
        var cipherTextBytes = Convert.FromBase64String(cipherText);
        var password        = new PasswordDeriveBytes("Pas5prseOlogiZm", saltValueBytes, "SHA1", 2);
        var keyBytes        = password.GetBytes(256 / 8);
        var symmetricKey    = new RijndaelManaged();
        symmetricKey.Mode = CipherMode.CBC;
        var decryptor          = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);
        var memoryStream       = new MemoryStream(cipherTextBytes);
        var cryptoStream       = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
        var plainTextBytes     = new byte[cipherTextBytes.Length];
        var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
        memoryStream.Close();
        cryptoStream.Close();
        var plainText = Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
        return plainText;
    }

    public static string EncryptString(string plainText)
    {
        var initVectorBytes = Encoding.ASCII.GetBytes("7BGF9$5tbgf23bhn1");
        var saltValueBytes  = Encoding.ASCII.GetBytes("S@lyanka");
        var plainTextBytes  = Encoding.UTF8.GetBytes(plainText);
        var password        = new PasswordDeriveBytes("Pas5prseOlogiZm", saltValueBytes, "SHA1", 2);
        var keyBytes        = password.GetBytes(256 / 8);
        var symmetricKey    = new RijndaelManaged();
        symmetricKey.Mode = CipherMode.CBC;
        var encryptor    = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes);
        var memoryStream = new MemoryStream();
        var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
        cryptoStream.FlushFinalBlock();
        var cipherTextBytes = memoryStream.ToArray();
        memoryStream.Close();
        cryptoStream.Close();
        var cipherText = Convert.ToBase64String(cipherTextBytes);
        return cipherText;
    }
}