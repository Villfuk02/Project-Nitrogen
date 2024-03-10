using Game.Blueprint;
using UnityEngine;

namespace BattleSimulation.Abilities
{
    public class Ability : MonoBehaviour, IBlueprinted
    {
        [Header("References")]
        [SerializeField] GameObject visuals;
        [Header("Runtime variables")]
        public bool placed;
        public Blueprint Blueprint { get; private set; }
        public Blueprint OriginalBlueprint { get; private set; }
        public void InitBlueprint(Blueprint blueprint)
        {
            visuals.SetActive(false);
            OriginalBlueprint = blueprint;
            Blueprint = blueprint.Clone();
            OnInitBlueprint();
        }

        public void Placed()
        {
            placed = true;
            visuals.SetActive(true);
            OnPlaced();
        }
        protected virtual void OnInitBlueprint() { }
        protected virtual void OnPlaced() { }
    }
}
