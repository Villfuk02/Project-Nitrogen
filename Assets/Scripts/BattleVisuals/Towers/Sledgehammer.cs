using UnityEngine;
using Utils;

namespace BattleVisuals.Towers
{
    public class Sledgehammer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] BattleSimulation.Towers.Sledgehammer sledgehammer;
        [SerializeField] Transform head;
        [Header("Settings")]
        [SerializeField] float maxHeight;
        [SerializeField] float minHeight;
        [SerializeField] float dropTime;
        [Header("Runtime variables")]
        [SerializeField] float time;

        void Start()
        {
            time = 100;
        }

        void Update()
        {
            time += Time.deltaTime;
            UpdatePosition();
        }

        public void Drop()
        {
            time = 0;
            UpdatePosition();
        }

        void UpdatePosition()
        {
            float t = time < dropTime ? time * time / (dropTime * dropTime) : 1 - (time - dropTime) / (sledgehammer.currentBlueprint.interval * TimeUtils.SECS_PER_TICK - dropTime * 2);
            head.localPosition = Vector3.up * Mathf.Lerp(maxHeight, minHeight, t);
        }
    }
}