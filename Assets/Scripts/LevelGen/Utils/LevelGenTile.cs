using InfiniteCombo.Nitrogen.Assets.Scripts.Utils;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Utils
{
    public class LevelGenTile
    {
        public Vector2Int pos;
        public int height;
        public WorldUtils.Slant slant;
        public LevelGenTile[] neighbors;
        public bool passable;
        public int dist;
    }
}
