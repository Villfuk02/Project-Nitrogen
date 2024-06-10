using UnityEngine;

namespace BattleSimulation.World.WorldData
{
    public class World : MonoBehaviour
    {
        public static World instance;
        public static WorldData data;
        public WorldData worldData;

        public static bool Ready { get; private set; }

        void Awake()
        {
            instance = this;
        }

        public static void InitData()
        {
            data = instance.worldData;
        }

        public void SetReady()
        {
            Ready = true;
        }
    }
}