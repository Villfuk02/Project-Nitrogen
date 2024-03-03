using UnityEngine;
using Utils;

namespace Game.Run.Events
{
    public class RunEvents : MonoBehaviour
    {
        public ModifiableCommand<int> damageHull = new();
        public ModifiableCommand<int> repairHull = new();
        public ModifiableCommand defeat = new();
        public ModifiableCommand nextLevel = new();
        public ModifiableCommand restart = new();
    }
}
