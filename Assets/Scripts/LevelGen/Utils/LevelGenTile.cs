
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace LevelGen.Utils
{
    public class LevelGenTile
    {
        public Vector2Int pos;
        public int height;
        public WorldUtils.Slant slant;
        public LevelGenTile[] neighbors;
        public bool passable;
        public int blocker;
        public int dist;
        public List<LevelGenTile> pathNext = new();

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
