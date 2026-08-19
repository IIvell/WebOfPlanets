---
tags: [svijet, moj-projekt]
---

# Event-lančani spawn hazarda i mobova

Dio [[Proceduralna generacija|Proceduralne generacije]]. Poveznica generacije i [[Event bus (GameEventBus)|event busa]] — najelegantniji dio arhitekture svijeta.

## Obrazac

`Planet.Start()` diže **`OnPlanetDiscovered`**. Tri sustava se pretplaćuju i reagiraju:

| Sustav | Reakcija | Na kojim planetima |
|---|---|---|
| `ResourceSpawnManager` | resursi po gustoći | svi osim huba ([[Spawnanje resursa]]) |
| `VolcanicHazardSpawner` | orbitirajuće zone štete | Volcanic ([[Vulkanski planet (Volcanic)]]) |
| `EnemyMobSpawner` | 3–5 mobova | po pravilima spawnera |

Svaki spawner drži `_processed` set (planet se obradi jednom) i u vlastitom `Start`-u pričeka frame (`yield return null`) pa **prođe i kroz već postojeće planete** — jer redoslijed `Start`-ova između spawnera i `PlanetCreatora` nije definiran.

## Zašto je ovo dobro — poanta za obranu

`PlanetCreator` **ne zna** da hazardi i mobovi postoje. Da sutra dodam „ruševine drevnih civilizacija", napišem novi spawner koji sluša isti event — **nula izmjena u postojećem kodu** (open/closed princip u praksi).

Druga, veća korist: **load radi „sam od sebe".** [[Save-load sustav]] ponovno stvara planete istim kodom kao world-gen → `Planet.Start` opet plane → hazardi i mobovi se sami spawnaju. Save o njima ne sprema **ništa** i ne mora.

## Detalji hazarda (za potpitanja)

- **`VolcanicHazardOrbit`** — zona kruži po površini rotacijom oko fiksne osi kroz centar (`RotateAround`). Vulkanski planeti su uniformno skalirane sfere → udaljenost od centra je duž kružnice **konstantna** → zona ostaje jednako ukopana **bez raycasta po frameu**. Geometrijsko svojstvo umjesto brute-forcea.
- **`VolcanicHazardZone`** — šteta u tickovima od 1 s; **zajednički statični tick za sve zone** — svaka zona s vlastitim timerom je u preklopu **duplirala štetu** (ispravljen defekt).
- **`GasPlanetAtmosphere`** ([[Plinski planet (Gaseous)]]) — pandan za cijeli planet: šteta bez [[Alati|gas maske]], grace od 3 s pri dolasku, a tick timer se drži interval ispred da skidanje maske ne izazove trenutačni burst štete. Sve tri klase žive konsolidirano (vidi [[Konvencije u kodu]]).

## Moguća potpitanja

- *„Zašto event, a ne da PlanetCreator zove spawnere?"* → direktni pozivi = PlanetCreator ovisi o svakom spawneru; event okreće smjer ovisnosti. Vidi [[Event bus (GameEventBus)]].
- *„Što ako se event digne prije nego se spawner pretplati?"* → zato prolaz kroz postojeće planete u `Start`-u — obrazac „event + catch-up sken".
- *„Spawnaju li se mobovi ponovno na loadu?"* → da, nanovo (nisu spremljeni) — pozicije mobova nisu vrijedne spremanja, za razliku od resursa koji jesu.
