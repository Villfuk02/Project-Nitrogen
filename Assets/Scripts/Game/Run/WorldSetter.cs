using System.Collections.Generic;
using UnityEngine;
using WorldGen.WorldSettings;
using Random = Utils.Random.Random;

namespace Game.Run
{
    public class WorldSetter : MonoBehaviour
    {
        public void SetupLevel(ulong randomSeed, int level)
        {
            WorldSettings ws = GameObject.FindGameObjectWithTag("WorldSettings").GetComponent<WorldSettings>();
            if (ws.overrideRun)
                return;
            Random rand = new(randomSeed);
            ws.seed = rand.NewSeed();
            int paths = rand.Int(1, Mathf.Clamp(1 + level / 2, 2, 6));
            int minPathLength = Mathf.Max(27 - 3 * level, 9);
            List<int> pathLengths = new();
            for (int i = 0; i < paths; i++)
            {
                pathLengths.Add(rand.Int(minPathLength, minPathLength + 5 + 2 * i));
            }
            pathLengths.Sort();
            ws.pathLengths = pathLengths.ToArray();
        }
    }
}
