using UnityEngine;

namespace World
{
    public class World : MonoBehaviour
    {
        public static World instance;
        public static WorldData.WorldData data;
        public WorldData.WorldData worldData;

        public bool Ready { get; private set; }

        void Awake()
        {
            instance = this;
        }

        void FixedUpdate()
        {
            if (Time.fixedDeltaTime != 0.05f)
                Debug.LogWarning(Time.fixedDeltaTime);
        }

        public void SetReady()
        {
            data = worldData;
            Ready = true;
        }
    }
}
