using System.Collections.Generic;

namespace Game.Blueprint
{
    public interface IBlueprintHolder
    {
        public IEnumerable<Blueprint> GetBlueprints();
    }
}