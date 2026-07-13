using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Hub napredak: pragovi se otključavaju na Hub računalu (HubProgressUI) trošenjem
    // specifičnih resursa iz Hub skladišta — česti resursi za prag 1, prerađeni i
    // rijetki za prag 2. Otključan prag otključava recepte (CraftingRecipe.unlockTier).
    public static class HubProgress
    {
        public class Requirement
        {
            // Ime asseta u "Assets/Scriptable Objects/Resources" (Unity Resources folder).
            public readonly string itemName;
            public readonly int amount;
            private Item _item;

            public Requirement(string itemName, int amount)
            {
                this.itemName = itemName;
                this.amount   = amount;
            }

            public Item Item => _item != null ? _item : _item = Resources.Load<Item>(itemName);
            public string DisplayName => Item != null ? Item.displayName : itemName;
        }

        // Zahtjevi po pragu, poredani po rijetkosti: prag 1 = česti resursi s početnog
        // rudarskog planeta, prag 2 = prerađeni (topionica) + rijetki (ledeni/vulkanski planeti).
        public static readonly Requirement[][] TierRequirements =
        {
            new[]
            {
                new Requirement("Mining_stone", 10),
                new Requirement("Mining_ore",    6),
            },
            new[]
            {
                new Requirement("Metal_ingot",   6),
                new Requirement("Water_ice",     4),
                new Requirement("Volcanic_rune", 2),
            },
        };

        // Kratki opis što koji prag otključava (prikaz na Hub računalu).
        public static readonly string[] TierUnlocks =
        {
            "Drill, Ore Collector, Hub Uplink, Ore Extractor",
            "Rune Drill, Eternal Pickaxe, Cryo Harvester, Blast Furnace, Gas Extractor",
        };

        public static int Tier { get; private set; }
        public static int MaxTier => TierRequirements.Length;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Tier = 0;

        public static bool IsUnlocked(int tier) => Tier >= Mathf.Min(tier, MaxTier);

        public static bool CanUnlockNext()
        {
            if (Tier >= MaxTier || HubStorage.current == null) return false;

            foreach (var req in TierRequirements[Tier])
            {
                var inv = req.Item != null ? HubStorage.current.Get(req.Item) : null;
                if (inv == null || inv.GetStackSize() < req.amount) return false;
            }
            return true;
        }

        // Troši resurse iz Hub skladišta i otključava sljedeći prag.
        public static bool TryUnlockNext()
        {
            if (!CanUnlockNext()) return false;

            foreach (var req in TierRequirements[Tier])
                for (int i = 0; i < req.amount; i++)
                    HubStorage.current.Remove(req.Item);

            Tier++;
            Debug.Log($"[HubProgress] Prag {Tier} otključan — novi recepti dostupni.");
            GameEventBus.RaiseRecipeTierUnlocked(Tier);
            return true;
        }
    }
}
