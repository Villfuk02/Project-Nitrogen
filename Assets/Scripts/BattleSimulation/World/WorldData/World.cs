using UnityEngine;

namespace BattleSimulation.World.WorldData
{
    public class World : MonoBehaviour
    {
        public static World instance;
        public static WorldData data;
        public WorldData worldData;

        public bool Ready { get; private set; }

        void Awake()
        {
            instance = this;
        }

        void FixedUpdate()
        {
            if (Time.fixedDeltaTime != 0.05f)
                Debug.LogWarning($"Fixed tick took {Time.fixedDeltaTime}s instead of 0.05s!");
        }

        public void SetReady()
        {
            data = worldData;
            Ready = true;
        }
    }
}
