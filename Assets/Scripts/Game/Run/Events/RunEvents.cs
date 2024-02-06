using UnityEngine;
using Utils;

namespace Game.Run.Events
{
    public class RunEvents : MonoBehaviour
    {
        public GameCommand<int> damageHull = new();
        public GameCommand<int> repairHull = new();
        public GameCommand defeat = new();
        public GameCommand nextLevel = new();
        public GameCommand restart = new();
    }
}
