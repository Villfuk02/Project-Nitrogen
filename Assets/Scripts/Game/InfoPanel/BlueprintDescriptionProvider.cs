using System.Collections.Generic;
using System.Text;
using Game.Blueprint;
using UnityEngine;
using Utils;

namespace Game.InfoPanel
{
    public class BlueprintDescriptionProvider : DescriptionProvider
    {
        readonly DescriptionFormat.BlueprintProvider getBlueprint_;
        readonly Blueprinted? blueprinted_;
        readonly DescriptionFormatter<(DescriptionFormat.BlueprintProvider, Blueprint.Blueprint)> descriptionFormatter_;
        readonly Box<int> cooldown_;

        public BlueprintDescriptionProvider(Blueprinted blueprinted, Box<int>? cooldown = null)
        {
            blueprinted_ = blueprinted;
            getBlueprint_ = () => blueprinted.Blueprint;
            descriptionFormatter_ = DescriptionFormat.Blueprint(getBlueprint_, blueprinted.OriginalBlueprint);
            cooldown_ = cooldown;
        }

        public BlueprintDescriptionProvider(Blueprint.Blueprint blueprint, Blueprint.Blueprint original, Box<int>? cooldown = null)
        {
            getBlueprint_ = () => blueprint;
            descriptionFormatter_ = DescriptionFormat.Blueprint(getBlueprint_, original);
            cooldown_ = cooldown;
        }

        protected override string GenerateDescription() => descriptionFormatter_.Format(GenerateRawDescription());

        string GenerateRawDescription()
        {
            bool initialized = blueprinted_ is MonoBehaviour mb && mb != null;
            StringBuilder sb = new();
            List<string> statBlock = new();
            Blueprint.Blueprint blueprint = getBlueprint_();


            if (cooldown_ is not null)
            {
                if (cooldown_.value > 0 || blueprint.cooldown > 0)
                    AppendStat($"Cooldown {cooldown_.value}[+CD]");
            }
            else if (!initialized || !blueprinted_.Placed)
            {
                if (blueprint.cooldown > 0)
                    AppendStat("[$CD]");
                if (blueprint.startingCooldown > 0 || blueprint.cooldown > 0)
                    AppendStat("[$SCD]");
            }

            if (initialized)
                foreach (var stat in blueprinted_.GetExtraStats())
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