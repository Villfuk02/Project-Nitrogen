using System;
using System.Collections.Generic;
using UnityEngine;

namespace Random
{
    public class Random
    {
        //Linear congruential generator, MODULO 2^64
        //MULTIPLIER taken from:
        //TABLES OF LINEAR CONGRUENTIAL GENERATORS OF DIFFERENT SIZES AND GOOD LATTICE STRUCTURE by PIERRE L'ECUYER, Table 4
        //https://citeseerx.ist.psu.edu/doc/10.1.1.34.1024
        const ulong MULTIPLIER = 3935559000370003845ul;
        //INCREMENT can be any odd number
        const ulong INCREMENT = 0x_FACED;
        //see NewSeed()
        const ulong SEED_MASK = 0x1D15ED_ACE71C_AC1D;
        /// <summary>
        /// Get the current state.
        /// </summary>
        public ulong CurrentState { get; private set; }

        public Random(ulong seed) => CurrentState = seed;

        //Advance to the next state.
        void Step() => CurrentState = MULTIPLIER * CurrentState + INCREMENT;

        /// <summary>
        /// Get a random double (multiple of 1/2^48) between [0..1).
        /// </summary>
        public double ExclusiveFraction()
        {
            Step();
            const double fraction = 1.0 / (1ul << 48);
            return (CurrentState >> 16) * fraction;
        }
        /// <summary>
        /// Get a random double (multiple of 1/(2^48-1)) between [0..1].
        /// </summary>
        public double InclusiveFraction()
        {
            Step();
            const double divisor = (1ul << 48) - 1;
            return (CurrentState >> 16) / divisor;
        }
        /// <summary>
        /// Get a random float between [0..1].
        /// </summary>
        public float Float() => (float)InclusiveFraction();
        /// <summary>
        /// Get a random float between [min..max].
        /// </summary>
        public float Float(float minInclusive, float maxInclusive) => (float)(minInclusive + (maxInclusive - minInclusive) * InclusiveFraction());
        /// <summary>
        /// Get a random int between [0..max-1].
        /// </summary>
        public int Int(int maxExclusive) => (int)Math.Floor(maxExclusive * ExclusiveFraction());
        /// <summary>
        /// Get a random int between [min..max-1].
        /// </summary>
        public int Int(int minInclusive, int maxExclusive) => (int)Math.Floor(minInclusive + (maxExclusive - minInclusive) * ExclusiveFraction());
        /// <summary>
        /// Get a random seed for a new Random instance.
        /// </summary>
        public ulong NewSeed()
        {
            Step();
            // XORing with a constant so the new generator produces seemingly unrelated values
            return CurrentState ^ SEED_MASK;
        }
        /// <summary>
        /// Get a random point on a unit circle, uniformly distributed.
        /// </summary>
        public Vector2 OnUnitCircle()
        {
            double angle = Math.PI * 2 * ExclusiveFraction();
            return new((float)Math.Cos(angle), (float)Math.Sin(angle));
        }
        /// <summary>
        /// Get a random point inside a unit circle (or disk), uniformly distributed.
        /// </summary>
        public Vector2 InsideUnitCircle()
        {
            // rejection sampling
            float x, y;
            do
            {
                x = Float(-1, 1);
                y = Float(-1, 1);
            } while (x * x + y * y > 1);
            return new(x, y);
        }
        /// <summary>
        /// Get a random point inside a unit sphere (or ball), uniformly distributed.
        /// </summary>
        public Vector3 InsideUnitSphere()
        {
            // rejection sampling
            float x, y, z;
            do
            {
                x = Float(-1, 1);
                y = Float(-1, 1);
                z = Float(-1, 1);
            } while (x * x + y * y + z * z > 1);
            return new(x, y, z);
        }
        /// <summary>
        /// Shuffle an IList.
        /// </summary>
        public void Shuffle<T>(IList<T> list)
        {
            int length = list.Count;
            for (int i = 1; i < length; i++)
            {
                int j = i - 1;
                int k = Int(i, length);
                (list[j], list[k]) = (list[k], list[j]);
            }
        }
    }
}
