using InfiniteCombo.Nitrogen.Assets.Scripts.Utils;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen.Blockers
{
    [System.Serializable]
    public class Blocker
    {
        public enum Type { Short, Tall, Crystals, Minerals }

        public string name;
        public bool enabled;
        public Type type;
        public int layer;
        public int min;
        public int max;
        public float baseProbability;
        public WorldUtils.TerrainType[] validTerrainTypes;
        public bool onSlants;
        public float[] forces;
        public string copyModules;
        //[SerializeReference, SubclassSelector] public Scatterer.ScattererObjectModule[] scattererModules;

        public int placed;
    }
}
