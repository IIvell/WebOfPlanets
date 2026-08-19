---
tags: [dizajn, moj-projekt, planeti]
---

# Rudarski planet (Mining)

Osnovni, "sigurni" tip — bez hazarda (samo mobovi kao svugdje). Prvi tip s kojim igrač radi.

- **Resurs:** [[Resursi|Stone]] (gustoća 0.2–0.4 po jedinici radijusa; ~10–20 kamenova na velikom planetu). Mining verzija traži **Pickaxe**, daje 2–4 komada + **25% šanse za bonus Ore**.
- **Hub je Mining planet** i poseban slučaj: uz Stone spawna i **Ore** — jedini prirodni izvor rude u igri (dalje ju proizvodi Ore Extractor, prag 3).
- Stone i Ore se **ne regeneriraju** — potrošeni nestaju (DisintegrationEffect).
- Materijal: `M_Planet_Mining.mat` (equirektangularna foto-tekstura, `SphericalUV`).

## GDD vs. implementacija

GDD predviđa kristale, rijetke metale, špilje i nižu gravitaciju — ništa od toga nije implementirano; gravitacija je nasumična (10–40) kao na svim planetima.

## Povezano

- [[Tipovi planeta]] · [[Resursi]] · [[Alati]] · [[Progresija kroz Hub]] (prag 1: Stone 10 + Ore 6)

## Moguća potpitanja

- *„Odakle igraču prva ruda?"* → s Huba — Hub je Mining planet s Ore spawnovima; prvi recepti (Pickaxe, Smelter) su dostupni od starta.
