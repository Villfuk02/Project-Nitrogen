using System.Collections.Generic;
using System.Linq;
using Game.Shared;
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
        [SerializeField] GameObject muteButton;
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
                blueprints = blueprintRewards.allBlueprints.Where(b => b.rarity == Blueprint.Blueprint.Rarity.Starter).OrderBy(b => (b.type, b.materialCost, b.energyCost)).ToList();
        }

        bool DamageHull(ref int dmg)
        {
            if (dmg <= 0)
                return false;
            var prevHull = hull;
            hull -= dmg;


            if (hull <= 0 && prevHull > 0)
                RunEvents.defeat.Invoke();

            else if (hull <= 5 && prevHull > 5)
                SoundController.PlaySound(SoundController.Sound.Siren, 0.3f, 1, 0, null, true);
            else if (hull > 0)
                SoundController.PlaySound(SoundController.Sound.HullLoss, 0.4f, 1, 0.05f, null, true);

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
            SceneController.ChangeScene(SceneController.Scene.Battle, true, false, "GENERATING...", Ready);
        }

        void Ready()
        {
            levelDisplay.enabled = true;
            muteButton.SetActive(true);
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