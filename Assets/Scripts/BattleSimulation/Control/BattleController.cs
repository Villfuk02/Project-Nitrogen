using System;
using BattleSimulation.Attackers;
using Game.AttackerStats;
using Game.Shared;
using UnityEngine;
using Utils;
using Random = Utils.Random.Random;

namespace BattleSimulation.Control
{
    public class BattleController : MonoBehaviour
    {
        public enum Affordable { Yes, UseMaterialsAsEnergy, No }

        public static readonly ModifiableCommand<(object source, float amount)> ADD_MATERIAL = new();
        public static readonly ModifiableCommand<(object source, float amount)> ADD_ENERGY = new();
        public static readonly ModifiableCommand<(object source, float amount)> ADD_FUEL = new();
        public static readonly ModifiableQuery<(int energy, int materials), (float energy, float materials), (Affordable affordable, int energy, int materials)> CAN_AFFORD = new(c => c);
        public static readonly ModifiableCommand<(int energy, int materials)> SPEND = new();
        public static readonly ModifiableQuery<Unit, float, int> MATERIALS_PER_WAVE = new(_ => 0, v => (int)v);
        public static readonly ModifiableQuery<Unit, float, int> ENERGY_PER_WAVE = new(_ => 0, v => (int)v);
        public static readonly ModifiableQuery<Unit, float, int> FUEL_PER_WAVE = new(_ => 0, v => (int)v);
        public static readonly ModifiableCommand WIN_LEVEL = new();

        static Random battleRandom_;

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
            ADD_MATERIAL.RegisterHandler(AddMaterial);
            ADD_ENERGY.RegisterHandler(AddEnergy);
            ADD_FUEL.RegisterHandler(AddFuel);
            CAN_AFFORD.RegisterAcceptor(CanAfford);
            SPEND.RegisterHandler(Spend);
            WIN_LEVEL.RegisterHandler(Win);

            RunEvents.damageHull.RegisterReaction(OnHullDmgTaken, 1000);
            RunEvents.repairHull.RegisterReaction(OnHullRepaired, 1000);

            WaveController.START_WAVE.RegisterReaction(OnWaveStarted, 1);
            WaveController.START_WAVE.RegisterModifier(CanStartWave, -1000);

            RunEvents.defeat.RegisterReaction(Lose, 1000);
        }

        void OnDestroy()
        {
            ADD_MATERIAL.UnregisterHandler(AddMaterial);
            ADD_ENERGY.UnregisterHandler(AddEnergy);
            ADD_FUEL.UnregisterHandler(AddFuel);
            CAN_AFFORD.UnregisterAcceptor(CanAfford);
            SPEND.UnregisterHandler(Spend);
            WIN_LEVEL.UnregisterHandler(Win);

            RunEvents.damageHull.UnregisterReaction(OnHullDmgTaken);
            RunEvents.repairHull.UnregisterReaction(OnHullRepaired);

            WaveController.START_WAVE.UnregisterReaction(OnWaveStarted);
            WaveController.START_WAVE.UnregisterModifier(CanStartWave);

            RunEvents.defeat.UnregisterReaction(Lose);
        }

        public static bool AdjustAndTrySpend(int energy, int materials)
        {
            var (affordable, spendEnergy, spendMaterials) = CAN_AFFORD.Query((energy, materials));
            if (affordable == Affordable.No)
                return false;
            SPEND.Invoke((spendEnergy, spendMaterials));
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
                WIN_LEVEL.Invoke();
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
            int energyCost = Mathf.FloorToInt(param.energy);
            int materialCost = Mathf.FloorToInt(param.materials);

            Affordable result = Affordable.Yes;

            if (energyCost > energy)
            {
                materialCost += energyCost - energy;
                energyCost = energy;
                result = Affordable.UseMaterialsAsEnergy;
            }

            if (materialCost > material)
                result = Affordable.No;

            return (result, energyCost, materialCost);
        }

        public static void AttackerReachedHub(Attacker attacker)
        {
            RunEvents.damageHull.Invoke(attacker.stats.size.GetHullDamage());
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
            SoundController.PlaySound(SoundController.Sound.Victory, 0.1f, 1, 0, null, SoundController.Priority.High);
            return true;
        }

        public void Stay()
        {
            won = false;
        }

        public void FinishLevel()
        {
            RunEvents.finishLevel.Invoke();
        }

        void Lose()
        {
            lost = true;
            SoundController.PlaySound(SoundController.Sound.Defeat, 0.9f, 1, 0, null, SoundController.Priority.High);
        }

        public void InitRandom() => battleRandom_ = new(World.WorldData.World.data.seed ^ 0x0F0F0F0F0F0F0F0F);
        public static Random GetNewRandom() => new(battleRandom_.NewSeed());
    }
}