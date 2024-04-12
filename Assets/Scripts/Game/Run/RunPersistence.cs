using Game.Run.Events;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = Utils.Random.Random;

namespace Game.Run
{
    public class RunPersistence : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] LevelSetter worldSetter;
        [SerializeField] RunEvents runEvents;
        [SerializeField] BlueprintRewardController blueprintRewards;
        [Header("Settings")]
        public ulong runSeed;
        public string seedString;
        [SerializeField] int startMaxHull;
        public List<Blueprint.Blueprint> blueprints;
        [Header("Runtime variables")]
        public int level;
        public int MaxHull { get; private set; }
        public int Hull { get; private set; }
        Random random_;

        [Header("Cheats")]
        [SerializeField] bool cheatAddHull;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);

            runEvents.damageHull.RegisterHandler(DamageHull);
            runEvents.repairHull.RegisterHandler(RepairHull);
            runEvents.finishLevel.RegisterHandler(FinishLevelCommand);
            runEvents.quit.RegisterHandler(Quit);
        }

        public void Init()
        {
            random_ = new(runSeed);
            MaxHull = startMaxHull;
            Hull = MaxHull;
            blueprintRewards.Init(random_.NewSeed());
        }

        void Update()
        {
            if (cheatAddHull)
            {
                cheatAddHull = false;
                MaxHull += 1000;
                Hull += 1000;
            }
        }

        bool DamageHull(ref int dmg)
        {
            if (dmg <= 0)
                return false;
            Hull -= dmg;
            if (Hull <= 0)
                runEvents.defeat.Invoke();
            return true;
        }

        bool RepairHull(ref int r)
        {
            if (MaxHull - Hull < r)
                r = MaxHull - Hull;
            if (r <= 0)
                return false;
            Hull += r;
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
