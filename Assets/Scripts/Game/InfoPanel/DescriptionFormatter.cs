using Game.Damage;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

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

            // BLUEPRINT STATS
            {"NAM", b => b.name},
            {"RNG", b => FormatFloatStat(b.range)},
            {"DMG", b => FormatIntStat(b.damage)},
            {"DMT", b => FormatDamageType(b.damageType)},
            {"SHI", b => FormatTicksStat(b.shotInterval)},
            {"GEN", FormatGeneration},
            {"M1-", b => FormatIntStat(b.magic1)},
            {"M1+", b => FormatIntStat(b.magic1)},
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
        static string FormatIntStat(int value) => value.ToString();
        static string FormatFloatStat(float value) => value.ToString("0.##");
        static string FormatDamageType(Damage.Damage.Type type) => type.ToHumanReadable();
        static string FormatTicksStat(int ticks) => $"{(ticks * 0.05f).ToString("0.##", CultureInfo.InvariantCulture)}s";
        static string FormatGeneration(Blueprint.Blueprint b)
        {
            // TODO:Icons
            StringBuilder sb = new();
            if (b.HasMaterialGeneration)
                sb.Append("[MAT]").Append(FormatIntStat(b.materialGeneration));
            if (b.HasEnergyGeneration)
                sb.Append("[ENE]").Append(FormatIntStat(b.energyGeneration));
            if (b.HasFuelGeneration)
                sb.Append("[FUE]").Append(FormatIntStat(b.fuelGeneration));
            return sb.ToString();
        }
    }
}
