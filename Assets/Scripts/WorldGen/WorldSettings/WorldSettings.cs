using UnityEngine;
using UnityRandom = UnityEngine.Random;

namespace WorldGen.WorldSettings
{
    public class WorldSettings : MonoBehaviour
    {
        public string terrainType;
        public ulong seed;
        //always keep these in ascending order
        public int[] pathLengths;
        public int maxExtraPaths;

        [Header("Testing settings")]
        [SerializeField]
        bool randomSeed;

        void Awake()
        {
            if (randomSeed)
            {
                // this is good enough for testing
                seed = (ulong)UnityRandom.Range(int.MinValue, int.MaxValue) +
                       ((ulong)UnityRandom.Range(int.MinValue, int.MaxValue) << 32);
            }
        }
    }
}
