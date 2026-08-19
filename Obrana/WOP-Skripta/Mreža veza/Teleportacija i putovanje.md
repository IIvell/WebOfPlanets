---
tags: [mreza-veza, moj-projekt]
---

# Teleportacija i putovanje

Dio [[Mreža veza|Mreže veza]]. Veze nisu samo vizual — one su **prijevozni sustav**. Dva režima:

| Režim | Ulaz | Cijena |
|---|---|---|
| Putovanje **po vezi** | totem aktivne veze (`ConnectionInteractable`) | besplatno |
| Teleport **bez veze** | potencijalni totem → opcija u `ConnectionChoiceUI` | resursi, raste s udaljenošću |

## Po vezi (besplatno)

Svaki kraj veze ima totem s `ConnectionInteractable`; interakcija zove `PlanetCreator.TeleportToPlanet` s **totemom druge strane kao odredištem** (`destinationMarker`) — igrač osvane točno uz totem, uspravan po njegovom radijalnom "up". To je nagrada za izgrađenu vezu: dok veza živi, putovanje je besplatno.

## Bez veze (skupo)

Na potencijalnom totemu `ConnectionChoiceUI` uz tri tiera nudi i jednokratni teleport. Cijena po GDD-u raste s udaljenošću: **množitelj = 1 + floor(d / 2000)** na osnovnu cijenu (`teleportCostDistanceStep`; 0 = fiksna cijena). Trade-off: jednokratni skok ili trajna (ali propadajuća) veza.

Oba režima prolaze kroz `GameManager.TestingMode` (besplatno u testingu). Teleport usput prebacuje aktivni [[Sferna gravitacija (Attractor)|Attractor]] — gasi se stari planet, pali novi.

## Moguća potpitanja

- *„Zašto je putovanje po vezi besplatno?"* → cijena je već plaćena izgradnjom i plaća se dalje kroz [[Degradacija veza|degradaciju]] — veza je investicija koja se troši.
- *„Što ako veza istrune dok sam na drugom planetu?"* → ništa strašno: potencijalni totemi se vrate, pa se veza obnovi ili se plati teleport bez veze.
