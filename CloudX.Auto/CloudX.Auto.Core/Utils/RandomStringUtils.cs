using System;
using System.Linq;

namespace CloudX.Auto.Core.Utils
{
    public static class RandomStringUtils
    {
        private static readonly Random Random = new Random();

        public static string RandomNumeric(int length)
        {
            if (length == 0)
            {
                return string.Empty;
            }
            if (length < 0)
            {
                throw new ArgumentException($"Random string length {length} is less than 0.");
            }
            var values = Enumerable.Range(1, length).Select(n => Random.Next(9)).ToArray();
            values[0] = values[0] == 0 ? Random.Next(8) + 1 : values[0];
            return string.Join(string.Empty, values);
        }

        public static string RandomNumeric(int min, int max)
        {
            return Random.Next(min, max).ToString();
        }

        public static string RandomString(int length)
        {
            string result;
            do
                result = GenerateAlphabeticalString(length);
            while (result.StartsWith('0'));
            return result;
        }

        private static string GenerateAlphabeticalString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[Random.Next(s.Length)]).ToArray());
        }
    }
}


