using System;
using System.Collections.Generic;
using System.Linq;
using BattleSimulation.Attackers;
using Game.AttackerStats;
using Game.Shared;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Utils;

namespace BattleSimulation.Control
{
    public class WaveController : MonoBehaviour
    {
        public static readonly ModifiableCommand START_WAVE = new();
        public static readonly EventReactionChain ON_WAVE_SPAWNED = new();
        public static readonly EventReactionChain ON_WAVE_FINISHED = new();
        [Header("References")]
        public WaveGenerator waveGenerator;
        public Button nextWaveButton;
        [Header("Settings")]
        [SerializeField] UnityEvent onSpawnedOnce;
        [SerializeField] UnityEvent<AttackerStats> showNewAttacker;
        [Header("Runtime variables")]
        [SerializeField] int spawnTimer;
        [SerializeField] uint[] currentIndexes;
        public int wave;
        public int attackersLeft;
        [SerializeField] bool startNextWave;
        [SerializeField] bool spawning;
        [SerializeField] bool waveStarted;
        [SerializeField] WaveGenerator.Wave currentWave;
        int paths_;
        public bool newAttacker;
        public bool disableNextWave;

        struct QueuedAttacker
        {
            public AttackerStats attacker;
            public int path;
            public int delay;
        }

        List<QueuedAttacker> spawnQueue_ = new();

        void Awake()
        {
            paths_ = World.WorldData.World.data.firstPathTiles.Length;

            // only use half the range to prevent overflow
            var startIndex = (uint)(World.WorldData.World.data.seed & 0x7FFFFFFF);
            // the extra entry is for attackers spawned during a wave
            currentIndexes = new uint[paths_ + 1];
            for (int i = 0; i < paths_; i++)
                currentIndexes[i] = startIndex;

            START_WAVE.RegisterHandler(StartNextWave);

            Attacker.spawnAttackersRelative = SpawnRelative;

            CheckForNewAttacker();
        }

        void OnDestroy()
        {
            START_WAVE.UnregisterHandler(StartNextWave);
        }

        void Update()
        {
            if (Input.GetKeyUp(KeyCode.Space))
                RequestWaveStart();
        }

        void FixedUpdate()
        {
            if (startNextWave)
            {
                startNextWave = false;
                START_WAVE.Invoke();
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
                nextWaveButton.interactable = true;
                CheckForNewAttacker();
                ON_WAVE_FINISHED.Broadcast();
            }
        }

        void ProcessSpawn()
        {
            if (currentWave.batches.Count == 0)
            {
                spawning = false;
                ON_WAVE_SPAWNED.Broadcast();
                return;
            }

            WaveGenerator.Batch batch = currentWave.batches[0];
            if (batch.count <= 0)
                throw new InvalidOperationException("Cannot spawn an empty batch of attackers");
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
            a.Init(attacker.Clone(), startingPoint, firstTile, ++currentIndexes[path]);
            a.onRemoved.AddListener(AttackerRemoved);
            a.onReachedHub.AddListener(BattleController.AttackerReachedHub);
        }

        public void SpawnRelative(Attacker parent, AttackerStats stats, float progressOffset)
        {
            attackersLeft++;
            Attacker a = Instantiate(stats.prefab, transform).GetComponent<Attacker>();
            a.Init(stats.Clone(), parent.startPosition, parent.firstNode, parent.startPathSplitIndex);
            a.onRemoved.AddListener(AttackerRemoved);
            a.onReachedHub.AddListener(BattleController.AttackerReachedHub);
            float progress = World.WorldData.World.data.tiles[parent.firstNode].dist + 1 - parent.GetDistanceToHub();
            a.AddPathProgress(progress + progressOffset, ++currentIndexes[^1]);
        }

        public void SpawnRelative(Attacker attacker, AttackerStats stats, int count, float offsetRadius)
        {
            if (count == 1)
            {
                SpawnRelative(attacker, stats, 0);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                SpawnRelative(attacker, stats, Mathf.Lerp(-offsetRadius, offsetRadius, i / (float)(count - 1)));
            }
        }

        bool StartNextWave()
        {
            wave++;
            spawnTimer = 0;
            spawning = true;
            waveStarted = true;
            currentWave = waveGenerator.GetWave(wave);
            nextWaveButton.interactable = false;
            SoundController.PlaySound(SoundController.Sound.WaveStart, 0.25f, 1, 0, null, SoundController.Priority.High);
            return true;
        }

        public void AttackerRemoved()
        {
            attackersLeft--;
        }

        void CheckForNewAttacker()
        {
            var attacker = waveGenerator.GetWave(wave + 1)?.newAttacker;
            newAttacker = attacker != null && !PersistentData.IsAttackerKnown(attacker.name);
        }

        public void RequestWaveStart()
        {
            if (disableNextWave)
                return;

            if (newAttacker)
            {
                var attacker = waveGenerator.GetWave(wave + 1).newAttacker!;
                showNewAttacker.Invoke(attacker);
                PersistentData.RegisterKnownAttacker(attacker.name);
                newAttacker = false;
                SoundController.PlaySound(SoundController.Sound.Notification, 0.45f, 1, 0, null, SoundController.Priority.High);
                return;
            }

            if (attackersLeft == 0 && !spawning && spawnQueue_.Count == 0)
                startNextWave = true;
        }
    }
}