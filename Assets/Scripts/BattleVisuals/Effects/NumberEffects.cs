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
        [SerializeField] GameObject numberEffectPrefab;

        [SerializeField] float damageTimeToLive;
        [SerializeField] float productionTimeToLive;
        [SerializeField] float productionDelay;

        [SerializeField] Color materialsColor;
        [SerializeField] Color energyColor;
        [SerializeField] Color fuelColor;
        [SerializeField] Color damageColor;

        [SerializeField] float productionFontSize;
        [SerializeField] float damageMinFontSize;
        [SerializeField] float damageMaxFontSize;
        [SerializeField] float fullSizeDamage;
        [SerializeField] float fullSizeDamageLog;

        readonly Dictionary<object, int> createdThisFrame_ = new();

        void Start()
        {
            fullSizeDamageLog = Mathf.Log(fullSizeDamage);

            Attacker.damage.Register(SpawnDamage, 100);
            BattleController.addMaterial.Register(SpawnMaterial, 100);
            BattleController.addEnergy.Register(SpawnEnergy, 100);
            BattleController.addFuel.Register(SpawnFuel, 100);
        }

        void OnDestroy()
        {
            Attacker.damage.Unregister(SpawnDamage);
            BattleController.addMaterial.Unregister(SpawnMaterial);
            BattleController.addEnergy.Unregister(SpawnEnergy);
            BattleController.addFuel.Unregister(SpawnFuel);
        }

        void Update()
        {
            createdThisFrame_.Clear();
        }

        bool SpawnDamage(ref (Attacker target, Damage damage) param)
        {
            float size = damageMinFontSize + Mathf.Log(param.damage.amount) / fullSizeDamageLog * (damageMaxFontSize - damageMinFontSize);
            Vector2 r = Random.insideUnitCircle;
            Vector3 vel = new(r.x, 2, r.y);
            Spawn(param.damage.amount.ToString(CultureInfo.InvariantCulture), size, damageColor, damageTimeToLive, param.target.target.position + Vector3.up * 0.3f, vel, Vector3.down * 10);
            return true;
        }

        bool SpawnMaterial(ref (object source, float amount) param) => SpawnProduction(param.source, $"+{param.amount}{TextUtils.Icon.Materials.Sprite()}", materialsColor);
        bool SpawnEnergy(ref (object source, float amount) param) => SpawnProduction(param.source, $"+{param.amount}{TextUtils.Icon.Energy.Sprite()}", energyColor);
        bool SpawnFuel(ref (object source, float amount) param) => SpawnProduction(param.source, $"+{param.amount}{TextUtils.Icon.Fuel.Sprite()}", fuelColor);

        bool SpawnProduction(object source, string text, Color color)
        {
            if (source == null || source is not Building b)
                return true;
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
            return true;
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
