using System.Collections.Generic;
using System.Linq;
using BattleSimulation.Control;
using UnityEngine;
using Utils;
using WorldGen.WorldSettings;
using Random = Utils.Random.Random;

namespace Game.Run
{
    public class LevelSetter : MonoBehaviour
    {
        public void SetupLevel(ulong randomSeed, int level)
        {
            if (level == 0)
            {
                SetupTutorialSettings(randomSeed);
                return;
            }

            Random rand = new(randomSeed);

            SetupWorldSettings(level, rand, out var pathCount, out var totalPathLength);

            SetupWaveGenerator(level, pathCount, totalPathLength, rand);

            SetupBattleController(level);
        }

        static void SetupWorldSettings(int level, Random rand, out int pathCount, out int totalPathLength)
        {
            WorldSettings ws = GameObject.FindGameObjectWithTag(TagNames.WORLD_SETTINGS).GetComponent<WorldSettings>();
            if (!ws.overrideRun)
            {
                ws.seed = rand.NewSeed();
                int paths;
                int minPathLength;
                if (level % 5 == 0)
                {
                    // every fifth level is just one long path
                    paths = 1;
                    minPathLength = Mathf.Min(50 + level * 2, 110);
                    ws.maxExtraPaths = 0;
                }
                else
                {
                    var maxPaths = Mathf.Min(level / 2 + 1, 5);
                    paths = 1 + rand.Int(0, maxPaths);
                    if (paths == 5 && level > 10)
                        paths = 6;
                    // this formula was selected such that it's 30 for lvl1, 25 for lvl 2, and approaches 10 for lvl -> inf
                    minPathLength = Mathf.RoundToInt(60f / (2 + level)) + 10;
                    ws.maxExtraPaths = level - 1;
                }

                List<int> pathLengths = new();
                for (int i = 0; i < paths; i++)
                {
                    pathLengths.Add(rand.Int(minPathLength, minPathLength + 5));
                }

                pathLengths.Sort();
                ws.pathLengths = pathLengths.ToArray();
                ws.maxHubDistFromCenter = 1.5f + 5f / paths;
            }

            pathCount = ws.pathLengths.Length;
            totalPathLength = ws.pathLengths.Sum();
        }

        static void SetupWaveGenerator(int level, int pathCount, int totalPathLength, Random rand)
        {
            WaveGenerator wg = GameObject.FindGameObjectWithTag(TagNames.WAVE_GENERATOR).GetComponent<WaveGenerator>();
            wg.pathCount = pathCount;
            if (wg.overrideRunSettings)
                return;

            wg.baseRate = 1.1f + 0.3f * level - 0.1f * pathCount;
            wg.baseCapacity = (7 + 1 * level) * (20 + totalPathLength / (float)pathCount) * 0.035f;
            wg.linearScaling = 1;
            wg.quadraticScaling = 0.01f;
            wg.cubicScaling = 0.007f;
            wg.random = new(rand.NewSeed());
        }

        static void SetupBattleController(int level)
        {
            PlayerState bc = GameObject.FindGameObjectWithTag(TagNames.BATTLE_CONTROLLER).GetComponent<PlayerState>();

            bc.fuelGoal = 90 + level * 10;
        }

        static void SetupTutorialSettings(ulong randomSeed)
        {
            WorldSettings ws = GameObject.FindGameObjectWithTag(TagNames.WORLD_SETTINGS).GetComponent<WorldSettings>();
            ws.seed = randomSeed;
            ws.pathLengths = new[] { 28 };
            ws.maxHubDistFromCenter = 6;

            WaveGenerator wg = GameObject.FindGameObjectWithTag(TagNames.WAVE_GENERATOR).GetComponent<WaveGenerator>();
            wg.pathCount = 1;
            wg.tutorial = true;

            PlayerState bc = GameObject.FindGameObjectWithTag(TagNames.BATTLE_CONTROLLER).GetComponent<PlayerState>();

            bc.fuelGoal = 110;
        }
    }
}