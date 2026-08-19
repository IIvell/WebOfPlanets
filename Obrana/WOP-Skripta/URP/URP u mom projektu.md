---
tags: [tehnologije, moj-projekt]
---

# URP u mom projektu

Kako je [[URP]] konkretno konfiguriran u Web of Planets:

- `Assets/_Project/Settings/PC_RPAsset.asset` — pipeline asset koji build koristi (postavljen u Graphics Settings), uparen s `PC_Renderer`.
- `Mobile_RPAsset.asset` + `Mobile_Renderer` — varijanta s nižim postavkama; ista igra se zamjenom **jednog asseta** prilagodi slabijem hardveru. Dobar primjer za „zašto [[URP]]" argument konfigurabilnosti.
- Svi materijali koriste URP shadere, standardno **`Universal Render Pipeline/Lit`** (nikad Built-in → vidi gotchu u [[Built-in pipeline]]).
- Color space: [[Linear color space|Linear]].

## Moguća potpitanja

- *„Što se dogodi ako ubacite stari shader?"* → ružičasti materijal, [[Built-in pipeline]].
- *„Zašto dva pipeline asseta?"* → priprema za razlike u hardveru; build trenutno cilja PC.
