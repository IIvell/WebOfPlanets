using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Hub napredak: pragovi se otključavaju na Hub računalu (HubProgressUI) trošenjem
    // specifičnih resursa iz Hub skladišta, od čestih prema rijetkima. Otključan prag
    // otključava recepte (CraftingRecipe.unlockTier) i povećava kapacitet Hub
    // skladišta (TierStorageBonus → HubStorage.MaxCapacity).
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

        // Zahtjevi po pragu — svaki prag troši resurse dohvatljive alatima/strojevima
        // prethodnog praga i tjera igrača na sljedeći tip planeta:
        // 1 rudarski → 2 organski+topionica → 3 ledeni+plinoviti → 4 vulkanski (Rune Drill iz praga 3) → 5 sve grane.
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
                new Requirement("Organic_wood",  5),
                new Requirement("Organic_plant", 4),
            },
            new[]
            {
                new Requirement("Metal_ingot",   8),
                new Requirement("Water_ice",     6),
                new Requirement("Gaseous_plin",  4),
            },
            new[]
            {
                new Requirement("Metal_ingot",  10),
                new Requirement("Volcanic_rune", 4),
            },
            new[]
            {
                new Requirement("Metal_ingot",  12),
                new Requirement("Volcanic_rune", 6),
                new Requirement("Gaseous_plin",  6),
                new Requirement("Water_ice",     6),
            },
        };

        // Bonus kapaciteta Hub skladišta koji donosi otključavanje praga (indeks = prag-1).
        // Kumulativno: na max pragu skladište je baznih 100 + 250.
        public static readonly int[] TierStorageBonus = { 25, 25, 50, 50, 100 };

        // Zbroj bonusa svih dosad otključanih pragova; HubStorage.MaxCapacity ga pribraja.
        public static int StorageBonus
        {
            get
            {
                int bonus = 0;
                for (int i = 0; i < Tier && i < TierStorageBonus.Length; i++)
                    bonus += TierStorageBonus[i];
                return bonus;
            }
        }

        // Kratki opis što koji prag otključava (prikaz na Hub računalu).
        public static readonly string[] TierUnlocks =
        {
            "Collector Machine, Ore Collector, Network Scanner, Hub Storage +25",
            "Drill, Hub Uplink, Teleporter, Gas Mask, Hub Storage +25",
            "Ore Extractor, Gas Extractor, Cryo Harvester, Rune Drill, Respawn Totem, Hub Storage +50",
            "Blast Furnace, Eternal Pickaxe, Network Computer, Hub Storage +50",
            "Two-Way Teleporter, Hub Storage +100",
        };

        public static int Tier { get; private set; }
        public static int MaxTier => TierRequirements.Length;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Tier = 0;

        public static bool IsUnlocked(int tier) => Tier >= Mathf.Min(tier, MaxTier);

        public static bool CanUnlockNext()
        {
            if (Tier >= MaxTier) return false;
            if (GameManager.TestingMode) return true;
            if (HubStorage.current == null) return false;

            foreach (var req in TierRequirements[Tier])
            {
                var inv = req.Item != null ? HubStorage.current.Get(req.Item) : null;
                if (inv == null || inv.GetStackSize() < req.amount) return false;
            }
            return true;
        }

        // Load iz save datoteke: postavlja prag BEZ RaiseRecipeTierUnlocked —
        // event bi na max tieru ponovno okinuo VictoryUI; UI-i ionako čitaju
        // Tier direktno pri svakom renderu.
        public static void LoadTier(int tier) => Tier = Mathf.Clamp(tier, 0, MaxTier);

        // Troši resurse iz Hub skladišta i otključava sljedeći prag.
        public static bool TryUnlockNext()
        {
            if (!CanUnlockNext()) return false;

            if (!GameManager.TestingMode)
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
