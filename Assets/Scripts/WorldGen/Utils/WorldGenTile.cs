using Data.WorldGen;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace WorldGen.Utils
{
    public class WorldGenTile
    {
        public Vector2Int pos;
        public int height;
        public WorldUtils.Slant slant;
        public CardinalDirs<WorldGenTile> neighbors;
        public bool passable;
        public BlockerData blocker;
        public int dist;
        public List<WorldGenTile> pathNext = new();

        public float GetHeight(Vector2 relativePos)
        {
            float offset = slant switch
            {
                WorldUtils.Slant.North => -relativePos.y - 0.5f,
                WorldUtils.Slant.East => -relativePos.x - 0.5f,
                WorldUtils.Slant.South => relativePos.y - 0.5f,
                WorldUtils.Slant.West => relativePos.x - 0.5f,
                _ => 0,
            };
            return height + offset;
        }
    }
}
