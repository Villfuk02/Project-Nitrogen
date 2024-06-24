using System.Collections.Generic;
using UnityEngine;

namespace Game.Blueprint
{
    public abstract class Blueprinted : MonoBehaviour, IBlueprintProvider
    {
        [Header("References")]
        [SerializeField] GameObject visuals;
        protected Blueprint originalBlueprint;
        public Blueprint baseBlueprint;
        public Blueprint currentBlueprint;
        public bool Placed { get; private set; }

        public void InitBlueprint(Blueprint blueprint)
        {
            visuals.SetActive(false);
            originalBlueprint = blueprint;
            baseBlueprint = Blueprint.CloneModifiedValues(blueprint);
            currentBlueprint = Blueprint.CloneModifiedValues(this);
            OnInit();
        }

        public void Place()
        {
            Placed = true;
            visuals.SetActive(true);
            OnPlaced();
        }

        protected virtual void OnPlaced() { }
        protected virtual void OnInit() { }

        public void OnSetupPlacement()
        {
            currentBlueprint = Blueprint.CloneModifiedValues(this);
        }

        public virtual IEnumerable<string> GetExtraStats()
        {
            yield break;
        }

        protected virtual void FixedUpdate()
        {
            currentBlueprint = Blueprint.CloneModifiedValues(this);
        }

        public Blueprint GetBaseBlueprint() => baseBlueprint;
    }
}