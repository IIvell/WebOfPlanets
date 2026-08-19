---
tags: [dizajn, moj-projekt, progresija]
---

# Progresija kroz Hub

Progresija = **5 linearnih pragova** (`HubProgress.cs`, statička klasa, `MaxTier = 5`). Otključava se na Glavnom Računalu Huba (`HubProgressUI`), a resursi se troše iz **Hub skladišta**, ne iz inventara — igrač prvo mora dopremiti resurse na Hub.

| Prag | Traži | Skladište | Otključava |
|---|---|---|---|
| 1 | Stone 10, Ore 6 | +25 | Collector Machine, Ore Collector, Network Scanner |
| 2 | Metal Ingot 6, Wood 5, Plant 4 | +25 | Drill, Hub Uplink, Teleporter, **Gas Mask** |
| 3 | Metal Ingot 8, Ice 6, Gas 4 | +50 | Ore/Gas Extractor, Cryo Harvester, **Rune Drill**, Respawn Totem |
| 4 | Metal Ingot 10, **Rune 4** | +50 | Blast Furnace, Eternal Pickaxe, Network Computer |
| 5 | Metal Ingot 12, Rune 6, Gas 6, Ice 6 | +100 | Two-Way Teleporter → **pobjeda** |

## Dizajn lanca (komentar u kodu)

Svaki prag troši resurse dohvatljive alatima/strojevima **prethodnog** praga i tjera igrača na **sljedeći tip planeta**:

```
1 rudarski → 2 organski + topionica → 3 ledeni + plinoviti → 4 vulkanski → 5 sve grane
```

Gas Mask (prag 2) otvara plinske planete za prag 3; Rune Drill (prag 3) otvara rune za prag 4. Skladište raste 100 → **350** (kumulativno +250).

## Veza s craftingom

Recepti imaju `unlockTier`; `IsUnlocked => HubProgress.IsUnlocked(unlockTier)`. **20 recepata**, 4 dostupna od starta (Pickaxe, Axe, Smelter, Storage Machine). Otključavanje emitira `RecipeTierUnlocked` na [[Koncept igre|event bus]] — UI i VictoryUI samo slušaju. `GameManager.TestingMode` sve pragove čini besplatnima (demo!).

## GDD vs. implementacija

GDD (§8.3) zamišlja **granato upgrade stablo** (Računalo/Skladište/Portal s razinama) i priču kroz milestone-ove (§8.5) — implementiran je **linearan lanac od 5 pragova** bez grananja i bez priče (milestone enumi postoje u event busu kao rezervirano sučelje). GDD-ova brojka "skladište 100" je preživjela kao `HubStorage.maxCapacity`.

## Povezano

- [[Osnovna petlja igre]] · [[Cilj i kraj igre]] · [[Resursi]] · [[Alati]] · [[Tipovi planeta]]

## Moguća potpitanja

- *„Zašto linearno, a ne stablo kao u GDD-u?"* → opseg završnog rada; linearan lanac je čitljiviji igraču i lakši za balansiranje, a zadržava istu funkciju (ritam otključavanja).
- *„Kako pragovi vode igrača kroz svijet?"* → svaki prag traži resurs s tipa planeta koji igrač još nije svladao, a prethodni prag daje alat/opremu za njega — progresija i istraživanje su isti sustav.
- *„Što sprječava preskakanje pragova?"* → resursi: bez Rune Drilla (prag 3) nema runa za prag 4; bez maske (prag 2) je plin za prag 3 skup po zdravlju.
