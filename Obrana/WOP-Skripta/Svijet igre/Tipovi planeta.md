---
tags: [dizajn, moj-projekt, planeti]
---

# Tipovi planeta

U igri postoji **5 tipova planeta**: `enum PlanetType { Mining, Organic, Ice, Gaseous, Volcanic }` (definiran u `GameEventBus.cs`, ne u `Planet.cs`).

| Tip                                       | Resurs            | Opasnost                    | Nestabilan? |
| ----------------------------------------- | ----------------- | --------------------------- | ----------- |
| [[Rudarski planet (Mining)\|Mining]]      | Stone (Hub i Ore) | samo mobovi                 | ne          |
| [[Organski planet (Organic)\|Organic]]    | Wood, Plant       | samo mobovi                 | ne          |
| [[Ledeni planet (Ice)\|Ice]]              | Ice               | klizava površina            | ne          |
| [[Plinski planet (Gaseous)\|Gaseous]]     | Gas               | otrovna atmosfera (5 dmg/s) | **da**      |
| [[Vulkanski planet (Volcanic)\|Volcanic]] | Rune              | lava zone (15 dmg/s)        | **da**      |

## Kako se tipovi dodjeljuju

- `PlanetCreator` spawna **30 planeta** (`startingPlanets`) lančano: svaki novi se sidri na nasumični već postojeći planet unutar dometa veze → graf potencijalnih veza je **povezan po konstrukciji**.
- Tip je **uniformno nasumičan (20% po tipu)** — bez težina i bez garancije "barem jedan od svakog". Teoretski moguća partija bez vulkanskih planeta (≈ 0.6%), što bi blokiralo 4. hub prag.
- **Hub je Mining tip**, ali nije generiran — objekt je u sceni (`IsHub: 1`). Jedini je izvor **Ore** resursa.
- Svi planeti su ista Unity sfera — tipovi se razlikuju **materijalom** (`M_Planet_*.mat`, teksture iz asset packova) i nasumičnom rotacijom, ne terenom.

## Nestabilnost

```csharp
public bool IsUnstable => Type == PlanetType.Volcanic || Type == PlanetType.Gaseous;
```

Jedina posljedica u kodu: veze uz nestabilan kraj degradiraju **1.5×** (jedan kraj) ili **2×** (oba) brže. Kvarenje strojeva na nestabilnim planetima je u GDD-u, ali **nije implementirano**.

## GDD vs. implementacija

GDD (§3.1) ima **6 tipova** — šesti je **Napušteni** (ruševine, artefakti) i nije implementiran, kao ni cijeli sustav artefakata (GDD §7). GDD-ovi sekundarni resursi (kristali, smola, fosili, plazma, obsidijan…) također ne postoje — svaki tip u kodu ima 1–2 resursa.

## Povezano

- [[Koncept igre]] · [[Osnovna petlja igre]] · [[Resursi]] · [[Progresija kroz Hub]]

## Moguća potpitanja

- *„Zašto baš 5 tipova?"* → svaki tip = jedna grana resursa koju progresija traži ([[Progresija kroz Hub]] tjera igrača redom na svaki tip).
- *„Kako garantirate da igrač može doći do svakog planeta?"* → lančani spawn: svaki planet se sidri unutar dometa veze na postojeći → mreža je povezana po konstrukciji.
- *„Po čemu se planeti fizički razlikuju?"* → materijal, radijus (scale 35–100 → radijus 17.5–50), gravitacija (10–40), resursi i hazardi; teren je uvijek glatka sfera (svjesno pojednostavljenje).
