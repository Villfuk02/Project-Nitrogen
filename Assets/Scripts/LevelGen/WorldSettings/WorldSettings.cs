using UnityEngine;
using UnityRandom = UnityEngine.Random;

namespace LevelGen.WorldSettings
{
    public class WorldSettings : MonoBehaviour
    {
        public string terrainType;
        public ulong seed;
        public int[] paths;

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
