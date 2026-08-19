---
tags: [dizajn, moj-projekt]
---

# Osnovna petlja igre

Core loop = slijed radnji koji se ponavlja i drži igrača u igri. U Web of Planets:

```
Istraži planet
    → skupljaj resurse (ručno rudarenje)
    → izradi alate i strojeve (crafting)
    → postavi strojeve → automatizirano skupljanje
    → izgradi vezu prema novom planetu
    → uloži resurse u Hub → novi recepti / napredak
        → proširi mrežu na sljedeći planet … (petlja se ponavlja)
```

## Petlja održavanja (protuteža rastu)

Paralelno s petljom rasta teče **petlja troška** — ono što mreži daje napetost:

- veze **degradiraju** s vremenom (tri razine: weak/mid/strong — jeftinije = kraće traju),
- strojevi se **kvare**, češće na nestabilnim planetima,
- popravci i održavanje troše iste resurse koje igrač želi uložiti u napredak.

Igrač stalno bira: **širiti mrežu ili održavati postojeću** — to je središnja odluka igre. → [[Koncept igre]]

## Progresija skupljanja (tri faze, GDD §6)

1. **Ručno** — sporo, igrač upoznaje planet.
2. **Alati** — brže skupljanje, specijalizirani alati (npr. plinska maska za plinske planete).
3. **Automatizacija** — strojevi rade bez prisutnosti igrača ([[Factory žanr]]).

## GDD vs. implementacija

GDD-ova petlja uključuje i **artefakte** i **drevne (skrivene) veze** — to je *dizajnerska vizija* koja je u implementaciji svjesno sužena. Implementirana petlja gore je stvarno stanje igre: rudarenje → crafting → veze → Hub → pobjeda. Za obranu: razlikovati što je u GDD-u, a što u kodu — smanjenje opsega je normalna inženjerska odluka.

## Povezano

- [[Koncept igre]] · [[Žanr]]

## Moguća potpitanja

- *„Kako igra završava?"* → Hub progresija kroz pragove ulaganja resursa; zadnji prag = pobjeda (VictoryUI).
- *„Što igrača motivira na povratak starim planetima?"* → pražnjenje strojeva, popravci, održavanje veza.
- *„Gdje je tu survival?"* → istraživanje novih planeta nosi rizik (hazardi, mobovi) → [[Survival žanr]].
