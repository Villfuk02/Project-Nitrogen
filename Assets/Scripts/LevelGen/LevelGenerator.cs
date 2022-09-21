using InfiniteCombo.Nitrogen.Assets.Scripts.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace InfiniteCombo.Nitrogen.Assets.Scripts.LevelGen
{
    public class LevelGenerator : MonoBehaviour
    {
        [Header("Debug Stepping")]
        [SerializeField] bool stepped;
        [SerializeField] StepType stepType;
        [Header("References")]
        [SerializeField] GizmoManager gizmos;
        //[Header("Settings")]
        //[Header("Runtime Values")]
        public enum StepType { None, Phase, Step, Substep }

        void Start()
        {
            StartCoroutine(Generate());
            StartCoroutine(Animate());
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKey(KeyCode.M))
                stepped = false;
        }

        IEnumerator Generate()
        {
            yield break;
        }
        IEnumerator Animate()
        {
            yield break;
        }

        public void RegisterGizmos(StepType duration, Func<List<GizmoManager.GizmoObject>> objectProvider)
        {
            if (duration <= stepType)
            {
                gizmos.Add(duration, objectProvider());
            }
        }
        public void RegisterGizmos(StepType duration, Func<GizmoManager.GizmoObject[]> objectProvider)
        {
            if (duration <= stepType)
            {
                gizmos.Add(duration, objectProvider());
            }
        }
        public void RegisterGizmos(StepType duration, Func<GizmoManager.GizmoObject> objectProvider)
        {
            if (duration <= stepType)
            {
                gizmos.Add(duration, objectProvider());
            }
        }

        bool CanStep(StepType type)
        {
            if (type <= stepType)
            {
                if (stepped)
                    return false;
                gizmos.Expire(type);
                stepped = true;
            }
            return true;
        }

        public void WaitForStep(StepType type)
        {
            while (!CanStep(type))
            {
                Thread.Sleep(15);
            }
        }

    }
}
