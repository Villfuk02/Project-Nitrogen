using UnityEngine;

namespace WorldGen.WorldSettings
{
    public class WorldSettings : MonoBehaviour
    {
        public bool overrideRun;
        public string terrainType;
        public ulong seed;
        // always keep these in ascending order
        public int[] pathLengths;
        public int maxExtraPaths;
    }
}
