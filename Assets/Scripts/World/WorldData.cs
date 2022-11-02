using InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.World
{
    public class WorldData
    {
        public LevelGenTiles tiles;
        public int[,] modules;
        public int[,] moduleHeights;
        public List<Vector2>[] decorationPositions;
        public List<float>[] decorationScales;
    }
}
