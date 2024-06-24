using System.Collections.Generic;
using System.Text;
using BattleSimulation.Attackers;

namespace Game.InfoPanel
{
    public class AttackerDescriptionProvider : DescriptionProvider
    {
        readonly Attacker? attacker_;
        readonly AttackerStats.AttackerStats stats_;
        readonly DescriptionFormatter<(AttackerStats.AttackerStats, Attacker)> descriptionFormatter_;

        public AttackerDescriptionProvider(Attacker attacker)
        {
            attacker_ = attacker;
            stats_ = attacker.stats;
            descriptionFormatter_ = DescriptionFormat.AttackerFormatter(stats_, attacker);
        }

        public AttackerDescriptionProvider(AttackerStats.AttackerStats stats)
        {
            stats_ = stats;
            descriptionFormatter_ = DescriptionFormat.AttackerFormatter(stats, null);
        }

        protected override string GenerateDescription() => descriptionFormatter_.Format(GenerateRawDescription());

        string GenerateRawDescription()
        {
            List<string> stats = new()
            {
                "Size [SIZ]",
                "Speed [SPD]",
                attacker_ == null ? "Health [MHP]" : "Health [HP/M]"
            };

            StringBuilder sb = new();
            sb.AppendJoin('\n', stats);
            foreach (string desc in stats_.descriptions)
            {
                sb.Append("[BRK]");
                sb.Append(desc);
            }

            return sb.ToString();
        }
    }
}