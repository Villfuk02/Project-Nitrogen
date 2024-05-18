using System.Collections.Generic;
using System.Linq;
using Game.Run.Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = Utils.Random.Random;

namespace Game.Run
{
    public class RunPersistence : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] LevelSetter worldSetter;
        [SerializeField] BlueprintRewardController blueprintRewards;
        [Header("Settings")]
        public ulong runSeed;
        public string seedString;
        [Header("Runtime variables")]
        public List<Blueprint.Blueprint> blueprints;
        public int level;
        public int maxHull;
        public int hull;
        Random random_;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);

            RunEvents.damageHull.RegisterHandler(DamageHull);
            RunEvents.repairHull.RegisterHandler(RepairHull);
            RunEvents.finishLevel.RegisterHandler(FinishLevelCommand);
            RunEvents.quit.RegisterHandler(Quit);
        }

        public void Init(bool noStartingBlueprints)
        {
            RunEvents.InitEvents();
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
            Invoke(nameof(FinishLevel), 0.5f);
            return true;
        }

        public void FinishLevel()
        {
            blueprintRewards.MakeBlueprintReward();
        }

        public void NextLevel()
        {
            level++;
            SceneManager.LoadScene("Battle");
        }

        public void SetupLevel()
        {
            worldSetter.SetupLevel(random_.NewSeed(), level);
        }

        public bool Quit()
        {
            SceneManager.LoadScene("Loading");
            Destroy(gameObject);
            return true;
        }
    }
}