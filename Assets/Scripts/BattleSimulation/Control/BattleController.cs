using System;
using BattleSimulation.Attackers;
using Game.Run.Events;
using UnityEngine;
using Utils;

namespace BattleSimulation.Control
{
    public class BattleController : MonoBehaviour
    {
        public enum Affordable
        {
            Yes,
            UseMaterialsAsEnergy,
            No
        }

        public static ModifiableCommand<(object source, float amount)> addMaterial = new();
        public static ModifiableCommand<(object source, float amount)> addEnergy = new();
        public static ModifiableCommand<(object source, float amount)> addFuel = new();
        public static ModifiableQuery<(float energy, float materials), (Affordable affordable, int energy, int materials)> canAfford = new();
        public static ModifiableCommand<(int energy, int materials)> spend = new();
        public static ModifiableCommand<float> updateMaterialsPerWave = new();
        public static ModifiableCommand<float> updateEnergyPerWave = new();
        public static ModifiableCommand<float> updateFuelPerWave = new();
        public static ModifiableCommand winLevel = new();
        public static RunEvents runEvents;

        [Header("Runtime variables")]
        public int material;
        public int energy;
        public int maxEnergy;
        public int fuel;
        public int fuelGoal;

        public int hullDmgTaken;
        public bool won;
        public bool lost;

        void Awake()
        {
            addMaterial.RegisterHandler(AddMaterial);
            addEnergy.RegisterHandler(AddEnergy);
            addFuel.RegisterHandler(AddFuel);
            canAfford.RegisterAcceptor(CanAfford);
            spend.RegisterHandler(Spend);
            winLevel.RegisterHandler(Win);

            runEvents = GameObject.FindGameObjectWithTag(TagNames.RUN_PERSISTENCE).GetComponent<RunEvents>();
            runEvents.damageHull.RegisterReaction(OnHullDmgTaken, 1000);
            runEvents.repairHull.RegisterReaction(OnHullRepaired, 1000);

            WaveController.startWave.RegisterReaction(OnWaveStarted, 1);
            WaveController.startWave.RegisterModifier(CanStartWave, -1000);

            runEvents.defeat.RegisterReaction(Lose, 1000);
        }

        void OnDestroy()
        {
            addMaterial.UnregisterHandler(AddMaterial);
            addEnergy.UnregisterHandler(AddEnergy);
            addFuel.UnregisterHandler(AddFuel);
            canAfford.UnregisterAcceptor(CanAfford);
            spend.UnregisterHandler(Spend);
            winLevel.UnregisterHandler(Win);

            runEvents.damageHull.UnregisterReaction(OnHullDmgTaken);
            runEvents.repairHull.UnregisterReaction(OnHullRepaired);

            WaveController.startWave.UnregisterReaction(OnWaveStarted);
            WaveController.startWave.UnregisterModifier(CanStartWave);

            runEvents.defeat.UnregisterReaction(Lose);
        }

        public static bool AdjustAndTrySpend(int energy, int materials)
        {
            var (affordable, spendEnergy, spendMaterials) = canAfford.Query((energy, materials));
            if (affordable == Affordable.No)
                return false;
            spend.Invoke((spendEnergy, spendMaterials));
            return true;
        }

        bool AddMaterial(ref (object source, float amount) param)
        {
            if (param.amount < 0)
                throw new ArgumentException("Amount cannot be negative");
            int realAmount = Mathf.FloorToInt(param.amount);
            param.amount = realAmount;
            material += realAmount;
            return true;
        }

        bool AddEnergy(ref (object source, float amount) param)
        {
            if (param.amount < 0)
                throw new ArgumentException("Amount cannot be negative");
            int realAmount = Mathf.Min(Mathf.FloorToInt(param.amount), Mathf.Max(maxEnergy - energy, 0));
            param.amount = realAmount;
            energy += realAmount;
            return realAmount > 0;
        }

        bool AddFuel(ref (object source, float amount) param)
        {
            if (param.amount < 0)
                throw new ArgumentException("Amount cannot be negative");
            int realAmount = Mathf.FloorToInt(param.amount);
            param.amount = realAmount;
            if (fuel >= fuelGoal)
                return true;
            fuel += realAmount;
            if (fuel > fuelGoal)
                fuel = fuelGoal;
            if (fuel == fuelGoal && !lost)
                winLevel.Invoke();
            return true;
        }

        bool Spend(ref (int energy, int materials) param)
        {
            if (param.energy < 0 || param.materials < 0)
                throw new ArgumentException("Amount cannot be negative");
            if (param.energy > energy || param.materials > material)
                throw new InvalidOperationException("Cannot afford");
            material -= param.materials;
            energy -= param.energy;
            return true;
        }

        (Affordable, int energy, int materials) CanAfford((float energy, float materials) param)
        {
            int energy = Mathf.FloorToInt(param.energy);
            int materials = Mathf.FloorToInt(param.materials);

            Affordable result = Affordable.Yes;

            if (energy > this.energy)
            {
                materials += energy - this.energy;
                energy = this.energy;
                result = Affordable.UseMaterialsAsEnergy;
            }

            if (materials > material)
                result = Affordable.No;

            return (result, energy, materials);
        }

        public static void AttackerReachedHub(Attacker attacker)
        {
            runEvents.damageHull.Invoke(attacker.stats.size switch
            {
                Game.AttackerStats.AttackerStats.Size.Small => 1,
                Game.AttackerStats.AttackerStats.Size.Large => 3,
                Game.AttackerStats.AttackerStats.Size.Boss => 1000,
                _ => throw new ArgumentOutOfRangeException(nameof(attacker), attacker, null)
            });
        }

        void OnHullDmgTaken(int dmg)
        {
            hullDmgTaken += dmg;
        }

        void OnHullRepaired(int repaired)
        {
            hullDmgTaken -= repaired;
        }

        bool CanStartWave() => !won && !lost;

        void OnWaveStarted()
        {
            hullDmgTaken = 0;
        }

        bool Win()
        {
            won = true;
            return true;
        }

        public void Stay()
        {
            won = false;
        }

        public void FinishLevel()
        {
            runEvents.finishLevel.Invoke();
        }

        void Lose()
        {
            lost = true;
        }
    }
}