using System;
using Game.Blueprint;
using Game.Shared;
using UnityEngine;

namespace BattleSimulation.Buildings
{
    public abstract class Building : Blueprinted
    {
        [Header("References")]
        public Transform[] rotateBack;
        [Header("Settings")]
        public bool permanent;

        protected override void OnPlaced()
        {
            if (!permanent)
                SoundController.PlaySound(SoundController.Sound.Build, 0.8f, 1, 0.1f, transform.position, SoundController.Priority.High);
            base.OnPlaced();
        }

        public void Delete()
        {
            if (permanent)
                throw new InvalidOperationException("Cannot delete a permanent building.");
            Destroy(gameObject);
        }

        protected virtual void OnDestroy() { }
    }
}