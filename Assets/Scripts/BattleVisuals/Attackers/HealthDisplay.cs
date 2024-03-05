using BattleSimulation.Attackers;
using UnityEngine;
using UnityEngine.UI;

namespace BattleVisuals.Attackers
{
    public class HealthDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Attacker attacker;
        [SerializeField] GameObject divider;
        [SerializeField] Transform dividerHolder;
        [SerializeField] Image shadow;
        [SerializeField] Image health;
        [Header("Settings")]
        [SerializeField] float shadowSpeed;
        [SerializeField] Color[] colors;
        [Header("Runtime variables")]
        [SerializeField] int max;
        void Start()
        {
            max = attacker.stats.maxHealth;
            CalculateDivisions(out int divisionOrder, out float divisionCount);
            for (int i = 1; i < divisionCount; i++)
                Instantiate(divider, dividerHolder).transform.localPosition = Vector3.right * i / divisionCount;
            health.color = colors[divisionOrder];
        }

        void CalculateDivisions(out int divisionOrder, out float divisionCount)
        {
            int size = 1;
            divisionOrder = 0;
            while (size * 10 < max)
            {
                divisionOrder++;
                size *= 10;
            }

            divisionCount = max / (float)size;
        }

        void Update()
        {
            health.fillAmount = attacker.health / (float)max;
            shadow.fillAmount = Mathf.Lerp(shadow.fillAmount, health.fillAmount, shadowSpeed * Time.deltaTime);
        }
    }
}
