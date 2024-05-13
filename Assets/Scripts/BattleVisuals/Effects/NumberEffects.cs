using BattleSimulation.Attackers;
using BattleSimulation.Buildings;
using BattleSimulation.Control;
using Game.Damage;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

namespace BattleVisuals.Effects
{
    public class NumberEffects : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameObject numberEffectPrefab;
        [Header("Settings - damage and healing")]
        [SerializeField] float damageTimeToLive;
        [SerializeField] Color damageColor;
        [SerializeField] Color healColor;
        [SerializeField] float damageMinFontSize;
        [SerializeField] float damageMaxFontSize;
        [SerializeField] float fullSizeDamage;
        [Header("Settings - production")]
        [SerializeField] float productionTimeToLive;
        [SerializeField] float productionDelay;
        [SerializeField] Color materialsColor;
        [SerializeField] Color energyColor;
        [SerializeField] Color fuelColor;
        [SerializeField] float productionFontSize;
        [Header("Runtime variables")]
        [SerializeField] float fullSizeDamageLog;
        readonly Dictionary<object, int> createdThisFrame_ = new();

        void Start()
        {
            fullSizeDamageLog = Mathf.Log(fullSizeDamage);

            Attacker.DAMAGE.RegisterReaction(SpawnDamage, 100);
            Attacker.HEAL.RegisterReaction(SpawnHeal, 100);
            BattleController.addMaterial.RegisterReaction(SpawnMaterial, 100);
            BattleController.addEnergy.RegisterReaction(SpawnEnergy, 100);
            BattleController.addFuel.RegisterReaction(SpawnFuel, 100);
        }

        void OnDestroy()
        {
            Attacker.DAMAGE.UnregisterReaction(SpawnDamage);
            Attacker.HEAL.UnregisterReaction(SpawnHeal);
            BattleController.addMaterial.UnregisterReaction(SpawnMaterial);
            BattleController.addEnergy.UnregisterReaction(SpawnEnergy);
            BattleController.addFuel.UnregisterReaction(SpawnFuel);
        }

        void Update()
        {
            createdThisFrame_.Clear();
        }

        void SpawnDamage((Attacker target, Damage damage) param)
        {
            float size = Mathf.Lerp(damageMinFontSize, damageMaxFontSize, Mathf.Log(param.damage.amount) / fullSizeDamageLog);
            Vector2 r = Random.insideUnitCircle;
            Vector3 vel = new(r.x, 2, r.y);
            Spawn(param.damage.amount.ToString(CultureInfo.InvariantCulture), size, damageColor, damageTimeToLive, param.target.target.position + Vector3.up * 0.3f, vel, Vector3.down * 10);
        }

        void SpawnHeal((Attacker target, float amount) param)
        {
            float size = Mathf.Lerp(damageMinFontSize, damageMaxFontSize, Mathf.Log(param.amount) / fullSizeDamageLog);
            Spawn(param.amount.ToString(CultureInfo.InvariantCulture), size, healColor, damageTimeToLive, param.target.target.position + Vector3.up * 0.3f, Vector3.up * 1.5f, Vector3.down * 0.5f);
        }

        void SpawnMaterial((object source, float amount) param) => SpawnProduction(param.source, $"+{param.amount}{TextUtils.Icon.Materials.Sprite()}", materialsColor);
        void SpawnEnergy((object source, float amount) param) => SpawnProduction(param.source, $"+{param.amount}{TextUtils.Icon.Energy.Sprite()}", energyColor);
        void SpawnFuel((object source, float amount) param) => SpawnProduction(param.source, $"+{param.amount}{TextUtils.Icon.Fuel.Sprite()}", fuelColor);

        void SpawnProduction(object source, string text, Color color)
        {
            if (source == null || source is not Building b)
                return;
            Vector3 pos = b.transform.position + Vector3.up;
            if (createdThisFrame_.ContainsKey(b))
            {
                StartCoroutine(SpawnLater(text, productionFontSize, color, productionTimeToLive, pos, Vector3.up * 1.5f, Vector3.down * 0.5f, createdThisFrame_[b] * productionDelay));
                createdThisFrame_[b]++;
            }
            else
            {
                Spawn(text, productionFontSize, color, productionTimeToLive, pos, Vector3.up * 1.5f, Vector3.down * 0.5f);
                createdThisFrame_[b] = 1;
            }
        }

        void Spawn(string s, float textSize, Color color, float timeToLive, Vector3 position, Vector3 velocity, Vector3 acceleration)
        {
            NumberEffect ne = Instantiate(numberEffectPrefab, transform).GetComponent<NumberEffect>();
            ne.transform.localPosition = position;
            ne.Init(s, textSize, color, timeToLive, velocity, acceleration);
        }

        IEnumerator SpawnLater(string s, float textSize, Color color, float timeToLive, Vector3 position, Vector3 velocity, Vector3 acceleration, float time)
        {
            yield return new WaitForSeconds(time);
            Spawn(s, textSize, color, timeToLive, position, velocity, acceleration);
        }
    }
}
