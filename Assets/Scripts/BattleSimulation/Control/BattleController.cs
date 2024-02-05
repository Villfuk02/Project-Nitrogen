using BattleSimulation.Attackers;
using Game.Run;
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

        public static GameCommand<(object source, float amount)> addMaterial = new();
        public static GameCommand<(object source, float amount)> addEnergy = new();
        public static GameCommand<(object source, float amount)> addFuel = new();
        public static GameQuery<(float priceEnergy, float priceMaterials, int toSpendEnergy, int toSpendMaterials)> canAfford = new();
        public static GameCommand<(int energy, int materials)> spend = new();
        public static GameCommand<float> updateMaterialsPerWave = new();
        public static GameCommand<float> updateEnergyPerWave = new();
        public static GameCommand<float> updateFuelPerWave = new();
        public static GameCommand winLevel = new();
        public static RunPersistence runPersistence;
        public int HullDmgTaken { get; private set; }
        public bool Won { get; private set; }
        public bool Lost { get; private set; }

        public enum Affordable { Yes, UseMaterialsAsEnergy, No }

        void Awake()
        {
            addMaterial.Register(AddMaterial, 0);
            addEnergy.Register(AddEnergy, 0);
            addFuel.Register(AddFuel, 0);
            canAfford.RegisterAcceptor(CanAfford);
            spend.Register(Spend, 0);
            winLevel.Register(Win, 0);
            runPersistence = GameObject.FindGameObjectWithTag("RunPersistence").GetComponent<RunPersistence>();
            runPersistence.damageHull.Register(OnHullDmgTaken, 1000);
            runPersistence.repairHull.Register(OnHullRepaired, 1000);
            WaveController.startWave.Register(OnWaveStarted, 1);
            WaveController.startWave.Register(CanStartWave, -1000);
            runPersistence.defeat.Register(Lose, 1000);
        }

        void OnDestroy()
        {
            addMaterial.Unregister(AddMaterial);
            addEnergy.Unregister(AddEnergy);
            addFuel.Unregister(AddFuel);
            canAfford.UnregisterAcceptor();
            spend.Unregister(Spend);
            winLevel.Unregister(Win);
            runPersistence.damageHull.Unregister(OnHullDmgTaken);
            runPersistence.repairHull.Unregister(OnHullRepaired);
            WaveController.startWave.Unregister(OnWaveStarted);
            WaveController.startWave.Unregister(CanStartWave);
            runPersistence.defeat.Unregister(Lose);
        }

        //TODO: hook up with initializing a level/battle
        void Start()
        {
            Material = 25;
            Energy = 0;
            MaxEnergy = 30;
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

        public static Affordable CanAfford(int energy, int materials)
        {
            (float priceEnergy, float priceMaterials, int toSpendEnergy, int toSpendMaterials) queryParam = (energy, materials, 0, 0);
            if (!canAfford.Query(ref queryParam))
                return Affordable.No;
            return queryParam.priceEnergy == queryParam.toSpendEnergy ? Affordable.Yes : Affordable.UseMaterialsAsEnergy;
        }

        public static bool AdjustAndTrySpend(int energy, int materials)
        {
            (float priceEnergy, float priceMaterials, int toSpendEnergy, int toSpendMaterials) queryParam = (energy, materials, 0, 0);
            if (!canAfford.Query(ref queryParam))
                return false;
            spend.Invoke((queryParam.toSpendEnergy, queryParam.toSpendMaterials));
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

        bool CanAfford(ref (float priceEnergy, float priceMaterials, int toSpendEnergy, int toSpendMaterials) param)
        {
            param.toSpendEnergy = Mathf.FloorToInt(param.priceEnergy);
            param.priceEnergy = param.toSpendEnergy;
            param.toSpendMaterials = Mathf.FloorToInt(param.priceMaterials);
            param.priceMaterials = param.toSpendMaterials;

            if (param.toSpendEnergy > Energy)
            {
                param.toSpendMaterials += param.toSpendEnergy - Energy;
                param.toSpendEnergy = Energy;
            }
            return param.toSpendMaterials <= Material;
        }

        public static void AttackerReachedHub(Attacker attacker)
        {
            runPersistence.damageHull.Invoke(attacker.stats.size switch
            {
                Game.AttackerStats.AttackerStats.Size.Small => 1,
                Game.AttackerStats.AttackerStats.Size.Large => 2,
                Game.AttackerStats.AttackerStats.Size.Boss => 1000,
                _ => throw new ArgumentOutOfRangeException()
            });
        }

        bool OnHullDmgTaken(ref int dmg)
        {
            HullDmgTaken += dmg;
            return true;
        }

        bool OnHullRepaired(ref int repaired)
        {
            HullDmgTaken -= repaired;
            return true;
        }

        bool CanStartWave() => !Won && !Lost;

        bool OnWaveStarted()
        {
            HullDmgTaken = 0;
            return true;
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
            runPersistence.NextLevel();
        }

        bool Lose()
        {
            Lost = true;
            return true;
        }
    }
}
