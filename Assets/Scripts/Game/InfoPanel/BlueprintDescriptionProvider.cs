using System;
using System.Collections.Generic;
using System.Text;
using Game.Blueprint;
using Utils;

namespace Game.InfoPanel
{
    public class BlueprintDescriptionProvider : DescriptionProvider
    {
        readonly IBlueprintProvider provider_;
        readonly DescriptionFormatter<IBlueprintProvider> descriptionFormatter_;
        readonly Func<int>? getCooldown_;

        public BlueprintDescriptionProvider(IBlueprintProvider provider, Func<int>? getCooldown = null)
        {
            provider_ = provider;
            descriptionFormatter_ = DescriptionFormat.BlueprintFormatter(provider);
            getCooldown_ = getCooldown;
        }

        protected override string GenerateDescription() => descriptionFormatter_.Format(GenerateRawDescription());

        string GenerateRawDescription()
        {
            Blueprinted blueprinted = provider_ as Blueprinted;
            StringBuilder sb = new();
            List<string> statBlock = new();
            Blueprint.Blueprint blueprint = provider_.GetBaseBlueprint();


            if (getCooldown_ is not null)
            {
                if (getCooldown_() > 0)
                    AppendStat($"Cooldown {getCooldown_()}[+CD]".Colored(TextUtils.CHANGED_COLOR));
                else if (Blueprint.Blueprint.Cooldown.Query(blueprint) > 0)
                    AppendStat($"Cooldown {getCooldown_()}[+CD]");
            }
            else if (blueprinted == null || !blueprinted.Placed)
            {
                int cooldown = Blueprint.Blueprint.Cooldown.Query(blueprint);
                int startingCooldown = Blueprint.Blueprint.StartingCooldown.Query(blueprint);
                if (cooldown > 0)
                    AppendStat("[$CD]");
                if (startingCooldown > 0 || cooldown > 0)
                    AppendStat("[$SCD]");
            }

            if (blueprinted != null)
                foreach (var stat in blueprinted.GetExtraStats())
                    AppendStat(stat);

            foreach (var stat in blueprint.statsToDisplay)
                AppendStat(stat);

            FlushStatBlock();

            foreach (string desc in blueprint.descriptions)
            {
                sb.Append("[BRK]");
                sb.Append(desc);
            }

            return sb.ToString();

            void AppendStat(string stat)
            {
                if (stat.Length <= 0 || stat == "[BRK]")
                    FlushStatBlock();
                else
                    statBlock.Add(stat);
            }

            void FlushStatBlock()
            {
                if (statBlock.Count <= 0)
                    return;
                if (sb.Length > 0)
                    sb.Append("[BRK]");
                sb.AppendJoin('\n', statBlock);
                statBlock.Clear();
            }
        }
    }
}