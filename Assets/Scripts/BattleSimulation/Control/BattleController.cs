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

        public static GameEvent<(object source, int amount)> addMaterial = new();
        public static GameEvent<(object source, int amount)> addEnergy = new();
        public static GameEvent<(object source, int amount)> addFuel = new();
        public static GameEvent<int> canSpendMaterial = new();
        public static GameEvent<int> spendMaterial = new();

        void Awake()
        {
            addMaterial.Register(0, AddMaterial);
            addEnergy.Register(0, AddEnergy);
            addFuel.Register(0, AddFuel);
            canSpendMaterial.Register(0, CanSpendMaterial);
            spendMaterial.Register(0, SpendMaterial);
        }

        void OnDestroy()
        {
            addMaterial.Unregister(AddMaterial);
            canSpendMaterial.Unregister(CanSpendMaterial);
            spendMaterial.Unregister(SpendMaterial);
        }

        //TODO: hook up with initializing a level/battle
        void Start()
        {
            Material = 200;
            Energy = 0;
            MaxEnergy = 30;
        }

        public static bool AdjustAndTrySpendMaterial(int amount)
        {
            if (!canSpendMaterial.Invoke(ref amount))
                return false;
            spendMaterial.Invoke(ref amount);
            return true;
        }

        void AddMaterial(ref (object source, int amount) param)
        {
            if (param.amount < 0)
                throw new ArgumentException("amount cannot be negative!");
            Material += param.amount;
        }
        void AddEnergy(ref (object source, int amount) param)
        {
            if (param.amount < 0)
                throw new ArgumentException("amount cannot be negative!");
            Energy += param.amount;
        }
        void AddFuel(ref (object source, int amount) param)
        {
            if (param.amount < 0)
                throw new ArgumentException("amount cannot be negative!");
            Fuel += param.amount;
        }

        void SpendMaterial(ref int amount)
        {
            if (amount < 0)
                throw new ArgumentException("amount cannot be negative!");
            if (amount > Material)
                throw new("CANNOT AFFORD!");
            Material -= amount;
        }

        bool CanSpendMaterial(ref int amount) => amount <= Material;
    }
}
