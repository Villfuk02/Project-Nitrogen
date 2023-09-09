using Game.Blueprint;
using UnityEngine;

namespace BattleSimulation.Buildings
{
    public abstract class Building : MonoBehaviour, IBlueprinted
    {
        public Blueprint Blueprint { get; private set; }
        public bool placed;
        [SerializeField] GameObject visuals;
        public void InitBlueprint(Blueprint blueprint)
        {
            visuals.SetActive(false);
            Blueprint = blueprint;
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
