---
tags: [dizajn, moj-projekt, resursi]
---

# Resursi

**9 itema** (ScriptableObjecti u `Assets/_Project/Data/Resources/` — Resources folder, jer ih [[Koncept igre|SaveSystem]] i crafting dohvaćaju po imenu): 7 se spawna na planetima, 2 su craft-only.

| Item (asset) | Ime | Gdje | Način | Alat | Regen |
|---|---|---|---|---|---|
| `Mining_stone` | Stone | [[Rudarski planet (Mining)\|Mining]] + Hub | mining 3 s, daje 2–4 (+25% bonus Ore) | Pickaxe | — |
| `Mining_ore` | Ore | **samo Hub** | mining 5 s | Pickaxe | — |
| `Organic_wood` | Wood | [[Organski planet (Organic)\|Organic]] | mining 3 s | Axe | 10 s |
| `Organic_plant` | Plant | Organic | pickup (instant) | — | 5 s |
| `Water_ice` | Ice | [[Ledeni planet (Ice)\|Ice]] | pickup | — | — |
| `Gaseous_plin` | Gas | [[Plinski planet (Gaseous)\|Gaseous]] | pickup | — | 8 s |
| `Volcanic_rune` | Rune | [[Vulkanski planet (Volcanic)\|Volcanic]] | mining 6 s, uvijek mining verzija | **Rune Drill** | — |
| `Metal_ingot` | Metal Ingot | craft-only | Smelter (2 Ore→1) / Blast Furnace (3 Ore→2) | — | — |
| `Water_liquid` | Water | craft-only | Blast Furnace (2 Ice→1) / Gas Extractor | — | — |

## Mehanika skupljanja

- Pri spawnu svaka instanca postaje **pickup ili mining verzija**: `Random.value < pickupChance` (0.5 za sve osim Rune = 0). Pickup = instant, bez alata, bez bonusa; mining = drži interakciju `miningTime` sekundi, provjerava alat, baca yield + bonus.
- **Stvarno vrijeme = miningTime / speedMultiplier alata** (npr. Rune 6 s ÷ Rune Drill 5× = 1.2 s).
- Provjera alata: točan alat **ili** ista `ToolClass` + `miningTier ≥` traženog → bolji alat otvara i niže resurse ([[Alati]]).
- **Regeneracija** (`regenerationTime > 0`): objekt ostaje, privremeno neuberiv → obnovljivi su Wood, Gas, Plant. Ostali nestaju uz DisintegrationEffect.
- Broj instanci po planetu: `max(1, round(Random(minGustoća, maxGustoća) × radijus))`.

## GDD vs. implementacija

GDD-ove tri faze skupljanja (ručno → alati → automatizacija, §6) jesu implementirane → [[Osnovna petlja igre]]. Nisu: radijus skupljanja, popravak alata, per-planet skladišta i automatski transport ruta (najbliže: Hub Uplink šalje 2 itema / 5 s u Hub skladište).

## Povezano

- [[Alati]] · [[Tipovi planeta]] · [[Progresija kroz Hub]] · [[Factory žanr]]

## Moguća potpitanja

- *„Kako su resursi definirani u kodu?"* → `Item` ScriptableObject (data-driven dizajn): displayName, prefabi, miningTime, requiredTool, regenerationTime, yield, bonus — novi resurs = novi asset, bez koda.
- *„Zašto pickup i mining verzija istog resursa?"* → pickup daje trenutnu gratifikaciju i popunjava svijet; mining verzija nosi progresijski zid (alat) i veći yield.
