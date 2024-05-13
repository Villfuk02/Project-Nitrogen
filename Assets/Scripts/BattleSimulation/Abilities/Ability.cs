using System.Collections.Generic;
using Game.Blueprint;
using UnityEngine;

namespace BattleSimulation.Abilities
{
    public class Ability : MonoBehaviour, IBlueprinted
    {
        [Header("References")]
        [SerializeField] GameObject visuals;

        // Runtime variables
        public Blueprint Blueprint { get; private set; }
        public Blueprint OriginalBlueprint { get; private set; }
        public bool Placed { get; private set; }

        public void InitBlueprint(Blueprint blueprint)
        {
            visuals.SetActive(false);
            OriginalBlueprint = blueprint;
            Blueprint = blueprint.Clone();
            OnInitBlueprint();
        }

        public void Place()
        {
            Placed = true;
            visuals.SetActive(true);
            OnPlaced();
        }

        public virtual IEnumerable<string> GetExtraStats()
        {
            yield break;
        }

        protected virtual void OnInitBlueprint() { }
        protected virtual void OnPlaced() { }
    }
}
