using Attackers.Simulation;
using UnityEngine;

namespace Battle
{
    public class WaveController : MonoBehaviour
    {
        [SerializeField] GameObject attackerPrefab;
        [SerializeField] int spawnInterval;
        [SerializeField] int spawnTimer;
        [SerializeField] uint currentIndex;

        void Awake()
        {
            // only use half the range to prevent overflow
            currentIndex = (uint)(World.World.data.seed & 0x7FFFFFFF);
        }

        void FixedUpdate()
        {
            spawnTimer++;
            while (spawnTimer >= spawnInterval)
            {
                spawnTimer -= spawnInterval;
                Spawn();
            }
        }

        void Spawn()
        {
            uint index = ++currentIndex;
            uint paths = (uint)World.World.data.firstPathTiles.Length;
            uint selectedPath = index % paths;
            index /= paths;
            Attacker a = Instantiate(attackerPrefab, transform).GetComponent<Attacker>();
            Vector2Int startingPoint = World.World.data.pathStarts[selectedPath];
            Vector2Int firstTile = World.World.data.firstPathTiles[selectedPath];
            a.InitPath(startingPoint, firstTile, index);
        }
    }
}

