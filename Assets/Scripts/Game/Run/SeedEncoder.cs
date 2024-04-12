using System.Text;
using UnityEngine;

namespace Game.Run
{
    public static class SeedEncoder
    {
        // translation table for values 0 to 15
        static readonly char[] CharFromCode = { 'A', 'C', 'D', 'E', 'F', 'I', 'L', 'M', 'N', 'O', 'P', 'R', 'S', 'T', 'U', 'Y' };
        // translation table for chars 'A' to 'Z'
        static readonly ulong[] CodeFromChar = { 0, 16, 1, 2, 3, 4, 17, 18, 5, 19, 20, 6, 7, 8, 9, 10, 21, 11, 12, 13, 14, 22, 23, 24, 15, 25 };
        static readonly ulong BaseSeed = 0xBA5E_5EED_BA5E_5EED;
        static readonly int RotationStep = 20;
        public static string EncodeSeed(ulong seed)
        {
            seed ^= BaseSeed;
            StringBuilder sb = new(19);
            int rotation = 0;
            for (int i = 0; i < 16; i++)
            {
                sb.Append(CharFromCode[(seed >> rotation) & 0xF]);
                rotation = (rotation + RotationStep) % 64;
                if (i % 4 == 3 && i != 15)
                    sb.Append(' ');
            }
            return sb.ToString();
        }

        public static ulong GetSeedFromString(ref string seedString)
        {
            seedString = seedString?.Trim();
            ulong seed;
            if (string.IsNullOrEmpty(seedString))
            {
                seed = GetRandomSeed();
                seedString = EncodeSeed(seed);
                return seed;
            }
            if (ulong.TryParse(seedString, out seed))
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

        static ulong DecodeChar(char c) => char.IsLetter(c) ? CodeFromChar[c - 'A'] : c;

        static ulong GetRandomSeed()
        {
            ulong seed = 0;
            for (int i = 0; i < 4; i++)
            {
                seed <<= 16;
                seed |= (ulong)(long)Random.Range(0, (1 << 16) + 1);
            }
            return seed;
        }
    }
}
