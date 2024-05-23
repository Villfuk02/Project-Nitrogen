using System.Collections.Generic;
using System.Linq;

namespace Utils
{
    /// <summary>
    /// A set of 32 bits. Used to represent a set of up to 32 items indexed 0 to 31.
    /// </summary>
    public struct BitSet32
    {
        uint bits_;

        BitSet32(uint bits) => bits_ = bits;

        /// <summary>
        /// bitset with all bits set.
        /// </summary>
        public static BitSet32 AllSet => new(0xffffffff);

        /// <summary>
        /// Constructs a bitset with the lowest n bits set.
        /// n must be between 0 and 31
        /// </summary>
        public static BitSet32 LowestBitsSet(int bits) => new((1u << bits) - 1);

        /// <summary>
        /// Constructs a bitset with only one bit set.
        /// </summary>
        public static BitSet32 OneBit(int bit) => new(1u << bit);

        /// <summary>
        /// Constructs a bitset with the given bits set.
        /// </summary>
        public static BitSet32 FromBits(IEnumerable<int> bits)
        {
            BitSet32 set = new();
            foreach (var bit in bits)
                set.SetBit(bit);
            return set;
        }

        public readonly bool IsEmpty => bits_ == 0;

        public readonly bool IsSet(int bit) => (bits_ & (1 << bit)) != 0;

        /// <summary>
        /// Enumerate the bits that are set.
        /// </summary>
        public readonly IEnumerable<int> GetBits()
        {
            uint bits = bits_;
            return Enumerable.Range(0, 32).Where(i => ((bits >> i) & 1) != 0);
        }

        /// <summary>
        /// Counts the number of bits that are set.
        /// </summary>
        // Taken from https://stackoverflow.com/a/109025/7861895
        public readonly int PopCount()
        {
            uint n = bits_;
            n -= (n >> 1) & 0x55555555; // add pairs of bits
            n = (n & 0x33333333) + ((n >> 2) & 0x33333333); // quads
            n = (n + (n >> 4)) & 0x0F0F0F0F; // groups of 8
            n *= 0x01010101; // horizontal sum of bytes
            return (int)(n >> 24);
        }

        public void SetBit(int bit) => bits_ |= 1u << bit;
        public void ResetBit(int bit) => bits_ &= ~(1u << bit);
        public void UnionWith(BitSet32 other) => bits_ |= other.bits_;
        public void IntersectWith(BitSet32 other) => bits_ &= other.bits_;
        public static BitSet32 Union(BitSet32 a, BitSet32 b) => new(a.bits_ | b.bits_);
        public static BitSet32 Intersect(BitSet32 a, BitSet32 b) => new(a.bits_ & b.bits_);
        public static bool operator ==(BitSet32 a, BitSet32 b) => a.bits_ == b.bits_;
        public static bool operator !=(BitSet32 a, BitSet32 b) => a.bits_ != b.bits_;
        public static BitSet32 operator <<(BitSet32 set, int shift) => new(set.bits_ << shift);
        public static BitSet32 operator >> (BitSet32 set, int shift) => new(set.bits_ >> shift);

        public readonly override bool Equals(object obj) => obj is BitSet32 other && this == other;
        public readonly bool Equals(BitSet32 other) => bits_ == other.bits_;
        public readonly override int GetHashCode() => (int)bits_;
    }
}