using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Google.Authenticator;

namespace TelegramBot.Class
{
    internal class HotpAuthenticator
    {
        [JsonInclude]
        long counter = 0;
        [JsonInclude]
        int digits = 8;
        [JsonInclude]
        string secretKey = "";
        static string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        public string Code { get => GetCode(); }

        public HotpAuthenticator()
        {
            byte[] randomKey = new byte[16];
            RandomNumberGenerator.Create().GetBytes(randomKey);

            secretKey = ToBase32String(randomKey);
        }
        public HotpAuthenticator(string sKey,long counter,int digits)
        {
            this.counter = counter;
            this.digits = digits;
            secretKey = sKey;
        }
        public string GetCode(long counter)
        {
            byte[] counterBytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(counterBytes);

            var hmac = new HMACSHA1(FromBase32String(secretKey));
            var hash = hmac.ComputeHash(counterBytes);
            int offset = hash[hash.Length - 1] & 0xf;
            int binary = ((hash[offset] & 0x7f) << 24) |
                         ((hash[offset + 1] & 0xff) << 16) |
                         ((hash[offset + 2] & 0xff) << 8) |
                         (hash[offset + 3] & 0xff);

            int otp = binary % (int)Math.Pow(10, digits);
            return otp.ToString(new string('0', digits));
        }
        public string GetCode() => GetCode(counter);
        public bool Compare(string code)
        {
            for (int index = -1;index < 1;index++)
            {
                if (GetCode(counter + index) == code)
                {
                    counter++;
                    Config.Save(Path.Combine(Config.DatabasePath, "HotpAuthenticator.data"), this);
                    return true;
                }                    
            }
            return false;
        }
        public static string ToBase32String(byte[] bytes)
        {
            int bitCount = 0;
            int bitIndex = 0;
            int index = 0;
            var encodedChars = new char[((bytes.Length + 4) / 5) * 8];

            foreach (byte b in bytes)
            {
                var currentByte = b & 0xFF;
                bitIndex = bitIndex << 8 | currentByte;
                bitCount += 8;
                while (bitCount >= 5)
                {
                    encodedChars[index++] = Base32Chars[(bitIndex >> (bitCount - 5)) & 0x1F];
                    bitCount -= 5;
                }
            }

            if (bitCount > 0)
                encodedChars[index++] = Base32Chars[(bitIndex << (5 - bitCount)) & 0x1F];

            return new string(encodedChars).Replace("\0","");
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
                    result[index++] = (byte)((bitIndex >> (bitCount - 8)) & 0xFF);
                    bitCount -= 8;
                }
            }

            Array.Resize(ref result, index);

            return result;
        }
    }
}
