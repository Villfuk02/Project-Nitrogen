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
            int paths = 4;//rand.Int(1, Mathf.Clamp(1 + level / 2, 2, 6)); TODO
            int minPathLength = 8; //Mathf.Max(27 - 3 * level, 9); TODO
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
