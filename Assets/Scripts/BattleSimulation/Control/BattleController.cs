using BattleSimulation.Attackers;
using Game.Run.Events;
using System;
using UnityEngine;
using Utils;

namespace BattleSimulation.Control
{
    public class BattleController : MonoBehaviour
    {
        public int Material { get; private set; }
        public int Energy { get; private set; }
        public int MaxEnergy { get; private set; }
        public int Fuel { get; private set; }
        public int FuelGoal { get; private set; }

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
        public int HullDmgTaken { get; private set; }
        public bool Won { get; private set; }
        public bool Lost { get; private set; }

        public enum Affordable { Yes, UseMaterialsAsEnergy, No }

        void Awake()
        {
            addMaterial.RegisterHandler(AddMaterial);
            addEnergy.RegisterHandler(AddEnergy);
            addFuel.RegisterHandler(AddFuel);
            canAfford.RegisterAcceptor(CanAfford);
            spend.RegisterHandler(Spend);
            winLevel.RegisterHandler(Win);
            runEvents = GameObject.FindGameObjectWithTag("RunPersistence").GetComponent<RunEvents>();
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

        void Start()
        {
            Material = 50;
            Energy = 30;
            MaxEnergy = 100;
            Fuel = 0;
            FuelGoal = 120;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                Material += 100;
                Energy += 100;
            }
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
                throw new ArgumentException("amount cannot be negative!");
            int realAmount = Mathf.FloorToInt(param.amount);
            param.amount = realAmount;
            Material += realAmount;
            return true;
        }
        bool AddEnergy(ref (object source, float amount) param)
        {
            if (param.amount < 0)
                throw new ArgumentException("amount cannot be negative!");
            int realAmount = Mathf.FloorToInt(param.amount);
            param.amount = realAmount;
            Energy += realAmount;
            if (Energy > MaxEnergy)
                Energy = MaxEnergy;
            return true;
        }
        bool AddFuel(ref (object source, float amount) param)
        {
            if (param.amount < 0)
                throw new ArgumentException("amount cannot be negative!");
            int realAmount = Mathf.FloorToInt(param.amount);
            param.amount = realAmount;
            if (Fuel >= FuelGoal)
                return true;
            Fuel += realAmount;
            if (Fuel > FuelGoal)
                Fuel = FuelGoal;
            if (Fuel == FuelGoal && !Lost)
                winLevel.Invoke();
            return true;
        }

        bool Spend(ref (int energy, int materials) param)
        {
            if (param.energy < 0 || param.materials < 0)
                throw new ArgumentException("amount cannot be negative!");
            if (param.energy > Energy || param.materials > Material)
                throw new("CANNOT AFFORD!");
            Material -= param.materials;
            Energy -= param.energy;
            return true;
        }

        (Affordable, int energy, int materials) CanAfford((float energy, float materials) param)
        {
            int energy = Mathf.FloorToInt(param.energy);
            int materials = Mathf.FloorToInt(param.materials);

            Affordable result = Affordable.Yes;

            if (energy > Energy)
            {
                materials += energy - Energy;
                energy = Energy;
                result = Affordable.UseMaterialsAsEnergy;
            }

            if (materials > Material)
                result = Affordable.No;

            return (result, energy, materials);
        }

        public static void AttackerReachedHub(Attacker attacker)
        {
            runEvents.damageHull.Invoke(attacker.stats.size switch
            {
                Game.AttackerStats.AttackerStats.Size.Small => 1,
                Game.AttackerStats.AttackerStats.Size.Large => 2,
                Game.AttackerStats.AttackerStats.Size.Boss => 1000,
                _ => throw new ArgumentOutOfRangeException()
            });
        }

        void OnHullDmgTaken(int dmg)
        {
            HullDmgTaken += dmg;
        }

        void OnHullRepaired(int repaired)
        {
            HullDmgTaken -= repaired;
        }

        bool CanStartWave() => !Won && !Lost;

        void OnWaveStarted()
        {
            HullDmgTaken = 0;
        }

        bool Win()
        {
            Won = true;
            return true;
        }

        public void Stay()
        {
            Won = false;
        }

        public void NextLevel()
        {
            runEvents.nextLevel.Invoke();
        }

        void Lose()
        {
            Lost = true;
        }
    }
}
