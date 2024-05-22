using System.Collections.Generic;
using System.Linq;
using Game.Run.Shared;
using UnityEngine;
using Random = Utils.Random.Random;

namespace Game.Run
{
    public class RunPersistence : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] LevelSetter worldSetter;
        [SerializeField] BlueprintRewardController blueprintRewards;
        [SerializeField] LevelDisplay levelDisplay;
        [Header("Settings")]
        public ulong runSeed;
        public string seedString;
        [SerializeField] ulong tutorialLevelSeed;
        [Header("Runtime variables")]
        public List<Blueprint.Blueprint> blueprints;
        public int level;
        public int maxHull;
        public int hull;
        Random random_;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);

            RunEvents.InitEvents();
            RunEvents.damageHull.RegisterHandler(DamageHull);
            RunEvents.repairHull.RegisterHandler(RepairHull);
            RunEvents.finishLevel.RegisterHandler(FinishLevelCommand);
            RunEvents.quit.RegisterHandler(Quit);
        }

        public void Init(bool noStartingBlueprints)
        {
            random_ = new(runSeed);
            hull = maxHull;
            blueprintRewards.Init(random_.NewSeed());
            if (!noStartingBlueprints)
                blueprints = blueprintRewards.allBlueprints.Where(b => b.rarity == Blueprint.Blueprint.Rarity.Starter).ToList();
        }

        bool DamageHull(ref int dmg)
        {
            if (dmg <= 0)
                return false;
            hull -= dmg;
            if (hull <= 0)
                RunEvents.defeat.Invoke();
            return true;
        }

        bool RepairHull(ref int r)
        {
            if (maxHull - hull < r)
                r = maxHull - hull;
            if (r <= 0)
                return false;
            hull += r;
            return true;
        }

        bool FinishLevelCommand()
        {
            if (level == 0)
                blueprintRewards.MakeTutorialReward();
            else
                blueprintRewards.MakeBlueprintReward();
            return true;
        }

        public void NextLevel()
        {
            if (level == 0)
                PersistentData.FinishedTutorial = true;
            level++;
            SceneController.ChangeScene(SceneController.Scene.Battle, true, false, "GENERATING...", () => levelDisplay.enabled = true);
        }

        public void SetupLevel()
        {
            var levelSeed = level != 0 ? random_.NewSeed() : tutorialLevelSeed;
            worldSetter.SetupLevel(levelSeed, level);
        }

        public bool Quit()
        {
            SceneController.ChangeScene(SceneController.Scene.Menu, true, true, "", () => Destroy(gameObject));
            return true;
        }
    }
}