using Attackers.Simulation;
using Blueprints;

namespace Buildings.Simulation.Towers
{
    public class TestTower : BasicProjectileTower
    {
        void Start()
        {
            InitBlueprint(new("Test Tower", Blueprint.BlueprintRarity.Starter, new()
            {
                { Blueprint.Stat.Range, 70 },
                { Blueprint.Stat.Damage, 3 },
                { Blueprint.Stat.DamageType, (int)Damage.Type.Physical },
                { Blueprint.Stat.ShotInterval, 10 }
            }));
        }
    }
}
