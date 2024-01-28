using UnityEngine;
using Utils;

namespace Game.Run
{
    public class RunPersistence : MonoBehaviour
    {
        public int MaxHull { get; private set; }
        public int Hull { get; private set; }
        public int level;


        public GameCommand<int> damageHull = new();
        public GameCommand<int> repairHull = new();

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            MaxHull = 10;
            Hull = MaxHull;

            damageHull.Register(DamageHull, 0);
            repairHull.Register(RepairHull, 0);
        }

        bool DamageHull(ref int dmg)
        {
            if (dmg <= 0)
                return false;
            Hull -= dmg;
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
    }
}
