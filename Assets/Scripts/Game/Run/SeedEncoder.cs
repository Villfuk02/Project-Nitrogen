using System.Text;
using UnityEngine;

namespace Game.Run
{
    public static class SeedEncoder
    {
        // translation table for values 0 to 15
        static readonly char[] CharFromCode = { 'A', 'C', 'D', 'E', 'F', 'I', 'L', 'M', 'N', 'O', 'P', 'R', 'S', 'T', 'U', 'Y' };
        // translation table for chars 'A' to 'Z'
        static readonly uint[] CodeFromChar = { 0, 16, 1, 2, 3, 4, 17, 18, 5, 19, 20, 6, 7, 8, 9, 10, 21, 11, 12, 13, 14, 22, 23, 24, 15, 25 };
        static readonly ulong BaseSeed = 0xBA5E_5EED_0123_1006;
        static readonly int RotationStep = 36;

        public static ulong GetSeedFromString(ref string seedString)
        {
            seedString = seedString?.Trim();
            if (string.IsNullOrEmpty(seedString))
                seedString = GetRandomSeed();
            if (ulong.TryParse(seedString, out ulong seed))
                return seed;
            return DecodeSeed(seedString);
        }

        public static ulong DecodeSeed(string seedString)
        {
            seedString = seedString.Trim().ToUpperInvariant();
            ulong seed = BaseSeed;
            int rotation = 0;
            foreach (char c in seedString)
            {
                if (char.IsWhiteSpace(c))
                    continue;
                ulong decoded = DecodeChar(c);
                seed ^= decoded << rotation;
                if (rotation != 0)
                    seed ^= decoded >> (64 - rotation);
                rotation = (rotation + RotationStep) % 64;
            }

            return seed;
        }

        static uint DecodeChar(char c) => c is >= 'A' and <= 'Z' ? CodeFromChar[c - 'A'] : c;

        static string GetRandomSeed()
        {
            StringBuilder sb = new(9);
            for (int i = 0; i < 8; i++)
            {
                sb.Append(CharFromCode[Random.Range(0, 16)]);
                if (i == 3)
                    sb.Append(' ');
            }

            return sb.ToString();
        }
    }
}