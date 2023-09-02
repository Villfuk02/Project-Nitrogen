using Blueprints;
using UnityEngine;

namespace Buildings.Simulation
{
    public abstract class Building : MonoBehaviour, IBlueprinted
    {
        public Blueprint Blueprint { get; private set; }
        public void InitBlueprint(Blueprint blueprint)
        {
            Blueprint = blueprint;
            OnInitBlueprint();
        }

        protected virtual void OnInitBlueprint() { }
    }
}
