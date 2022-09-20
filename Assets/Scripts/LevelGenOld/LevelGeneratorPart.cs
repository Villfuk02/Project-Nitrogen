using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGenOld
{
    public abstract class LevelGeneratorPart : MonoBehaviour
    {
        [System.NonSerialized]
        public bool started;
        [System.NonSerialized]
        public bool stopped;

        public abstract void Init();
    }
}
