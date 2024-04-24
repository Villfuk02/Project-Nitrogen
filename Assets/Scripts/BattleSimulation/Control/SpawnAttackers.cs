using BattleSimulation.Attackers;
using Game.AttackerStats;
using UnityEngine;
using Utils;

namespace BattleSimulation.Control
{
    public class SpawnAttackers : MonoBehaviour
    {
        [Header("Settings")]
        public Attacker parent;
        public AttackerStats attackersToSpawn;
        public int count;
        public float offsetRadius;
        [Header("Runtime variables")]
        public WaveController waveController;

        void Awake()
        {
            waveController = GameObject.FindGameObjectWithTag(TagNames.WAVE_CONTROLLER).GetComponent<WaveController>();
        }

        public void Spawn()
        {
            waveController.SpawnRelative(parent, attackersToSpawn, count, offsetRadius);
        }
    }
}
