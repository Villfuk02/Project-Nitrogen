using Data.WorldGen;
using UnityEngine;
using Utils;

namespace World.WorldData
{
    public class WorldData : MonoBehaviour
    {
        public Vector2Int[] pathStarts;
        public Vector2Int[] firstPathNodes;
        public Array2D<(Module module, int height)> terrain;
        public TilesData tiles;

        public void Clear()
        {
            tiles = null;
        }
    }
}

