using BattleSimulation.Control;
using System.Collections.Generic;
using UnityEngine;
using WorldGen.WorldSettings;
using Random = Utils.Random.Random;

namespace Game.Run
{
    public class LevelSetter : MonoBehaviour
    {
        public void SetupLevel(ulong randomSeed, int level)
        {
            Random rand = new(randomSeed);
            int paths;
            WorldSettings ws = GameObject.FindGameObjectWithTag("WorldSettings").GetComponent<WorldSettings>();
            if (!ws.overrideRun)
            {
                ws.seed = rand.NewSeed();
                paths = rand.Int(1, Mathf.Clamp(1 + level / 2, 2, 6));
                int minPathLength = Mathf.Max(27 - 3 * level, 9);
                List<int> pathLengths = new();
                for (int i = 0; i < paths; i++)
                {
                    pathLengths.Add(rand.Int(minPathLength, minPathLength + 5 + 2 * i));
                }

                pathLengths.Sort();
                ws.pathLengths = pathLengths.ToArray();
            }
            else
            {
                paths = ws.pathLengths.Length;
            }

            WaveGenerator wg = GameObject.FindGameObjectWithTag("WaveGenerator").GetComponent<WaveGenerator>();
            wg.paths = paths;
            if (!wg.overrideRun)
            {
                wg.baseValueRate = 1.2f + 0.3f * level;
                wg.baseEffectiveValueBuffer = 8 + 2 * level;
                wg.linearScaling = 0.5f;
                wg.quadraticScaling = 1 / 6f;
                wg.cubicScaling = 1 / 60f;
                wg.exponentialScalingBase = 1;
                wg.random = new(rand.NewSeed());
            }
        }
    }
}
