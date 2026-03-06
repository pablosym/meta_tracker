using System.Security.Cryptography;
using System.Text;

namespace Tracker.Helpers;

/// <summary>
/// Clase de criptografía heredada utilizada para mantener compatibilidad
/// con datos previamente encriptados. ¡NO CAMBIAR SIN MIGRAR LOS DATOS!
/// </summary>
public class Cryptography
{
    public static string PassKey()
    {
        return "ExtraGoticoNuclear";
    }

    public static string varhttpRequest(string varhttpRequestKey, string vars)
    {
        return "?" + varhttpRequestKey + "=" + vars;
    }

    public static string varhttpRequestKey()
    {
        string chars = "ABCDefgh123456789FGHIJKLMnopq0";
        var stringChars = new char[5];
        var random = new Random();

        for (int i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }

        return new string(stringChars);
    }

    public static string DecryptText(string input, string key)
    {
        try
        {
            input = input.Replace(" ", "");

            // Get the bytes of the string
            byte[] bytesToBeDecrypted = Convert.FromBase64String(input);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(key);
            passwordBytes = SHA256.HashData(passwordBytes);

            byte[] bytesDecrypted = AES_Decrypt(bytesToBeDecrypted, passwordBytes);

            return Encoding.UTF8.GetString(bytesDecrypted);
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string EncryptText(string input, string key)
    {
        // Get the bytes of the string
        byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(input);
        byte[] passwordBytes = Encoding.UTF8.GetBytes(key);

        // Hash the password with SHA256
        passwordBytes = SHA256.HashData(passwordBytes);

        byte[] bytesEncrypted = AES_Encrypt(bytesToBeEncrypted, passwordBytes);

        return Convert.ToBase64String(bytesEncrypted);
    }

    protected static byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes)
    {
        byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        using (var ms = new MemoryStream())
        using (var aes = Aes.Create())
        {
            aes.KeySize = 256;
            aes.BlockSize = 128;

            var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
            aes.Key = key.GetBytes(aes.KeySize / 8);
            aes.IV = key.GetBytes(aes.BlockSize / 8);

            aes.Mode = CipherMode.CBC;

            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
            }

            return ms.ToArray();
        }
    }

    protected static byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes)
    {
        byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        using (var ms = new MemoryStream())
        using (var aes = Aes.Create())
        {
            aes.KeySize = 256;
            aes.BlockSize = 128;

            var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
            aes.Key = key.GetBytes(aes.KeySize / 8);
            aes.IV = key.GetBytes(aes.BlockSize / 8);

            aes.Mode = CipherMode.CBC;

            using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
            {
                cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
            }

            return ms.ToArray();
        }
    }
}
