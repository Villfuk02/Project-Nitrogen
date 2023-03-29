using Assets.Scripts.LevelGen.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.World.WorldData
{
    public class WorldData
    {
        public static WorldData WORLD_DATA;

        public LevelGenTiles tiles;
        public int[,] modules;
        public int[,] moduleHeights;
        public List<Vector2>[] decorationPositions;
        public List<float>[] decorationScales;
        public Vector2Int[] firstPathNodes;
        public Vector2Int[] pathStarts;
    }
}
