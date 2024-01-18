using BattleSimulation.World;
using System;
using System.Collections.Generic;
using Utils;

namespace Game.InfoPanel
{
    public class TileDescriptionProvider : DescriptionProvider
    {
        readonly Tile tile_;
        readonly DescriptionFormatter<Tile> descriptionFormatter_;

        public TileDescriptionProvider(Tile tile)
        {
            tile_ = tile;
            descriptionFormatter_ = DescriptionFormat.Tile(tile);
        }
        protected override string GenerateDescription() => descriptionFormatter_.Format(GenerateRawDescription());

        string GenerateRawDescription()
        {
            List<string> texts = new();

            if (tile_.slant != WorldUtils.Slant.None)
                texts.Add("Slanted");

            texts.Add(tile_.obstacle switch
            {
                Tile.Obstacle.None => "Empty",
                Tile.Obstacle.Path => "Attacker path runs across",
                Tile.Obstacle.Small => "Contains a small obstacle",
                Tile.Obstacle.Large => "Contains a large obstacle",
                Tile.Obstacle.Fuel => "Rich in [FUE]Fuel",
                Tile.Obstacle.Minerals => "Rich in [MAT]Minerals",
                _ => throw new ArgumentOutOfRangeException()
            });

            return string.Join("[BRK]", texts);
        }

    }
}
