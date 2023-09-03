using Data.WorldGen;
using UnityEngine;
using Utils;

namespace World.WorldData
{
    public class WorldData : MonoBehaviour
    {
        /// <summary>
        /// Seed to use for setting up other generators.
        /// </summary>
        public ulong seed;
        /// <summary>
        /// The starts of the paths, where attackers appear, one tile outside of the world.
        /// </summary>
        public Vector2Int[] pathStarts;
        /// <summary>
        /// The first tile position along each path.
        /// </summary>
        public Vector2Int[] firstPathTiles;
        /// <summary>
        /// The generated terrain <see cref="Module"/>s and their heights.
        /// </summary>
        public Array2D<(Module module, int height)> terrain;
        /// <summary>
        /// The generated tiles.
        /// </summary>
        public TilesData tiles;

        /// <summary>
        /// Clear all stored data.
        /// </summary>
        public void Clear()
        {
            seed = 0;
            pathStarts = null;
            firstPathTiles = null;
            terrain = null;
            tiles = null;
        }
    }
}

