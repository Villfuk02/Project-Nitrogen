using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Game.Blueprint
{
    public abstract class Blueprinted : MonoBehaviour
    {
        public static readonly ModifiableQuery<(Blueprinted blueprinted, Blueprint blueprint), Blueprint> GET_BLUEPRINT = new();

        static Blueprinted()
        {
            GET_BLUEPRINT.RegisterAcceptor(param => param.blueprint);
        }

        [Header("References")]
        [SerializeField] GameObject visuals;

        [Header("Runtime variables")]
        protected Blueprint baseBlueprint;

        public Blueprint Blueprint { get; private set; }

        public Blueprint OriginalBlueprint { get; private set; }
        public bool Placed { get; private set; }

        [SerializeField] int lastBlueprintHash;

        public void InitBlueprint(Blueprint blueprint)
        {
            visuals.SetActive(false);
            OriginalBlueprint = blueprint;
            baseBlueprint = blueprint.Clone();
            UpdateBlueprint();
            OnInit();
            OnSetupChanged();
        }

        public void Place()
        {
            Placed = true;
            visuals.SetActive(true);
            OnPlaced();
        }

        protected virtual void OnPlaced() { }
        public virtual void OnSetupChanged() { }
        protected virtual void OnInit() { }

        public virtual IEnumerable<string> GetExtraStats()
        {
            yield break;
        }

        void FixedUpdate()
        {
            UpdateBlueprint();
            int hash = Blueprint.GetStatsHash();
            if (lastBlueprintHash != hash)
            {
                lastBlueprintHash = hash;
                OnSetupChanged();
            }

            FixedUpdateInternal();
            UpdateBlueprint();
        }

        protected virtual void FixedUpdateInternal() { }

        void UpdateBlueprint()
        {
            Blueprint = GET_BLUEPRINT.Query((this, baseBlueprint.Clone()));
        }
    }
}