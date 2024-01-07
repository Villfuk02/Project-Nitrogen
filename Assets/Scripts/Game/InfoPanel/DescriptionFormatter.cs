using Game.Damage;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Utils;

namespace Game.InfoPanel
{
    public class DescriptionFormatter
    {
        public delegate string HandleTag(Blueprint.Blueprint blueprint);

        static readonly Dictionary<string, HandleTag> TagHandlers = new()
        {
            // FORMATTING
            {"BRK", _=>"<line-height=150%>\n<line-height=100%>"},
            // ICONS
            {"MAT", _ => TextUtils.Icon.Materials.Sprite()},
            {"ENE", _ => TextUtils.Icon.Energy.Sprite()},
            {"FUE", _ => TextUtils.Icon.Fuel.Sprite()},
            {"HUL", _ => TextUtils.Icon.Hull.Sprite()},

            // BLUEPRINT STATS
            {"NAM", b => b.name},
            {"RNG", b => FormatFloatStat(b.range, TextUtils.Icon.Range)},
            {"DMG", b => FormatIntStat(b.damage, TextUtils.Icon.Damage)},
            {"DMT", b => FormatDamageType(b.damageType)},
            {"SHI", b => FormatTicksStat(b.shotInterval, TextUtils.Icon.ShotInterval)},
            {"GEN", FormatGeneration},
            {"M1-", b => FormatIntStat(b.magic1, null)},
            {"M1+", b => FormatIntStat(b.magic1, null)},
        };

        readonly Blueprint.Blueprint blueprint_;
        public DescriptionFormatter(Blueprint.Blueprint blueprint)
        {
            blueprint_ = blueprint;
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

        string FormatTag(string tag) => TagHandlers.TryGetValue(tag, out var handle) ? handle(blueprint_) : $"<UNKNOWN-TAG-{tag}>";
        static string FormatIntStat(int value, TextUtils.Icon? icon) => $"{icon?.Sprite()}{value}";
        static string FormatFloatStat(float value, TextUtils.Icon? icon) => $"{icon?.Sprite()}{value:0.##}";
        static string FormatDamageType(Damage.Damage.Type type) => type.ToHumanReadable(true);
        static string FormatTicksStat(int ticks, TextUtils.Icon? icon) => $"{icon?.Sprite()}{(ticks * 0.05f).ToString("0.##", CultureInfo.InvariantCulture)}s";
        static string FormatGeneration(Blueprint.Blueprint b)
        {
            StringBuilder sb = new();
            sb.Append(TextUtils.Icon.Generation.Sprite());
            if (b.HasMaterialGeneration)
                sb.Append(FormatIntStat(b.materialGeneration, TextUtils.Icon.Materials));
            if (b.HasEnergyGeneration)
                sb.Append(FormatIntStat(b.energyGeneration, TextUtils.Icon.Energy));
            if (b.HasFuelGeneration)
                sb.Append(FormatIntStat(b.fuelGeneration, TextUtils.Icon.Fuel));
            return sb.ToString();
        }
    }
}
