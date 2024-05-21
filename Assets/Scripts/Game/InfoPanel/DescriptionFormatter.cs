using System.Collections.Generic;
using BattleSimulation.Attackers;
using BattleSimulation.World;
using Game.AttackerStats;
using Game.Damage;
using AttackerDescriptionFormatter = Game.InfoPanel.DescriptionFormatter<(Game.AttackerStats.AttackerStats stats, Game.AttackerStats.AttackerStats original, BattleSimulation.Attackers.Attacker attacker)>;
using BlueprintDescriptionFormatter = Game.InfoPanel.DescriptionFormatter<(Game.InfoPanel.DescriptionFormat.BlueprintProvider getBlueprint, Game.Blueprint.Blueprint original)>;
using TileDescriptionFormatter = Game.InfoPanel.DescriptionFormatter<BattleSimulation.World.Tile>;
using static Utils.TextUtils;

namespace Game.InfoPanel
{
    public static class DescriptionFormat
    {
        public delegate Blueprint.Blueprint BlueprintProvider();

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
            { "NAM", (s => FormatStringStat(s.getBlueprint().name, s.original.name), "Name") },
            { "RNG", (s => FormatFloatStat(Icon.Range, s.getBlueprint().range, s.original.range, s.original.HasRange, Improvement.More), "Range") },
            { "DMG", (s => Damage.Damage.FormatDamage(s.getBlueprint().damage, s.original.damage, s.original.HasDamage, s.getBlueprint().damageType, Improvement.More), "Damage") },
            { "DTL", (s => Damage.Damage.FormatDamageType(s.getBlueprint().damageType, s.original.damageType), "Damage type") },
            { "DTI", (s => s.getBlueprint().damageType.ToHumanReadable(true), "Damage type") },
            { "INT", (s => FormatTicksStat(Icon.Interval, s.getBlueprint().interval, s.original.interval, s.original.HasInterval, Improvement.Less), "Interval") },
            { "DPS", (s => FormatFloatStat(Icon.Dps, s.getBlueprint().BaseDps, s.original.BaseDps, s.original.HasDamage && s.original.HasInterval, Improvement.More), "Damage/s") },
            { "RAD", (s => FormatFloatStat(Icon.Radius, s.getBlueprint().radius, s.original.radius, s.original.HasRadius, Improvement.More), "Radius") },
            { "DEL", (s => FormatTicksStat(Icon.Delay, s.getBlueprint().delay, s.original.delay, s.original.HasDelay, Improvement.Less), "Delay") },
            { "DUR", (s => FormatDuration(s.getBlueprint().durationTicks, s.original.durationTicks, s.getBlueprint().durationWaves, s.original.durationWaves), "Duration") },
            { "PRO", (s => FormatProduction(s.getBlueprint().fuelProduction, s.getBlueprint().materialProduction, s.getBlueprint().energyProduction, s.original.fuelProduction, s.original.materialProduction, s.original.energyProduction), "Production") },
            { "SCD", (s => $"{s.getBlueprint().startingCooldown} waves", "Starting cooldown") },
            { "CD", (s => $"{FormatIntStat(null, s.getBlueprint().cooldown, s.original.cooldown, true, Improvement.Less)} waves", "Cooldown") },
            { "+CD", (s => FormatCooldownSuffix(s.getBlueprint().cooldown, s.original.cooldown), "Cooldown SUFFIX") },
            { "FUE", (s => FormatIntStat(Icon.Fuel, s.getBlueprint().fuelProduction, s.original.fuelProduction, s.original.HasFuelProduction, Improvement.More), "Fuel production") },
            { "MAT", (s => FormatIntStat(Icon.Materials, s.getBlueprint().materialProduction, s.original.materialProduction, s.original.HasMaterialProduction, Improvement.More), "Material production") },
            { "ENE", (s => FormatIntStat(Icon.Energy, s.getBlueprint().energyProduction, s.original.energyProduction, s.original.HasEnergyProduction, Improvement.More), "Energy production") }
        };

        static readonly Dictionary<string, (AttackerDescriptionFormatter.HandleTag, string)> AttackerTags = new()
        {
            { "SIZ", (s => s.stats.size.ToHumanReadable(true, true), "Size") },
            { "SPD", (s => FormatFloatStat(Icon.Speed, s.stats.speed, s.original.speed, true, Improvement.More), "Speed") },
            { "HP", (s => FormatIntStat(Icon.Health, s.attacker.health, s.stats.maxHealth, true, Improvement.Undeclared), "Health") },
            { "MHP", (s => FormatIntStat(Icon.Health, s.stats.maxHealth, s.original.maxHealth, true, Improvement.More), "Health") },
            { "HP/M", (s => $"{FormatIntStat(Icon.Health, s.attacker.health, s.stats.maxHealth, true, Improvement.Undeclared)}/{FormatIntStat(null, s.stats.maxHealth, s.original.maxHealth, true, Improvement.More)}", "Health") }
        };

        static readonly Dictionary<string, (TileDescriptionFormatter.HandleTag, string)> TileTags = new();

        public static BlueprintDescriptionFormatter Blueprint(BlueprintProvider getBlueprint, Blueprint.Blueprint original) => new((getBlueprint, original), BlueprintTags);
        public static AttackerDescriptionFormatter Attacker(AttackerStats.AttackerStats stats, AttackerStats.AttackerStats original, Attacker attacker) => new((stats, original, attacker), AttackerTags);
        public static TileDescriptionFormatter Tile(Tile tile) => new(tile, TileTags);
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