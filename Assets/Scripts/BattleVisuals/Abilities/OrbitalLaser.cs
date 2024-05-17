using BattleSimulation.World.WorldData;
using System;
using UnityEngine;
using UnityEngine.Events;
using Utils;

namespace BattleVisuals.Abilities
{
    public class OrbitalLaser : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] BattleSimulation.Abilities.OrbitalLaser orbitalLaser;
        [Header("Settings")]
        [SerializeField] UnityEvent onEnd;
        [Header("Runtime variables")]
        [SerializeField] float duration;
        [SerializeField] float elapsed;
        [SerializeField] bool ended;

        void OnEnable()
        {
            transform.position = Vector3.one * 1000;
        }

        void Start()
        {
            duration = orbitalLaser.Blueprint.durationTicks * TimeUtils.SECS_PER_TICK;
        }

        void Update()
        {
            if (ended)
                return;

            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(orbitalLaser.startPos, orbitalLaser.endPos, elapsed / duration);
            transform.position = new(transform.position.x, World.data.tiles.GetHeightAt(WorldUtils.WorldPosToTilePos(transform.position)) * WorldUtils.HEIGHT_STEP, transform.position.z);

            if (elapsed > duration)
            {
                onEnd.Invoke();
                ended = true;
            }
        }
    }
}