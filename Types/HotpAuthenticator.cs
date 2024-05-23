using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace NekoBot.Types;

public class HotpAuthenticator
{
    public long Counter { get; set; } = 0;
    public int Digits { get; private set; } = 8;
    public string SecretKey { get; private set; } = "";
    static string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    public int FailureCount { get; private set; }
    [YamlIgnore]
    public string Code { get => GetCode(); }

    public HotpAuthenticator()
    {
        byte[] randomKey = new byte[16];
        RandomNumberGenerator.Create().GetBytes(randomKey);

        SecretKey = ToBase32String(randomKey);
    }
    public HotpAuthenticator(string sKey, long counter, int digits)
    {
        this.Counter = counter;
        this.Digits = digits;
        SecretKey = sKey;
    }
    public string GetCode(long counter)
    {
        byte[] counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes);

        var hmac = new HMACSHA1(FromBase32String(SecretKey));
        var hash = hmac.ComputeHash(counterBytes);
        int offset = hash[hash.Length - 1] & 0xf;
        int binary = (hash[offset] & 0x7f) << 24 |
                     (hash[offset + 1] & 0xff) << 16 |
                     (hash[offset + 2] & 0xff) << 8 |
                     hash[offset + 3] & 0xff;

        int otp = binary % (int)Math.Pow(10, Digits);
        return otp.ToString(new string('0', Digits));
    }
    public string GetCode() => GetCode(Counter);
    public bool Compare(string code)
    {
        if (FailureCount >= 4)
            return false;

        for (int index = -1; index < 1; index++)
        {
            if (GetCode(Counter + index) == code)
            {
                Counter++;
                FailureCount = 0;
                return true;
            }
        }
        FailureCount++;
        return false;
    }
    public static string ToBase32String(byte[] bytes)
    {
        int bitCount = 0;
        int bitIndex = 0;
        int index = 0;
        var encodedChars = new char[(bytes.Length + 4) / 5 * 8];

        foreach (byte b in bytes)
        {
            var currentByte = b & 0xFF;
            bitIndex = bitIndex << 8 | currentByte;
            bitCount += 8;
            while (bitCount >= 5)
            {
                encodedChars[index++] = Base32Chars[bitIndex >> bitCount - 5 & 0x1F];
                bitCount -= 5;
            }
        }

        if (bitCount > 0)
            encodedChars[index++] = Base32Chars[bitIndex << 5 - bitCount & 0x1F];

        return new string(encodedChars).Replace("\0", "");
    }
    public static byte[] FromBase32String(string base32)
    {
        int bCount = base32.Length * 5 / 8;
        byte[] result = new byte[bCount];

        int bitCount = 0;
        int bitIndex = 0;
        int index = 0;

        foreach (var c in base32.ToUpper())
        {
            int currentChar = Base32Chars.IndexOf(c);
            if (currentChar == -1)
                continue;

            bitIndex = bitIndex << 5 | currentChar;
            bitCount += 5;
            if (bitCount >= 8)
            {
                result[index++] = (byte)(bitIndex >> bitCount - 8 & 0xFF);
                bitCount -= 8;
            }
        }

        Array.Resize(ref result, index);

        return result;
    }
}
