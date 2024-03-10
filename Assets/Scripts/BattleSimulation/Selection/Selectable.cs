using BattleSimulation.Attackers;
using BattleSimulation.World;
using UnityEngine;

namespace BattleSimulation.Selection
{
    [System.Serializable]
    public class Selectable : MonoBehaviour
    {
        [Header("References")]
        public Attacker attacker;
        public Tile tile;
    }
}
