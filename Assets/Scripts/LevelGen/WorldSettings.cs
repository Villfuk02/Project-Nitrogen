using UnityEngine;
using Random = UnityEngine.Random;

namespace LevelGen
{
    public class WorldSettings : MonoBehaviour
    {
        public string terrainType;
        public long seed;
        public int[] paths;

        [Header("Testing settings")]
        [SerializeField]
        bool randomSeed;

        void Awake()
        {
            if (randomSeed)
            {
                // this is good enough for testing
                seed = Random.Range(int.MinValue, int.MaxValue) +
                       ((long)Random.Range(int.MinValue, int.MaxValue) << 32);
            }
        }
    }
}
