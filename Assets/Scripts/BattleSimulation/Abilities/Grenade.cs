using Game.Shared;

namespace BattleSimulation.Abilities
{
    public class Grenade : TargetedAbility
    {
        protected override void OnPlaced()
        {
            Explode();
            SoundController.PlaySound(SoundController.Sound.ExplosionSmall, 0.75f, 0.8f, 0.2f, transform.position);
        }

        void Explode()
        {
            foreach (var a in targeting.GetValidTargets())
                a.TryHit(new(Blueprint.damage, Blueprint.damageType, this), out _);
            Destroy(gameObject, 3f);
        }
    }
}