using BattleSimulation.Buildings;
using Game.Damage;

namespace BattleSimulation.Towers
{
    public abstract class Tower : Building, IDamageSource
    {
        public Targeting.Targeting targeting;
        protected int damageDealt;

        protected override void OnInitBlueprint()
        {
            targeting.SetRange(Blueprint.range);
        }

        public override string? GetExtraStats() => $"Damage dealt {damageDealt}";
    }
}
