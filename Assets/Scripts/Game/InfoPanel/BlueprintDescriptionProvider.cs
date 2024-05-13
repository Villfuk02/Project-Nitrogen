using System.Collections.Generic;
using System.Text;
using Game.Blueprint;
using UnityEngine;
using Utils;

namespace Game.InfoPanel
{
    public class BlueprintDescriptionProvider : DescriptionProvider
    {
        readonly Blueprint.Blueprint blueprint_;
        readonly IBlueprinted? blueprinted_;
        readonly DescriptionFormatter<(Blueprint.Blueprint, Blueprint.Blueprint)> descriptionFormatter_;
        readonly Box<int> cooldown_;

        public BlueprintDescriptionProvider(IBlueprinted blueprinted, Box<int>? cooldown = null) : this(blueprinted.Blueprint, blueprinted.OriginalBlueprint, cooldown)
        {
            blueprinted_ = blueprinted;
        }

        public BlueprintDescriptionProvider(Blueprint.Blueprint blueprint, Blueprint.Blueprint original, Box<int>? cooldown = null)
        {
            blueprint_ = blueprint;
            descriptionFormatter_ = DescriptionFormat.Blueprint(blueprint, original);
            cooldown_ = cooldown;
        }

        protected override string GenerateDescription() => descriptionFormatter_.Format(GenerateRawDescription());

        string GenerateRawDescription()
        {
            bool initialized = blueprinted_ is MonoBehaviour mb && mb != null;
            StringBuilder sb = new();
            List<string> statBlock = new();

            if (cooldown_ is not null)
            {
                if (cooldown_.value > 0 || blueprint_.cooldown > 0)
                    AppendStat($"Cooldown {cooldown_.value}[+CD]");
            }
            else if (!initialized || !blueprinted_.Placed)
            {
                if (blueprint_.cooldown > 0)
                    AppendStat("[$CD]");
                if (blueprint_.startingCooldown > 0 || blueprint_.cooldown > 0)
                    AppendStat("[$SCD]");
            }

            if (initialized)
                foreach (var stat in blueprinted_.GetExtraStats())
                    AppendStat(stat);

            foreach (var stat in blueprint_.statsToDisplay)
                AppendStat(stat);

            if (!initialized)
                foreach (var stat in blueprint_.statsToDisplayWhenUninitialized)
                    AppendStat(stat);

            FlushStatBlock();

            foreach (string desc in blueprint_.descriptions)
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
