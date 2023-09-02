using Attackers.Simulation;
using UnityEngine;

namespace Battle
{
    public class WaveController : MonoBehaviour
    {
        [SerializeField] GameObject attackerPrefab;
        [SerializeField] float spawnInterval;
        [SerializeField] float spawnTimer;
        [SerializeField] uint currentIndex;

        void Awake()
        {
            // only use half the range to not overflow
            currentIndex = (uint)Random.Range(0, int.MaxValue);
        }

        void FixedUpdate()
        {
            spawnTimer += Time.fixedDeltaTime;
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

