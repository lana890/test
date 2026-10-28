using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace EskaCommerce.Payments.Helper
{
    public class SHA256Signature
    {



        public static string SHA1HashStringForUTF8String(string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);

            var sha1 = SHA1.Create();
            byte[] hashBytes = sha1.ComputeHash(bytes);

            return HexStringFromBytes(hashBytes);
        }

        /// <summary>
        /// Convert an array of bytes to a string of hex digits
        /// </summary>
        /// <param name="bytes">array of bytes</param>
        /// <returns>String of hex digits</returns>
        public static string HexStringFromBytes(byte[] bytes)
        {
            var sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                var hex = b.ToString("x2");
                sb.Append(hex);
            }
            return sb.ToString();
        }

        public static string CreateSha1( string _requestFields)
        {
           

            using (SHA1 sha1Hash = SHA1.Create())
            {
                //From String to byte array
                byte[] sourceBytes = Encoding.UTF8.GetBytes(_requestFields);
                byte[] hashBytes = sha1Hash.ComputeHash(sourceBytes);
                string hash = BitConverter.ToString(hashBytes).Replace("-", String.Empty);

                // Console.WriteLine("The SHA1 hash of " + source + " is: " + hash);
                return hash;
            }

       
        }

        public static string hex(sbyte[] input)
        {
            //  This is an array for creating hex chars
            char[] HEX_TABLE = new char[] {
                '0',
                '1',
                '2',
                '3',
                '4',
                '5',
                '6',
                '7',
                '8',
                '9',
                'A',
                'B',
                'C',
                'D',
                'E',
                'F'};
            //  create a StringBuffer 2x the size of the hash array
            StringBuilder sb = new StringBuilder((input.Length * 2));
            //  retrieve the byte array data, convert it to hex
            //  and add it to the StringBuffer
            for (int i = 0; (i < input.Length); i++)
            {
                sb.Append(HEX_TABLE[(input[i] >> 4) & 0xf]);
                sb.Append(HEX_TABLE[input[i] & 0xf]);
            }

            return sb.ToString();
        }

        public static byte[] fromHexString(String s, int offset, int length)
        {
            if (((length % 2)
                        != 0))
            {
                return null;
            }

            byte[] byteArray = new byte[(length / 2)];
            int j = 0;
            int end = (offset + length);
            for (int i = offset; (i < end); i += 2)
            {
                int high_nibble = Digit(s[i], 16);
                int low_nibble = Digit(s[i + 1], 16);
                if (((high_nibble == -1)
                            || (low_nibble == -1)))
                {
                    //  illegal format
                    return null;
                }

                byteArray[j++] = Convert.ToByte((((high_nibble << 4)
                            & 0xf0)
                            | (low_nibble & 0x0f)));
            }

            return byteArray;
        }
        public static int Digit(char value, int radix)
        {
            if ((radix <= 0) || (radix > 36))
                return -1; // Or throw exception

            if (radix <= 10)
                if (value >= '0' && value < '0' + radix)
                    return value - '0';
                else
                    return -1;
            else if (value >= '0' && value <= '9')
                return value - '0';
            else if (value >= 'a' && value < 'a' + radix - 10)
                return value - 'a' + 10;
            else if (value >= 'A' && value < 'A' + radix - 10)
                return value - 'A' + 10;

            return -1;
        }
    }
}
