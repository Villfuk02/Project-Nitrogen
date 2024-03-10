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
        public static ModifiableCommand startWave = new();
        public static EventReactionChain onWaveSpawned = new();
        public static EventReactionChain onWaveFinished = new();
        [Header("References")]
        public WaveGenerator waveGenerator;
        [Header("Settings")]
        [SerializeField] UnityEvent onSpawnedOnce;
        [Header("Runtime variables")]
        [SerializeField] int spawnTimer;
        [SerializeField] uint currentIndex;
        public int wave;
        public int attackersLeft;
        [SerializeField] bool startNextWave;
        [SerializeField] bool spawning;
        [SerializeField] bool waveStarted;
        [SerializeField] WaveGenerator.Wave currentWave;
        int paths_;

        struct QueuedAttacker
        {
            public AttackerStats attacker;
            public int path;
            public int delay;
        }
        List<QueuedAttacker> spawnQueue_ = new();

        void Awake()
        {
            // only use half the range to prevent overflow
            currentIndex = (uint)(World.WorldData.World.data.seed & 0x7FFFFFFF);
            paths_ = World.WorldData.World.data.firstPathTiles.Length;

            startWave.RegisterHandler(StartNextWave);
        }

        void OnDestroy()
        {
            startWave.UnregisterHandler(StartNextWave);
        }

        void Update()
        {
            if (attackersLeft == 0 && !spawning && spawnQueue_.Count == 0 && Input.GetKeyUp(KeyCode.Return))
                startNextWave = true;
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

            if (attackersLeft == 0 && !spawning && waveStarted)
            {
                waveStarted = false;
                onWaveFinished.Broadcast();
            }
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
                throw new("Batch cannot be empty");
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
                {
                    spawnQueue_.Add(new() { attacker = s, path = i, delay = paths_ - i });
                    attackersLeft++;
                }
            }
            onSpawnedOnce.Invoke();
        }

        void ProcessSpawnQueue()
        {
            spawnQueue_ = spawnQueue_.OrderBy(e => e.delay).ToList();
            while (spawnQueue_.Count > 0 && spawnQueue_[0].delay == 0)
            {
                int path = spawnQueue_[0].path;
                var attacker = spawnQueue_[0].attacker;
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
            Attacker a = Instantiate(attacker.prefab, transform).GetComponent<Attacker>();
            Vector2Int startingPoint = World.WorldData.World.data.pathStarts[path];
            Vector2Int firstTile = World.WorldData.World.data.firstPathTiles[path];
            a.InitPath(startingPoint, firstTile, ++currentIndex);
            a.onRemoved.AddListener(AttackerRemoved);
            a.onReachedHub.AddListener(BattleController.AttackerReachedHub);
        }

        bool StartNextWave()
        {
            wave++;
            spawnTimer = 0;
            spawning = true;
            waveStarted = true;
            currentWave = waveGenerator.GetWave(wave);
            return true;
        }

        public void AttackerRemoved()
        {
            attackersLeft--;
        }
    }
}

