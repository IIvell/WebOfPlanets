---
tags: [dizajn, moj-projekt, progresija]
---

# Cilj i kraj igre

## Pobjeda

**Uvjet: otključan 5. (zadnji) hub prag.** Nema drugog uvjeta — nije "spoji sve planete" ni "izgradi N veza".

- `VictoryUI` (klasa u `MainMenuUI.cs`) sluša `RecipeTierUnlocked`; kad `tier == HubProgress.MaxTier` (5) → ekran **"NETWORK COMPLETE"**, `GameState.Victory`, `Time.timeScale = 0`.
- Gumbi: **Keep Playing** (nastavak igre, ekran se više ne vraća) i **Quit** — pobjeda ne briše svijet.
- Pri učitavanju savea s otključanim pragom event se namjerno **ne** emitira ponovno — inače bi pobjednički ekran iskakao na svaki load.

## Smrt (nema pravog poraza)

- Zdravlje 100, bez ijednog izvora liječenja (svjesna odluka — `Heal()` postoji, nitko ga ne zove).
- Izvori štete: mobovi (10 po dodiru), lava zone (15 dmg/s), otrovna atmosfera (5 dmg/s).
- Smrt → `GameState.GameOver`, pauza → tipka **R** → respawn na **Respawn Totemu** (glavni na Hubu; dodatni craftabilni od praga 3) s punim zdravljem, **inventar ostaje**. Komentar u kodu: *"smrt košta samo povratak"*.
- Nema game-over ekrana koji završava partiju — smrt je setback, ne kraj ([[Survival žanr|survival-lite]]).

## GDD vs. implementacija

**GDD uopće ne definira uvjet pobjede** — najbliže je "završni log, kraj priče" (§8.5) i neiskorišteni `MilestoneType.NetworkComplete`. Kod je uvjet definirao sam: 5/5 pragova; naziv ekrana preuzet je iz tog enuma. Za smrt GDD kaže "oporavak nije još definiran" — kod je otišao dalje (respawn totem sustav).

## Povezano

- [[Progresija kroz Hub]] · [[Osnovna petlja igre]] · [[Koncept igre]]

## Moguća potpitanja

- *„Kako igra završava?"* → uloži resurse u 5. prag na Hub računalu → NETWORK COMPLETE; može se nastaviti igrati.
- *„Zašto smrt ne završava igru?"* → svijet je proceduralan i mreža živi u memoriji — restart bi bio frustrirajući i tehnički skup (scene se ne reloadaju); trošak smrti je izgubljeno vrijeme povratka.
- *„Zašto nema liječenja?"* → šteta postaje trajna cijena nepažnje po planetu; balansira činjenicu da je respawn blag.
