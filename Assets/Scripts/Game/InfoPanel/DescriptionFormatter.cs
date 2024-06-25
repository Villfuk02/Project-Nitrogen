using System.Collections.Generic;
using BattleSimulation.Attackers;
using BattleSimulation.World;
using Game.AttackerStats;
using Game.Blueprint;
using Game.Shared;
using AttackerDescriptionFormatter = Game.InfoPanel.DescriptionFormatter<(Game.AttackerStats.AttackerStats stats, BattleSimulation.Attackers.Attacker attacker)>;
using BlueprintDescriptionFormatter = Game.InfoPanel.DescriptionFormatter<Game.Blueprint.IBlueprintProvider>;
using TileDescriptionFormatter = Game.InfoPanel.DescriptionFormatter<BattleSimulation.World.Tile>;
using BP = Game.Blueprint.Blueprint;
using static Utils.TextUtils;

namespace Game.InfoPanel
{
    public static class DescriptionFormat
    {
        public static readonly Dictionary<string, string> SHARED_TAGS = new()
        {
            // FORMATTING
            { "BRK", "<line-height=150%>\n<line-height=100%>" },

            // ICONS
            { "#FUE", Icon.Fuel.Sprite() },
            { "#MAT", Icon.Materials.Sprite() },
            { "#ENE", Icon.Energy.Sprite() },
            { "#HUL", Icon.Hull.Sprite() },
            { "#RNG", Icon.Range.Sprite() },
            { "#DMG", Icon.Damage.Sprite() },
            { "#INT", Icon.Interval.Sprite() },
            { "#DUR", Icon.Duration.Sprite() },
            { "#HP", Icon.Health.Sprite() },
            { "#RAD", Icon.Radius.Sprite() },
            { "#+", Icon.Production.Sprite() },

            // DAMAGE TYPE ICONS
            { "#DMT-HPL", Icon.DmgHpLoss.Sprite() },
            { "#DMT-PHY", Icon.DmgPhysical.Sprite() },
            { "#DMT-ENE", Icon.DmgEnergy.Sprite() },
            { "#DMT-EXP", Icon.DmgExplosive.Sprite() }
        };

        static readonly Dictionary<string, (BlueprintDescriptionFormatter.HandleTag, string)> BlueprintTags = new()
        {
            { "NAM", (s => FormatStringStat(BP.Name.Query(s), s.GetBaseBlueprint().name), "Name") },
            { "RNG", (s => FormatFloatStat(Icon.Range, BP.Range.Query(s), s.GetBaseBlueprint().range, Improvement.More), "Range") },
            { "DMG", (s => Damage.FormatDamage(BP.Damage.Query(s), s.GetBaseBlueprint().damage, s.GetBaseBlueprint().damageType, Improvement.More), "Damage") },
            { "DTL", (s => Damage.FormatDamageType(BP.DamageType.Query(s), s.GetBaseBlueprint().damageType), "Damage type") },
            { "DTI", (s => BP.DamageType.Query(s).ToHumanReadable(true), "Damage type") },
            { "INT", (s => FormatTicksStat(Icon.Interval, BP.Interval.Query(s), s.GetBaseBlueprint().interval, Improvement.Less), "Interval") },
            { "DPS", (s => FormatFloatStat(Icon.Dps, BP.Dps(s), s.GetBaseBlueprint().BaseDps, Improvement.More), "Damage/s") },
            { "RAD", (s => FormatFloatStat(Icon.Radius, BP.Radius.Query(s), s.GetBaseBlueprint().radius, Improvement.More), "Radius") },
            { "DEL", (s => FormatTicksStat(Icon.Delay, BP.Delay.Query(s), s.GetBaseBlueprint().delay, Improvement.Less), "Delay") },
            { "DUR", (s => FormatDuration(BP.DurationTicks.Query(s), s.GetBaseBlueprint().durationTicks, BP.DurationWaves.Query(s), s.GetBaseBlueprint().durationWaves), "Duration") },
            { "PRO", (s => FormatProduction(BP.FuelProduction.Query(s), BP.MaterialProduction.Query(s), BP.EnergyProduction.Query(s), s.GetBaseBlueprint().fuelProduction, s.GetBaseBlueprint().materialProduction, s.GetBaseBlueprint().energyProduction), "Production") },
            { "SCD", (s => $"{BP.StartingCooldown.Query(s)} waves", "Starting cooldown") },
            { "CD", (s => $"{FormatIntStat(null, BP.Cooldown.Query(s), s.GetBaseBlueprint().cooldown, Improvement.Less)} waves", "Cooldown") },
            { "+CD", (s => FormatCooldownSuffix(BP.Cooldown.Query(s), s.GetBaseBlueprint().cooldown), "Cooldown SUFFIX") },
            { "FUE", (s => FormatIntStat(Icon.Fuel, BP.FuelProduction.Query(s), s.GetBaseBlueprint().fuelProduction, Improvement.More), "Fuel production") },
            { "MAT", (s => FormatIntStat(Icon.Materials, BP.MaterialProduction.Query(s), s.GetBaseBlueprint().materialProduction, Improvement.More), "Material production") },
            { "ENE", (s => FormatIntStat(Icon.Energy, BP.EnergyProduction.Query(s), s.GetBaseBlueprint().energyProduction, Improvement.More), "Energy production") }
        };

        static readonly Dictionary<string, (AttackerDescriptionFormatter.HandleTag, string)> AttackerTags = new()
        {
            { "SIZ", (s => s.stats.size.ToHumanReadable(true, true), "Size") },
            { "SPD", (s => FormatFloatStat(Icon.Speed, s.attacker == null ? s.stats.speed : s.attacker.Speed, s.stats.speed, Improvement.More), "Speed") },
            { "HP", (s => FormatIntStat(Icon.Health, s.attacker.health, s.stats.maxHealth, Improvement.Undeclared), "Health") },
            { "MHP", (s => $"{Icon.Health.Sprite()}{s.stats.maxHealth}", "Health") },
            { "HP/M", (s => $"{FormatIntStat(Icon.Health, s.attacker.health, s.stats.maxHealth, Improvement.Undeclared)}/{s.stats.maxHealth}", "Health") }
        };

        static readonly Dictionary<string, (TileDescriptionFormatter.HandleTag, string)> TileTags = new();

        public static BlueprintDescriptionFormatter BlueprintFormatter(IBlueprintProvider provider) => new(provider, BlueprintTags);
        public static AttackerDescriptionFormatter AttackerFormatter(AttackerStats.AttackerStats stats, Attacker attacker) => new((stats, attacker), AttackerTags);
        public static TileDescriptionFormatter TileFormatter(Tile tile) => new(tile, TileTags);
    }

    public class DescriptionFormatter<T>
    {
        public delegate string HandleTag(T state);

        readonly IReadOnlyDictionary<string, (HandleTag handle, string statName)> tagHandlers_;

        readonly T state_;

        public DescriptionFormatter(T state, IReadOnlyDictionary<string, (HandleTag handle, string statName)> tagHandlers)
        {
            state_ = state;
            tagHandlers_ = tagHandlers;
        }

        public string Format(string description)
        {
            // split string on tags
            var split = description.Split('[', ']');
            for (int i = 0; i < split.Length; i++)
            {
                if (i % 2 == 0)
                    continue;
                // replace tags
                split[i] = FormatTag(split[i]);
            }

            // join
            return string.Join("", split);
        }

        string FormatTag(string tag)
        {
            if (tag.Length == 0)
                return string.Empty;

            bool includeStatName = tag[0] == '$';
            if (includeStatName)
                tag = tag[1..];

            if (DescriptionFormat.SHARED_TAGS.TryGetValue(tag, out var replacement))
                return replacement;
            if (tagHandlers_.TryGetValue(tag, out var handler))
                return (includeStatName ? $"{handler.statName} " : "") + handler.handle(state_);
            return $"[{(includeStatName ? '$' : "")}{tag}]".Colored(CHANGED_COLOR);
        }
    }
}