using Game.Blueprint;
using Game.Damage;

namespace BattleSimulation.Towers
{
    public class TestTower : BasicProjectileTower
    {
        void Start()
        {
            InitBlueprint(new("Test Tower", Blueprint.Rarity.Starter) { damage = 1, damageType = Damage.Type.Physical, range = 70, shotInterval = 8 });
        }
    }
}
