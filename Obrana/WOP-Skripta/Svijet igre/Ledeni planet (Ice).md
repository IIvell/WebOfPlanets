---
tags: [dizajn, moj-projekt, planeti]
---

# Ledeni planet (Ice)

Tip s **mehaničkim** izazovom umjesto štete: klizava površina.

- **Resurs:** **Ice** (gustoća 0.2–0.4) — čisti pickup, bez alata, **ne regenerira se**.
- **Klizavost:** `PlayerController` detektira `Type == Ice` i prebacuje na `MoveOnIce()`; collideru igrača se dodjeljuje frictionless `PhysicsMaterial` (trenje 0) → inercija, teže zaustavljanje.
- **Water** postoji kao item, ali se **nigdje ne spawna** — dobiva se samo iz Blast Furnacea (2 Ice → 1 Water) ili Gas Extractora.
- Bez štete po zdravlje (samo mobovi). Materijal: `M_Planet_Ice.mat`.

## GDD vs. implementacija

Klizave površine iz GDD-a **jesu implementirane**. Krioplin, fosili i ciklično tajanje nisu; Voda kao spawnani resurs zamijenjena je craftanjem.

## Povezano

- [[Tipovi planeta]] · [[Resursi]] · [[Progresija kroz Hub]] (prag 3 i 5 traže Ice)

## Moguća potpitanja

- *„Kako je izvedena klizavost?"* → fizikalni materijal bez trenja + posebna grana kretanja u PlayerControlleru — sila umjesto izravnog postavljanja brzine.
- *„Čemu služi Voda ako se ne spawna?"* → craft-only međuresurs; pokazuje lanac prerade (Ice → Water) po uzoru na factory žanr → [[Factory žanr]].
