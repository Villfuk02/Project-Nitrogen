using BattleSimulation.Attackers;
using BattleSimulation.World;
using UnityEngine;

namespace BattleSimulation.Selection
{
    [System.Serializable]
    public class Selectable : MonoBehaviour
    {
        public Attacker attacker;
        public Tile tile;
    }
}
