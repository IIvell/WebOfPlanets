using UnityEngine;

namespace WebOfPlanets
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
            private bool _missingLogged;

            public Requirement(string itemName, int amount)
            {
                this.itemName = itemName;
                this.amount   = amount;
            }

            public Item Item
            {
                get
                {
                    if (_item == null)
                    {
                        _item = Resources.Load<Item>(itemName);
                        // Glasno umjesto tihog nulla: typo/preimenovani asset bi
                        // inače značio prag koji se bez ikakve poruke više nikad
                        // ne može otključati.
                        if (_item == null && !_missingLogged)
                        {
                            _missingLogged = true;
                            Debug.LogError($"[HubProgress] Item asset '{itemName}' nije pronađen u Resources — prag koji ga traži NE MOŽE se otključati (typo ili preimenovani asset?).");
                        }
                    }
                    return _item;
                }
            }

            public string DisplayName => Item != null ? Item.displayName : itemName;
        }

        // Sve o jednom pragu na jednom mjestu (prije su zahtjevi, bonus skladišta
        // i prikazni tekst bili tri ručno sinkronizirana polja). NAPOMENA: popis
        // otključanih recepata u Unlocks mora pratiti CraftingRecipe.unlockTier
        // na assetima, a "+N" u tekstu vrijednost StorageBonus — provjereno
        // usklađeno 24.7.2026. (audit).
        public sealed class TierInfo
        {
            public readonly Requirement[] Requirements;
            public readonly int StorageBonus;
            public readonly string Unlocks;

            public TierInfo(Requirement[] requirements, int storageBonus, string unlocks)
            {
                Requirements = requirements;
                StorageBonus = storageBonus;
                Unlocks      = unlocks;
            }
        }

        // Zahtjevi po pragu — svaki prag troši resurse dohvatljive alatima/strojevima
        // prethodnog praga i tjera igrača na sljedeći tip planeta:
        // 1 rudarski → 2 organski+topionica → 3 ledeni+plinoviti → 4 vulkanski (Rune Drill iz praga 3) → 5 sve grane.
        // Kumulativno: na max pragu skladište je baznih 100 + 250.
        public static readonly TierInfo[] Tiers =
        {
            // Balans kolovoz 2026.: ruda praga 1 i metal pragova 2-5 prepolovljeni
            // (6→3, 6/8/10/12 → 3/4/5/6) — stari su iznosi predugo držali igrača
            // na farmanju istog resursa prije prvog otključavanja.
            new TierInfo(new[]
            {
                new Requirement("Mining_stone", 10),
                new Requirement("Mining_ore",    3),
            }, 25, "Collector Machine, Smelter, Network Scanner, Hub Storage +25"),

            new TierInfo(new[]
            {
                new Requirement("Metal_ingot",   3),
                new Requirement("Organic_wood",  5),
                new Requirement("Organic_plant", 4),
            }, 25, "Drill, Hub Uplink, Teleporter, Gas Mask, Hub Storage +25"),

            new TierInfo(new[]
            {
                new Requirement("Metal_ingot",   4),
                new Requirement("Water_ice",     6),
                new Requirement("Gaseous_plin",  4),
            }, 50, "Ore Extractor, Gas Extractor, Cryo Harvester, Rune Drill, Respawn Totem, Hub Storage +50"),

            new TierInfo(new[]
            {
                new Requirement("Metal_ingot",   5),
                new Requirement("Volcanic_rune", 4),
            }, 50, "Blast Furnace, Eternal Pickaxe, Network Computer, Hub Storage +50"),

            new TierInfo(new[]
            {
                new Requirement("Metal_ingot",   6),
                new Requirement("Volcanic_rune", 6),
                new Requirement("Gaseous_plin",  6),
                new Requirement("Water_ice",     6),
            }, 100, "Two-Way Teleporter, Hub Storage +100"),
        };

        // Zbroj bonusa svih dosad otključanih pragova; HubStorage.MaxCapacity ga pribraja.
        public static int StorageBonus
        {
            get
            {
                int bonus = 0;
                for (int i = 0; i < Tier && i < Tiers.Length; i++)
                    bonus += Tiers[i].StorageBonus;
                return bonus;
            }
        }

        public static int Tier { get; private set; }
        public static int MaxTier => Tiers.Length;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Tier = 0;

        public static bool IsUnlocked(int tier) => Tier >= Mathf.Min(tier, MaxTier);

        public static bool CanUnlockNext()
        {
            if (Tier >= MaxTier) return false;
            if (GameManager.TestingMode) return true;
            if (HubStorage.Instance == null) return false;

            foreach (var req in Tiers[Tier].Requirements)
            {
                var inv = req.Item != null ? HubStorage.Instance.Get(req.Item) : null;
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
                foreach (var req in Tiers[Tier].Requirements)
                    for (int i = 0; i < req.amount; i++)
                        HubStorage.Instance.Remove(req.Item);

            Tier++;
            Debug.Log($"[HubProgress] Prag {Tier} otključan — novi recepti dostupni.");
            GameEventBus.RaiseRecipeTierUnlocked(Tier);
            return true;
        }
    }
}
