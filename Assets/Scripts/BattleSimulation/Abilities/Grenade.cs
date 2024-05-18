namespace BattleSimulation.Abilities
{
    public class Grenade : TargetedAbility
    {
        protected override void OnPlaced()
        {
            Explode();
        }

        void Explode()
        {
            foreach (var a in targeting.GetValidTargets())
                a.TryHit(new(Blueprint.damage, Blueprint.damageType, this), out _);
            Destroy(gameObject, 3f);
        }
    }
}