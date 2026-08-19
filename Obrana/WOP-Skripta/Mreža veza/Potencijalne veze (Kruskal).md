---
tags: [mreza-veza, moj-projekt]
---

# Potencijalne veze (Kruskal)

Dio [[Mreža veza|Mreže veza]]. `ConnectionManager.SpawnPotentialMarkers()` — odlučuje **gdje veza uopće smije nastati**.

## Problem

Svi parovi planeta u dometu (`maxConnectionRange`, 5000 jedinica) čine gotovo **potpun graf** — totem za svaki par bio bi kaos od markera na svakom planetu.

## Rješenje: razapinjuće stablo + višak

1. Skupe se svi parovi u dometu i **sortiraju po udaljenosti**.
2. **Kruskalov algoritam** (union-find s path-halvingom) gradi razapinjuće stablo najkraćih bridova → garantira da je **svaki planet dostižan iz huba** lancem totema. Preduvjet — da je graf parova u dometu uopće povezan — osigurava [[Generiranje svijeta (PlanetCreator)|PlanetCreator]] lančanim spawnom (svaki novi planet sidri se na već spawnani u dometu).
3. Nakon stabla se dodaju **dodatne kratke veze**, ali samo dok su *oba* kraja ispod `maxPotentialPerPlanet` (3). Limit je mekan: stablo ga ignorira, jer je dostižnost važnija od urednosti.

## Exclusion zone

Oko spawna igrača, škrinje i računala (radijus 20) totemi se ne spawnaju. Dvije suptilnosti iz komentara u kodu:

- Par nastaje **samo ako obje strane dobiju totem** — jednostrani totem pokazivao bi na vezu koja se s druge strane ne vidi.
- Par odbijen zbog zone **ne smije potrošiti mjesto u stablu** — komponente ostaju razdvojene i Kruskal ih spoji prvim sljedećim (duljim) kandidatom.

## Totem

Radijalno uspravan **kao strojevi** (normala trokuta na low-poly planetu zna lagati), prizemljen kroz [[Prizemljenje na sferu (SurfacePlacement)|SurfacePlacement]], solid collider po geometriji. `FindClearSurfacePoint` bježi od zauzetog tla (resursi se spawnaju isti frame!) s do 8 bočnih pokušaja; ignorira igrača i mobove. Interakcija: `PotentialConnectionInteractable` → otvara izbor tiera ([[Teleportacija i putovanje|ConnectionChoiceUI]]).

Kad prava veza nastane, potencijalni totemi se samo **ugase** (poza se čuva i ponovno koristi — vidi [[Degradacija veza]]); kad veza istrune, opet se upale.

## Moguća potpitanja

- *„Zašto Kruskal, a ne Prim?"* → svejedno je za rezultat (MST); Kruskal je prirodan jer već imam listu bridova sortiranu po udaljenosti i union-find je par linija.
- *„Što ako je limit 0?"* → samo stablo — mreža je i dalje potpuno prohodna, samo bez alternativnih ruta.
