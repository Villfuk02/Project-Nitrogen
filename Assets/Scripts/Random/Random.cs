using System;
using UnityEngine;

namespace Random
{
    public class Random
    {
        //const int MODULO = 2^64;
        //MULTIPLIER taken from https://citeseerx.ist.psu.edu/doc/10.1.1.34.1024
        const ulong MULTIPLIER = 3935559000370003845ul;
        //INCREMENT can be any odd number
        const ulong INCREMENT = 0x_FACED;
        const ulong SEED_MASK = 0x1D15ED_ACE71C_AC1D;
        ulong state_;

        public Random(ulong seed) => state_ = seed;

        void Step() => state_ = MULTIPLIER * state_ + INCREMENT;

        //get a random double (multiple of 1/2^53) between [0..1)
        public double ExclusiveFraction()
        {
            Step();
            const double fraction = 1.0 / (1ul << 53);
            return (state_ >> 11) * fraction;
        }
        //get a random double (multiple of 1/(2^64-1)) between [0..1]
        public double InclusiveFraction()
        {
            Step();
            const double fraction = 1.0 / ulong.MaxValue;
            return state_ * fraction;
        }
        //get a random float between [0..1]
        public float Float() => (float)InclusiveFraction();
        //get a random float between [min..max]
        public float Float(float minInclusive, float maxInclusive) => (float)(minInclusive + (maxInclusive - minInclusive) * InclusiveFraction());
        //get a random int between [min..max)
        public int Int(int minInclusive, int maxExclusive) => (int)(minInclusive + (maxExclusive - minInclusive) * ExclusiveFraction());
        //get a random seed for a new Random
        public ulong NewSeed()
        {
            Step();
            return state_ ^ SEED_MASK;
        }
        //get current state
        public ulong CurrentState() => state_;
        //get a random point on a unit circle
        public Vector2 OnUnitCircle()
        {
            double angle = Math.PI * 2 * ExclusiveFraction();
            return new((float)Math.Cos(angle), (float)Math.Sin(angle));
        }
        //get a random point in a unit circle (or disk)
        public Vector2 InsideUnitCircle()
        {
            float x, y;
            do
            {
                x = Float(-1, 1);
                y = Float(-1, 1);
            } while (x * x + y * y > 1);
            return new(x, y);
        }
        //get a random point in a unit sphere (or ball)
        public Vector3 InsideUnitSphere()
        {
            float x, y, z;
            do
            {
                x = Float(-1, 1);
                y = Float(-1, 1);
                z = Float(-1, 1);
            } while (x * x + y * y + z * z > 1);
            return new(x, y, z);
        }
    }
}
