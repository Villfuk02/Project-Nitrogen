using TMPro;
using UnityEngine;

namespace BattleVisuals.Effects
{
    public class NumberEffect : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] TextMeshProUGUI text;
        [Header("Settings - auto-assigned")]
        [SerializeField] float timeToLive;
        [SerializeField] Vector3 velocity;
        [SerializeField] Vector3 acceleration;
        [SerializeField] Color color;
        [Header("Runtime variables")]
        [SerializeField] float time;

        public void Init(string s, float textSize, Color color, float timeToLive, Vector3 velocity, Vector3 acceleration)
        {
            text.text = s;
            text.fontSize = textSize;
            text.color = color;
            this.color = color;
            this.timeToLive = timeToLive;
            this.velocity = velocity;
            this.acceleration = acceleration;
        }
        void Update()
        {
            time += Time.deltaTime;
            if (time > timeToLive)
            {
                Destroy(gameObject);
                return;
            }
            float ratio = time / timeToLive;
            color.a = 2 * (1 - ratio);
            text.color = color;

            transform.localPosition += velocity * Time.deltaTime;
            velocity += acceleration * Time.deltaTime;
        }
    }
}
