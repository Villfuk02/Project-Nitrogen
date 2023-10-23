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

        public static GameCommand<(object source, float amount)> addMaterial = new();
        public static GameCommand<(object source, float amount)> addEnergy = new();
        public static GameCommand<(object source, float amount)> addFuel = new();
        public static GameQuery<float> canSpendMaterial = new();
        public static GameCommand<float> spendMaterial = new();

        void Awake()
        {
            addMaterial.Register(AddMaterial, 0);
            addEnergy.Register(AddEnergy, 0);
            addFuel.Register(AddFuel, 0);
            canSpendMaterial.RegisterAcceptor(CanSpendMaterial);
            spendMaterial.Register(SpendMaterial, 0);
        }

        void OnDestroy()
        {
            addMaterial.Unregister(AddMaterial);
            addEnergy.Unregister(AddEnergy);
            addFuel.Unregister(AddFuel);
            canSpendMaterial.UnregisterAcceptor();
            spendMaterial.Unregister(SpendMaterial);
        }

        //TODO: hook up with initializing a level/battle
        void Start()
        {
            Material = 25;
            Energy = 0;
            MaxEnergy = 30;
            Fuel = 0;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                Material += 100;
                Energy += 100;
            }
        }

        public static bool AdjustAndTrySpendMaterial(int amount)
        {
            float amountCalculation = amount;
            if (!canSpendMaterial.Query(ref amountCalculation))
                return false;
            spendMaterial.Invoke(amountCalculation);
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

        bool SpendMaterial(ref float amount)
        {
            if (amount < 0)
                throw new ArgumentException("amount cannot be negative!");
            int realAmount = Mathf.FloorToInt(amount);
            amount = realAmount;
            if (realAmount > Material)
                throw new("CANNOT AFFORD!");
            Material -= realAmount;
            return true;
        }

        bool CanSpendMaterial(ref float amount)
        {
            int realAmount = Mathf.FloorToInt(amount);
            amount = realAmount;
            return realAmount <= Material;
        }
    }
}
