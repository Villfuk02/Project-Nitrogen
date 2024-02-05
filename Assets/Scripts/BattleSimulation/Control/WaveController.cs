using BattleSimulation.Attackers;
using Game.AttackerStats;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Utils;

namespace BattleSimulation.Control
{
    public class WaveController : MonoBehaviour
    {
        public WaveGenerator waveGenerator;
        [SerializeField] int spawnTimer;
        [SerializeField] uint currentIndex;
        public int wave;
        public int attackersLeft;
        [SerializeField] bool startNextWave;
        [SerializeField] bool spawning;
        [SerializeField] WaveGenerator.Wave currentWave;
        int paths_;
        List<(int delay, int path, AttackerStats attacker)> spawnQueue_ = new();

        public static GameCommand startWave = new();
        public static GameEvent onWaveSpawned = new();
        public static GameEvent onWaveFinished = new();

        [SerializeField] UnityEvent onSpawnedOnce;

        void Awake()
        {
            // only use half the range to prevent overflow
            // TODO: mangle the value slightly
            currentIndex = (uint)(World.WorldData.World.data.seed & 0x7FFFFFFF);
            paths_ = World.WorldData.World.data.firstPathTiles.Length;

            startWave.Register(StartNextWave, 0);
        }

        void OnDestroy()
        {
            startWave.Unregister(StartNextWave);
        }

        void Update()
        {
            if (attackersLeft == 0 && !spawning && spawnQueue_.Count == 0 && Input.GetKeyUp(KeyCode.Return))
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
            else
            {
                spawnTimer++;
            }

            if (spawning && spawnTimer >= 0)
                ProcessSpawn();

            if (spawnQueue_.Count > 0)
                ProcessSpawnQueue();
        }

        void ProcessSpawn()
        {
            if (currentWave.batches.Count == 0)
            {
                spawning = false;
                onWaveSpawned.Broadcast();
                return;
            }

            WaveGenerator.Batch batch = currentWave.batches[0];
            if (batch.count <= 0)
                throw new("Batch cannot be empty!");
            batch.count--;
            if (batch.count == 0)
            {
                currentWave.batches.RemoveAt(0);
                spawnTimer = -AttackerStats.Spacing.BatchSpacing.GetTicks();
            }
            else
            {
                spawnTimer = -batch.spacing.GetTicks();
            }

            for (int i = 0; i < paths_; i++)
            {
                if (batch.typePerPath[i] is AttackerStats s)
                    spawnQueue_.Add((paths_ - i, i, s));
            }
            onSpawnedOnce.Invoke();
        }

        void ProcessSpawnQueue()
        {
            spawnQueue_ = spawnQueue_.OrderBy(e => e.delay).ToList();
            while (spawnQueue_.Count > 0 && spawnQueue_[0].delay == 0)
            {
                (int _, int path, var attacker) = spawnQueue_[0];
                spawnQueue_.RemoveAt(0);
                Spawn(attacker, path);
            }

            for (int i = 0; i < spawnQueue_.Count; i++)
            {
                var e = spawnQueue_[i];
                e.delay--;
                spawnQueue_[i] = e;
            }
        }

        void Spawn(AttackerStats attacker, int path)
        {
            uint index = ++currentIndex;
            Attacker a = Instantiate(attacker.prefab, transform).GetComponent<Attacker>();
            Vector2Int startingPoint = World.WorldData.World.data.pathStarts[path];
            Vector2Int firstTile = World.WorldData.World.data.firstPathTiles[path];
            a.InitPath(startingPoint, firstTile, index);
            a.onRemoved.AddListener(AttackerRemoved);
            a.onReachedHub.AddListener(BattleController.AttackerReachedHub);

            attackersLeft++;
        }

        bool StartNextWave()
        {
            wave++;
            spawnTimer = 0;
            spawning = true;
            currentWave = waveGenerator.GetWave(wave);
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

