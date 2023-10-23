using BattleSimulation.Attackers;
using UnityEngine;
using Utils;

namespace BattleSimulation.Control
{
    public class WaveController : MonoBehaviour
    {
        [SerializeField] GameObject attackerPrefab;
        [SerializeField] int spawnInterval;
        [SerializeField] int spawnTimer;
        [SerializeField] uint currentIndex;
        public int wave;
        public int toSpawn;
        public int attackersLeft;
        [SerializeField] bool startNextWave;

        public static GameCommand startWave = new();
        public static GameEvent onWaveSpawned = new();
        public static GameEvent onWaveFinished = new();

        void Awake()
        {
            // only use half the range to prevent overflow
            currentIndex = (uint)(World.WorldData.World.data.seed & 0x7FFFFFFF);

            startWave.Register(StartNextWave, 0);
        }

        void OnDestroy()
        {
            startWave.Unregister(StartNextWave);
        }

        void Update()
        {
            if (attackersLeft == 0 && toSpawn == 0 && Input.GetKeyUp(KeyCode.Return))
            {
                startNextWave = true;
            }
        }

        void FixedUpdate()
        {
            if (startNextWave)
            {
                startNextWave = false;
                startWave.Invoke();
            }

            spawnTimer++;
            while (toSpawn > 0 && spawnTimer >= spawnInterval)
            {
                spawnTimer -= spawnInterval;
                toSpawn--;
                Spawn();
                if (toSpawn == 0)
                    onWaveSpawned.Broadcast();
            }
        }

        void Spawn()
        {
            uint index = ++currentIndex;
            uint paths = (uint)World.WorldData.World.data.firstPathTiles.Length;
            uint selectedPath = index % paths;
            index /= paths;
            Attacker a = Instantiate(attackerPrefab, transform).GetComponent<Attacker>();
            Vector2Int startingPoint = World.WorldData.World.data.pathStarts[selectedPath];
            Vector2Int firstTile = World.WorldData.World.data.firstPathTiles[selectedPath];
            a.InitPath(startingPoint, firstTile, index);
            a.onRemoved.AddListener(AttackerRemoved);

            attackersLeft++;
        }

        bool StartNextWave()
        {
            wave++;
            spawnTimer = 0;
            toSpawn = wave * (wave + 1) / 2;
            return true;
        }

        public void AttackerRemoved()
        {
            attackersLeft--;
            if (attackersLeft == 0)
                onWaveFinished.Broadcast();
        }
    }
}

