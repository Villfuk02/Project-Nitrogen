using Blueprints;

namespace Buildings.Simulation.Towers
{
    public class TestTower : BasicProjectileTower
    {
        void Start()
        {
            InitBlueprint(new("Test Tower", Blueprint.BlueprintRarity.Starter, new() { { Blueprint.Stat.Range, 70 }, { Blueprint.Stat.Damage, 5 }, { Blueprint.Stat.ShotInterval, 10 } }));
        }
    }
}
