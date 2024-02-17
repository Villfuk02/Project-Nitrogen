using BattleSimulation.Attackers;
using BattleSimulation.World;
using Game.Damage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Utils;
using AttackerDescriptionFormatter = Game.InfoPanel.DescriptionFormatter<(Game.AttackerStats.AttackerStats stats, Game.AttackerStats.AttackerStats original, BattleSimulation.Attackers.Attacker attacker)>;
using BlueprintDescriptionFormatter = Game.InfoPanel.DescriptionFormatter<(Game.Blueprint.Blueprint blueprint, Game.Blueprint.Blueprint original)>;
using TileDescriptionFormatter = Game.InfoPanel.DescriptionFormatter<BattleSimulation.World.Tile>;

namespace Game.InfoPanel
{
    public static class DescriptionFormat
    {
        public static readonly string IMPROVED_COLOR = "#" + ColorUtility.ToHtmlStringRGBA(Color.green);
        public static readonly string WORSENED_COLOR = "#" + ColorUtility.ToHtmlStringRGBA(0.5f * Color.red + 0.5f * Color.white);
        public static readonly string CHANGED_COLOR = "#" + ColorUtility.ToHtmlStringRGBA(0.7f * Color.yellow + 0.3f * Color.white);
        public static readonly string NEW_COLOR = "#" + ColorUtility.ToHtmlStringRGBA(0.7f * Color.yellow + 0.3f * Color.red);
        public enum Improvement { More, Less, Undeclared }

        public static readonly Dictionary<string, string> SHARED_TAGS = new()
        {
            // FORMATTING
            { "BRK", "<line-height=150%>\n<line-height=100%>" },

            // ICONS
            { "FUE", TextUtils.Icon.Fuel.Sprite() },
            { "MAT", TextUtils.Icon.Materials.Sprite() },
            { "ENE", TextUtils.Icon.Energy.Sprite() },
            { "HUL", TextUtils.Icon.Hull.Sprite() },
        };

        static readonly Dictionary<string, BlueprintDescriptionFormatter.HandleTag> BlueprintTags = new()
        {
            { "NAM", s => FormatStringStat(s.blueprint.name, s.original.name) },
            { "RNG", s => FormatFloatStat(TextUtils.Icon.Range, s.blueprint.range, s.original.range, s.original.HasRange, Improvement.More) },
            { "DMG", s => FormatIntStat(TextUtils.Icon.Damage, s.blueprint.damage, s.original.damage, s.original.HasDamage, Improvement.More) },
            { "DMT", s => FormatDamageType(s.blueprint.damageType, s.original.damageType) },
            { "INT", s => FormatTicksStat(TextUtils.Icon.Interval, s.blueprint.interval, s.original.interval, s.original.HasInterval, Improvement.Less) },
            { "DPS", s => FormatFloatStat(TextUtils.Icon.Dps, s.blueprint.BaseDps, s.original.BaseDps, s.original.HasDamage && s.original.HasInterval, Improvement.More)},
            { "RAD", s => FormatFloatStat(TextUtils.Icon.Radius, s.blueprint.radius, s.original.radius, s.original.HasRadius, Improvement.More)},
            { "DEL", s => FormatTicksStat(TextUtils.Icon.Time, s.blueprint.delay, s.original.delay, s.original.HasDelay, Improvement.Less)},
            { "DUT", s => FormatTicksStat(TextUtils.Icon.Time, s.blueprint.durationTicks, s.original.durationTicks, s.original.HasDurationTicks, Improvement.More)},
            { "DUW", s => FormatIntStat(TextUtils.Icon.Time, s.blueprint.durationWaves, s.original.durationWaves, s.original.HasDurationWaves, Improvement.More)},
            { "PRO", s => FormatProduction(s.blueprint, s.original) },
            { "M1-", s => FormatIntStat(null, s.blueprint.magic1, s.original.magic1, true, Improvement.Less) },
            { "M1+", s => FormatIntStat(null, s.blueprint.magic1, s.original.magic1, true, Improvement.More) },
        };

        static readonly Dictionary<string, AttackerDescriptionFormatter.HandleTag> AttackerTags = new()
        {
            {"SIZ", s => AttackerStats.AttackerStats.HumanReadableSize(s.stats.size, true)},
            {"SPD", s => FormatFloatStat(TextUtils.Icon.Speed, s.stats.speed, s.original.speed, true, Improvement.More)},
            {"HP", s => FormatIntStat(TextUtils.Icon.Health, s.attacker.health, s.stats.maxHealth, true, Improvement.Undeclared)},
            {"MHP", s => FormatIntStat(TextUtils.Icon.Health, s.stats.maxHealth, s.original.maxHealth, true, Improvement.More)},
            {"HP/M", s => $"{FormatIntStat(TextUtils.Icon.Health, s.attacker.health, s.stats.maxHealth, true, Improvement.Undeclared)}/{FormatIntStat(null,s.stats.maxHealth, s.original.maxHealth, true, Improvement.More)}"}
        };

        static readonly Dictionary<string, TileDescriptionFormatter.HandleTag> TileTags = new();

        public static BlueprintDescriptionFormatter Blueprint(Blueprint.Blueprint blueprint, Blueprint.Blueprint original) => new((blueprint, original), BlueprintTags);
        public static AttackerDescriptionFormatter Attacker(AttackerStats.AttackerStats stats, AttackerStats.AttackerStats original, Attacker attacker) => new((stats, original, attacker), AttackerTags);
        public static TileDescriptionFormatter Tile(Tile tile) => new(tile, TileTags);

        static string FormatStringStat(string text, string? original)
        {
            if (text == original)
                return text;
            return text.Colored(string.IsNullOrEmpty(original) ? NEW_COLOR : CHANGED_COLOR);
        }
        static string FormatIntStat(TextUtils.Icon? icon, int value, int original, bool originalExists, Improvement improvement)
        {
            return ColorImprovement($"{icon?.Sprite()}{value}", value, original, originalExists, improvement);
        }
        static string FormatFloatStat(TextUtils.Icon? icon, float value, float original, bool originalExists, Improvement improvement)
        {
            return ColorImprovement($"{icon?.Sprite()}{value.ToString("0.##", CultureInfo.InvariantCulture)}", value, original, originalExists, improvement);
        }
        static string FormatDamageType(Damage.Damage.Type type, Damage.Damage.Type original) => $"{(type & original).ToHumanReadable(true)} {(type & ~original).ToHumanReadable(true).Colored(NEW_COLOR)}";
        static string FormatTicksStat(TextUtils.Icon? icon, int ticks, int original, bool originalExists, Improvement improvement)
        {
            return ColorImprovement($"{icon?.Sprite()}{(ticks * 0.05f).ToString("0.##", CultureInfo.InvariantCulture)}s", ticks, original, originalExists, improvement);
        }

        static string FormatProduction(Blueprint.Blueprint b, Blueprint.Blueprint o)
        {
            StringBuilder sb = new();
            sb.Append(TextUtils.Icon.Production.Sprite());
            if (b.HasFuelProduction)
                sb.Append(FormatIntStat(TextUtils.Icon.Fuel, b.fuelProduction, o.fuelProduction, o.HasFuelProduction, Improvement.More));
            if (b.HasMaterialProduction)
                sb.Append(FormatIntStat(TextUtils.Icon.Materials, b.materialProduction, o.materialProduction, o.HasMaterialProduction, Improvement.More));
            if (b.HasEnergyProduction)
                sb.Append(FormatIntStat(TextUtils.Icon.Energy, b.energyProduction, o.energyProduction, o.HasEnergyProduction, Improvement.More));
            return sb.ToString();
        }

        static string ColorImprovement<T>(string text, T stat, T original, bool existedBefore, Improvement improvement) where T : IComparable<T>
        {
            if (!existedBefore)
                return text.Colored(NEW_COLOR);
            int comparison = stat.CompareTo(original);
            if (comparison == 0)
                return text;
            if (improvement == Improvement.Undeclared)
                return text.Colored(CHANGED_COLOR);

            if (improvement == Improvement.Less)
                comparison *= -1;

            return text.Colored(comparison > 0 ? IMPROVED_COLOR : WORSENED_COLOR);
        }
    }
    public class DescriptionFormatter<T>
    {
        public delegate string HandleTag(T state);

        readonly IReadOnlyDictionary<string, HandleTag> tagHandlers_;

        readonly T state_;

        public DescriptionFormatter(T state, IReadOnlyDictionary<string, HandleTag> tagHandlers)
        {
            state_ = state;
            tagHandlers_ = tagHandlers;
        }

        public string Format(string description)
        {
            // split string on tags, replace tags, join
            var split = description.Split('[', ']');
            for (int i = 0; i < split.Length; i++)
            {
                if (i % 2 == 0)
                    continue;
                split[i] = FormatTag(split[i]);
            }

            return string.Join("", split);
        }

        string FormatTag(string tag)
        {
            if (DescriptionFormat.SHARED_TAGS.TryGetValue(tag, out var replacement))
                return replacement;
            if (tagHandlers_.TryGetValue(tag, out var handle))
                return handle(state_);
            return $"<UNKNOWN-TAG-{tag}>";
        }
    }
}
