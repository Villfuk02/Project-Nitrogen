using Attackers.Simulation;
using UnityEngine;
using World.WorldData;

namespace Battle
{
    public class WaveController : MonoBehaviour
    {
        [SerializeField] WorldData worldData;
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
            uint paths = (uint)worldData.firstPathNodes.Length;
            uint selectedPath = index % paths;
            index /= paths;
            Attacker a = Instantiate(attackerPrefab, transform).GetComponent<Attacker>();
            a.worldData = worldData;
            Vector2Int startingPoint = worldData.pathStarts[selectedPath];
            Vector2Int firstTile = worldData.firstPathNodes[selectedPath];
            a.InitPath(startingPoint, firstTile, index);
        }
    }
}

