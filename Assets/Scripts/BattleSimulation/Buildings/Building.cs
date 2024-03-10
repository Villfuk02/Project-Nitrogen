using Game.Blueprint;
using System;
using UnityEngine;

namespace BattleSimulation.Buildings
{
    public abstract class Building : MonoBehaviour, IBlueprinted
    {
        [Header("References")]
        [SerializeField] GameObject visuals;
        public Transform[] rotateBack;
        [Header("Settings")]
        public bool permanent;
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

        public virtual string? GetExtraStats() => null;

        public void Delete()
        {
            if (permanent)
                throw new InvalidOperationException("Cannot delete permanent building");
            Destroy(gameObject);
        }
        protected virtual void OnDestroy() { }
    }
}
