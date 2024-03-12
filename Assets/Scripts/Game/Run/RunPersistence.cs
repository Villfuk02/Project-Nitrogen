using Game.Run.Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = Utils.Random.Random;
using UnityRandom = UnityEngine.Random;

namespace Game.Run
{
    public class RunPersistence : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] LevelSetter worldSetter;
        [SerializeField] RunEvents runEvents;
        [Header("Settings")]
        [SerializeField] ulong runSeed;
        [SerializeField] bool randomSeed;
        [SerializeField] int startMaxHull;
        [Header("Runtime variables")]
        public int level;
        public int MaxHull { get; private set; }
        public int Hull { get; private set; }
        Random random_;

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            if (randomSeed)
                runSeed = (ulong)UnityRandom.Range(int.MinValue, int.MaxValue) + ((ulong)UnityRandom.Range(int.MinValue, int.MaxValue) << 32);
            random_ = new(runSeed);

            MaxHull = startMaxHull;
            Hull = MaxHull;

            runEvents.damageHull.RegisterHandler(DamageHull);
            runEvents.repairHull.RegisterHandler(RepairHull);
            runEvents.nextLevel.RegisterHandler(NextLevelCommand);
            runEvents.restart.RegisterHandler(Restart);
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
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

        bool NextLevelCommand()
        {
            Invoke(nameof(NextLevel), 0.5f);
            return true;
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

        public bool Restart()
        {
            SceneManager.LoadScene("Loading");
            Destroy(gameObject);
            return true;
        }
    }
}
