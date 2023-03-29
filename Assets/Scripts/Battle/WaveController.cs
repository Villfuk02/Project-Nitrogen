using Assets.Scripts.Attackers.Simulation;
using UnityEngine;
using static Assets.Scripts.World.WorldData.WorldData;

namespace Assets.Scripts.Battle
{
    public class WaveController : MonoBehaviour
    {
        [SerializeField] GameObject attackerPrefab;
        [SerializeField] float spawnInterval;
        [SerializeField] float spawnTimer;
        [SerializeField] uint currentIndex;

        private void Awake()
        {
            currentIndex = (uint)Random.Range(int.MinValue, int.MaxValue);
        }

        private void FixedUpdate()
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
            uint paths = (uint)WORLD_DATA.firstPathNodes.Length;
            uint selectedPath = index % paths;
            index /= paths;
            Attacker a = Instantiate(attackerPrefab, transform).GetComponent<Attacker>();
            a.InitPath(WORLD_DATA.pathStarts[selectedPath], WORLD_DATA.firstPathNodes[selectedPath], index);
        }
    }
}

