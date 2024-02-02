using Game.Blueprint;
using UnityEngine;

namespace BattleSimulation.Buildings
{
    public abstract class Building : MonoBehaviour, IBlueprinted
    {
        public Blueprint Blueprint { get; private set; }
        public Blueprint OriginalBlueprint { get; private set; }
        public bool placed;
        public bool permanent;
        [SerializeField] GameObject visuals;
        public Transform[] rotateBack;

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

        public virtual string? GetExtraStats() => null;

        public void Delete()
        {
            if (permanent)
                Debug.LogError("Cannot delete permanent building");
            Destroy(gameObject);
        }
        protected virtual void OnDestroy() { }
    }
}
