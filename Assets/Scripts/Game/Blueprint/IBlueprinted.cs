using System.Collections.Generic;

namespace Game.Blueprint
{
    public interface IBlueprinted
    {
        public Blueprint Blueprint { get; }
        public Blueprint OriginalBlueprint { get; }
        public bool Placed { get; }
        public void InitBlueprint(Blueprint blueprint);
        public void Place();
        public IEnumerable<string> GetExtraStats();
    }
}
