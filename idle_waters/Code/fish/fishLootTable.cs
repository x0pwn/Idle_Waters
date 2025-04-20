using Sandbox;
using System.Collections.Generic;

public static class FishLootTable
{
    private static List<Fish> fishPool = new()
    {
        new Fish { Name = "Smol Carp", Value = 5, Rarity = 0.6f },
        new Fish { Name = "Medium Carp", Value = 10, Rarity = 0.25f },
        new Fish { Name = "Large Carp", Value = 50, Rarity = 0.1f },
        new Fish { Name = "Boot", Value = 1, Rarity = 0.5f }
    };

    public static Fish GenerateFish()
    {
        float roll = Game.Random.Float(0f, 1f);
        float cumulative = 0f;

        foreach (var fish in fishPool)
        {
            cumulative += fish.Rarity;
            if (roll <= cumulative)
                return fish;
        }

        // Fallback in case of weird numbers
        return fishPool[0];
    }
}
