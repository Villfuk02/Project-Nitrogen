using System.Collections.Generic;
using Data.WorldGen;
using UnityEngine;
using Utils;

namespace BattleSimulation.World.WorldData
{
    public class TileData
    {
        public Vector2Int pos;
        public int height;
        public WorldUtils.Slant slant;
        public CardinalDirs<TileData> neighbors;
        public bool blocked;
        public ObstacleData obstacle;
        public int dist;
        public List<TileData> pathNext = new();
        public List<DecorationInstance> decorations = new();

        /// <summary>
        /// Returns the tile-space height of the terrain at a position relative to the center of this tile.
        /// </summary>
        public float GetHeight(Vector2 relativePos)
        {
            float offset = slant switch
            {
                WorldUtils.Slant.North => -relativePos.y - 0.5f,
                WorldUtils.Slant.East => -relativePos.x - 0.5f,
                WorldUtils.Slant.South => relativePos.y - 0.5f,
                WorldUtils.Slant.West => relativePos.x - 0.5f,
                _ => 0
            };
            return height + offset;
        }
    }

    public struct DecorationInstance
    {
        public Decoration decoration;
        public Vector2 position;
        public float size;
        public Vector3 eulerRotation;
    }
}