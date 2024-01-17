using BattleSimulation.Attackers;
using UnityEngine;
using UnityEngine.UI;

namespace BattleVisuals.Attackers
{
    public class HealthDisplay : MonoBehaviour
    {
        [SerializeField] Attacker attacker;
        [SerializeField] GameObject divider;
        [SerializeField] Transform dividerHolder;
        [SerializeField] Image shadow;
        [SerializeField] Image health;
        [SerializeField] float shadowSpeed;
        [SerializeField] Color[] colors;
        [SerializeField] int max;
        [SerializeField] int division;
        void Start()
        {
            max = attacker.stats.maxHealth;
            division = 1;
            int divisionOrder = 0;
            while (division * 10 < max)
            {
                divisionOrder++;
                division *= 10;
            }
            float divisions = max / (float)division;
            for (int i = 1; i < divisions; i++)
                Instantiate(divider, dividerHolder).transform.localPosition = Vector3.right * i / divisions;
            health.color = colors[divisionOrder];
        }

        void Update()
        {
            health.fillAmount = attacker.health / (float)max;
            shadow.fillAmount = Mathf.Lerp(shadow.fillAmount, health.fillAmount, shadowSpeed * Time.deltaTime);
        }
    }
}
