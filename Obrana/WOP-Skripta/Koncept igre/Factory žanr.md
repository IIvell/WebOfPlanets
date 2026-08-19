---
tags: [dizajn, teorija]
---

# Factory žanr

Žanr automatizacije: igrač gradi **strojeve i logističke lance** koji rade umjesto njega, pa se fokus s ručnog rada premješta na **planiranje i optimizaciju proizvodnje**. Ključni obrazac: ručni rad → alati → potpuna automatizacija.

Poznati primjeri: *Factorio*, *Satisfactory*, *Dyson Sphere Program*.

## Factory elementi u Web of Planets

- **Strojevi** — `CollectorMachine` (skuplja resurse na planetu) i `ProductionMachine` (prerađuje ih); podaci u `*MachineData` ScriptableObjectima, spawn kroz `MachineFactory`.
- **Recepti** — `CraftingRecipe` SO-ovi; novi recepti otključavaju se napretkom Huba.
- **Logistika** — mreža veza među planetima kao transportna infrastruktura; teleporteri za kretanje.
- **Trošenje i kvarovi** — strojevi se kvare (`MachineBreakdown`), češće na nestabilnim planetima; veze degradiraju → automatizacija nije „postavi i zaboravi".

## Razlika od klasičnog factory žanra

Umjesto pokretnih traka na jednoj mapi, „tvornica" je **raspršena po planetima**, a uska grla su **veze između njih** — logistički problem je graf, ne traka. → [[Koncept igre]]

## Povezano

- [[Žanr]] · [[Survival žanr]] · [[Osnovna petlja igre]]

## Moguća potpitanja

- *„Zašto se strojevi kvare?"* → protuteža automatizaciji: igrač ima razlog vraćati se na planete, mreža traži održavanje.
