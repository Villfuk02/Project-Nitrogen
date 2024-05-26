using System;
using System.Collections.Generic;
using BattleSimulation.Attackers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BattleVisuals.Towers
{
    public class StaticSparker : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameObject sparkPrefab;
        [Header("Settings")]
        [SerializeField] float sparkDuration;
        [SerializeField] Gradient sparkColor;
        [SerializeField] int vertices;
        [Header("Runtime variables")]
        readonly Dictionary<SparkTime, List<LineRenderer>> sparks_ = new();
        SparkTime? current_;

        class SparkTime
        {
            public float time;
        }

        void Update()
        {
            current_ = null;
            List<SparkTime> toRemove = new();
            foreach (var (time, sparks) in sparks_)
            {
                time.time += Time.deltaTime;
                if (time.time <= sparkDuration)
                {
                    Color c = sparkColor.Evaluate(time.time / sparkDuration);
                    foreach (var spark in sparks)
                        SetSparkColor(spark, c);
                }
                else
                {
                    toRemove.Add(time);
                }
            }

            foreach (var time in toRemove)
            {
                foreach (var spark in sparks_[time])
                    Destroy(spark.gameObject);
                sparks_.Remove(time);
            }
        }


        public void Spark((Transform origin, Attacker a) param)
        {
            var spark = Instantiate(sparkPrefab, transform).GetComponent<LineRenderer>();
            var positions = new Vector3[vertices];
            var origin = param.origin.position;
            var target = param.a.target.position;
            for (int i = 0; i < vertices; i++)
            {
                positions[i] = Vector3.Lerp(origin, target, i / (float)(vertices - 1));
                if (i != 0 && i != vertices - 1)
                    positions[i] += Random.insideUnitSphere * 0.1f;
            }

            spark.positionCount = vertices;
            spark.SetPositions(positions);
            SetSparkColor(spark, sparkColor.Evaluate(0));

            if (current_ is null)
            {
                current_ = new() { time = 0 };
                sparks_.Add(current_, new());
            }

            sparks_[current_].Add(spark);
        }

        static void SetSparkColor(LineRenderer spark, Color color)
        {
            Gradient g = new();
            g.SetKeys(new[] { new GradientColorKey(color, 0) }, Array.Empty<GradientAlphaKey>());
            spark.colorGradient = g;
        }
    }
}