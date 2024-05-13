using Game.Blueprint;
using System;
using System.Collections.Generic;
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

        protected virtual void OnInitBlueprint() { }
        protected virtual void OnPlaced() { }

        public virtual IEnumerable<string> GetExtraStats()
        {
            yield break;
        }

        public void Delete()
        {
            if (permanent)
                throw new InvalidOperationException("Cannot delete permanent building");
            Destroy(gameObject);
        }
        protected virtual void OnDestroy() { }
    }
}
