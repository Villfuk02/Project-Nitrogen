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

        public enum Affordable { Yes, UseMaterialsAsEnergy, No }

        void Awake()
        {
            addMaterial.Register(AddMaterial, 0);
            addEnergy.Register(AddEnergy, 0);
            addFuel.Register(AddFuel, 0);
            canAfford.RegisterAcceptor(CanAfford);
            spend.Register(Spend, 0);
        }

        void OnDestroy()
        {
            addMaterial.Unregister(AddMaterial);
            addEnergy.Unregister(AddEnergy);
            addFuel.Unregister(AddFuel);
            canAfford.UnregisterAcceptor();
            spend.Unregister(Spend);
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
            return true;
        }
        bool AddFuel(ref (object source, float amount) param)
        {
            if (param.amount < 0)
                throw new ArgumentException("amount cannot be negative!");
            int realAmount = Mathf.FloorToInt(param.amount);
            param.amount = realAmount;
            Fuel += realAmount;
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
    }
}
