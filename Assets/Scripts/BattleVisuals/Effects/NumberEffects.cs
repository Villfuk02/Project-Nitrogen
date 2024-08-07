using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using BattleSimulation.Attackers;
using BattleSimulation.Control;
using Game.Shared;
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
            PlayerState.ADD_MATERIAL.RegisterReaction(SpawnMaterial, 100);
            PlayerState.ADD_ENERGY.RegisterReaction(SpawnEnergy, 100);
            PlayerState.ADD_FUEL.RegisterReaction(SpawnFuel, 100);
        }

        void OnDestroy()
        {
            Attacker.DAMAGE.UnregisterReaction(SpawnDamage);
            Attacker.HEAL.UnregisterReaction(SpawnHeal);
            PlayerState.ADD_MATERIAL.UnregisterReaction(SpawnMaterial);
            PlayerState.ADD_ENERGY.UnregisterReaction(SpawnEnergy);
            PlayerState.ADD_FUEL.UnregisterReaction(SpawnFuel);
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
            Spawn(param.damage.amount.ToString(CultureInfo.InvariantCulture), size, damageColor, damageTimeToLive, param.target.target.position + Vector3.up * 0.3f, vel, Vector3.down * 10, null);
        }

        void SpawnHeal((Attacker target, float amount) param)
        {
            float size = Mathf.Lerp(damageMinFontSize, damageMaxFontSize, Mathf.Log(param.amount) / fullSizeDamageLog);
            Spawn(param.amount.ToString(CultureInfo.InvariantCulture), size, healColor, damageTimeToLive, param.target.target.position + Vector3.up * 0.3f, Vector3.up * 1.5f, Vector3.down * 0.5f, null);
        }

        void SpawnMaterial((object source, float amount) param) => SpawnProduction(param.source, $"+{param.amount}{TextUtils.Icon.Materials.Sprite()}", materialsColor, SoundController.Sound.Materials);

        void SpawnEnergy((object source, float amount) param) => SpawnProduction(param.source, $"+{param.amount}{TextUtils.Icon.Energy.Sprite()}", energyColor, SoundController.Sound.Energy);

        void SpawnFuel((object source, float amount) param) => SpawnProduction(param.source, $"+{param.amount}{TextUtils.Icon.Fuel.Sprite()}", fuelColor, SoundController.Sound.Fuel);

        void SpawnProduction(object source, string text, Color color, SoundController.Sound sound)
        {
            if (source is not Component component || component == null)
                return;
            Vector3 pos = component.transform.position + Vector3.up;
            if (createdThisFrame_.ContainsKey(component))
            {
                StartCoroutine(SpawnLater(text, productionFontSize, color, productionTimeToLive, pos, Vector3.up * 1.5f, Vector3.down * 0.5f, sound, createdThisFrame_[component] * productionDelay));
                createdThisFrame_[component]++;
            }
            else
            {
                Spawn(text, productionFontSize, color, productionTimeToLive, pos, Vector3.up * 1.5f, Vector3.down * 0.5f, sound);
                createdThisFrame_[component] = 1;
            }
        }

        void Spawn(string s, float textSize, Color color, float timeToLive, Vector3 position, Vector3 velocity, Vector3 acceleration, SoundController.Sound? sound)
        {
            NumberEffect ne = Instantiate(numberEffectPrefab, transform).GetComponent<NumberEffect>();
            ne.transform.localPosition = position;
            ne.Init(s, textSize, color, timeToLive, velocity, acceleration);
            if (sound is not null)
                SoundController.PlaySound(sound.Value, 0.25f, 1, 0.2f, position);
        }

        IEnumerator SpawnLater(string s, float textSize, Color color, float timeToLive, Vector3 position, Vector3 velocity, Vector3 acceleration, SoundController.Sound? sound, float time)
        {
            yield return new WaitForSeconds(time);
            Spawn(s, textSize, color, timeToLive, position, velocity, acceleration, sound);
        }
    }
}