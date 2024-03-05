using BattleSimulation.Buildings;

namespace BattleSimulation.Towers
{
    public abstract class Tower : Building
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
