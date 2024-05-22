using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Run.Shared;
using UnityEngine;
using Utils.Random;
using Random = Utils.Random.Random;

namespace Game.Run
{
    public class BlueprintRewardController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] RunPersistence runPersistence;
        [Header("Settings")]
        public Blueprint.Blueprint[] allBlueprints;
        [SerializeField] int blueprintsPerOffer;
        [SerializeField] float rareChanceIncrement;
        [SerializeField] float legendaryChanceIncrement;
        [SerializeField] Blueprint.Blueprint mortar;
        [Header("Runtime variables")]
        [SerializeField] float rareChance;
        [SerializeField] float legendaryChance;
        RandomSet<Blueprint.Blueprint> commonBlueprints_;
        RandomSet<Blueprint.Blueprint> rareBlueprints_;
        RandomSet<Blueprint.Blueprint> legendaryBlueprints_;
        Random random_;
        List<Blueprint.Blueprint> currentOffer_;
        BlueprintSelectionController currentBlueprintSelectionController_;

        public void Init(ulong seed)
        {
            random_ = new(seed);
            rareChance = rareChanceIncrement;
            legendaryChance = legendaryChanceIncrement;
            commonBlueprints_ = new(random_.NewSeed());
            rareBlueprints_ = new(random_.NewSeed());
            legendaryBlueprints_ = new(random_.NewSeed());
            IncludeBlueprintsInOffers(allBlueprints);
        }

        void IncludeBlueprintsInOffers(IEnumerable<Blueprint.Blueprint> blueprints)
        {
            foreach (var blueprint in blueprints)
            {
                switch (blueprint.rarity)
                {
                    case Blueprint.Blueprint.Rarity.Common:
                        commonBlueprints_.Add(blueprint);
                        break;
                    case Blueprint.Blueprint.Rarity.Rare:
                        rareBlueprints_.Add(blueprint);
                        break;
                    case Blueprint.Blueprint.Rarity.Legendary:
                        legendaryBlueprints_.Add(blueprint);
                        break;
                }
            }
        }

        public void SignalReady(BlueprintSelectionController bsc)
        {
            currentBlueprintSelectionController_ = bsc;
        }

        IEnumerator SetupWhenReady(Action<BlueprintSelectionController> callback)
        {
            currentBlueprintSelectionController_ = null;
            SceneController.ChangeScene(SceneController.Scene.BlueprintSelect, true, true);
            yield return new WaitUntil(() => currentBlueprintSelectionController_ != null);
            callback.Invoke(currentBlueprintSelectionController_);
        }

        public void MakeBlueprintReward()
        {
            currentOffer_ = new();
            for (int i = 0; i < blueprintsPerOffer; i++)
            {
                Blueprint.Blueprint b = SelectABlueprint();
                if (b != null)
                    currentOffer_.Add(b);
            }

            StartCoroutine(SetupWhenReady(bsc => bsc.Setup(runPersistence.blueprints, currentOffer_, null, null, OnFinishedPickingReward, true)));
        }

        public void MakeTutorialReward()
        {
            currentOffer_ = new() { mortar };
            StartCoroutine(SetupWhenReady(bsc => bsc.Setup(runPersistence.blueprints, currentOffer_, null, null, OnFinishedPickingReward, true)));
        }

        public void MakeBlueprintSelection()
        {
            currentOffer_ = allBlueprints.OrderBy(b => b.type).ToList();
            StartCoroutine(SetupWhenReady(bsc => bsc.Setup(runPersistence.blueprints, currentOffer_, null, "Finish", OnFinishedPickingSelection, true)));
        }

        Blueprint.Blueprint SelectABlueprint()
        {
            float selection = random_.Float();
            if (selection <= legendaryChance)
            {
                legendaryChance = legendaryChanceIncrement;
                rareChance += rareChanceIncrement;
                return legendaryBlueprints_.Count > 0 ? legendaryBlueprints_.PopRandom() : null;
            }

            if (selection <= legendaryChance + rareChance)
            {
                legendaryChance += legendaryChanceIncrement;
                rareChance = rareChanceIncrement;
                return rareBlueprints_.Count > 0 ? rareBlueprints_.PopRandom() : null;
            }

            legendaryChance += legendaryChanceIncrement;
            rareChance += rareChanceIncrement;
            return commonBlueprints_.Count > 0 ? commonBlueprints_.PopRandom() : null;
        }

        void OnFinishedPickingReward(Blueprint.Blueprint? addedBlueprint, Blueprint.Blueprint? removedBlueprint)
        {
            UpdateBlueprintLocations(addedBlueprint, removedBlueprint);
            IncludeBlueprintsInOffers(currentOffer_);
            runPersistence.NextLevel();
        }

        void UpdateBlueprintLocations(Blueprint.Blueprint? addedBlueprint, Blueprint.Blueprint? removedBlueprint)
        {
            if (removedBlueprint != null)
                runPersistence.blueprints.Remove(removedBlueprint);
            if (addedBlueprint != null)
            {
                runPersistence.blueprints.Add(addedBlueprint);
                currentOffer_.Remove(addedBlueprint);
            }
        }

        void OnFinishedPickingSelection(Blueprint.Blueprint? addedBlueprint, Blueprint.Blueprint? removedBlueprint)
        {
            if (addedBlueprint == null)
                runPersistence.NextLevel();
            UpdateBlueprintLocations(addedBlueprint, removedBlueprint);
            if (removedBlueprint != null)
                currentOffer_.Add(removedBlueprint);
            currentBlueprintSelectionController_.Setup(runPersistence.blueprints, currentOffer_, null, null, OnFinishedPickingSelection, true);
        }
    }
}