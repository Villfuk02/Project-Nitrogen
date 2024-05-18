using System;
using Game.Blueprint;
using UnityEngine;

namespace BattleSimulation.Buildings
{
    public abstract class Building : Blueprinted
    {
        [Header("References")]
        public Transform[] rotateBack;
        [Header("Settings")]
        public bool permanent;

        public void Delete()
        {
            if (permanent)
                throw new InvalidOperationException("Cannot delete a permanent building.");
            Destroy(gameObject);
        }

        protected virtual void OnDestroy() { }
    }
}