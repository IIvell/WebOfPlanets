---
tags: [fizika, moj-projekt]
---

# Surface lock

Dio [[Gravitacija i kretanje|Gravitacije i kretanja]]. Metoda `EnforceSurfaceLock` u [[Kretanje igrača (PlayerController)|PlayerControlleru]], zadnji korak svakog `FixedUpdate`.

## Problem

Guranjem u box collider resursa/stroja/totema PhysX **depenetracija** vuče kapsulu preko ruba prema gore, a `Move` čuva vertikalnu brzinu — bez locka igrač se mogao **popeti na objekte**.

## Rješenje

Dno kapsule smije biti najviše `surfaceSkin` (0,15) iznad površine planeta. Svaki fizički korak:

1. Raycast prema tlu koji **prihvaća samo planetov vlastiti collider** (`SurfacePlacement.TryRaycastSurface` — objekti na površini nisu tlo; vidi [[Prizemljenje na sferu (SurfacePlacement)]]). Zraka kreće **ispod kapsule, ne ispod pivota** — capsule.center je nakon poravnanja ~0,8 bočno od pivota, pa bi kriva zraka na nagibu mjerila tuđu visinu.
2. Ako je igrač prizemljen i dno je iznad skina: **odreže se outward komponenta brzine** (po stvarnoj radijali, ne `transform.up` koji kasni za Attractor slerpom) i **vrati višak visine**, najviše `maxSnapPerStep` (0,2) po koraku.

## Gating po `_grounded` — čuvanje legitimnih letova

Teleport/respawn/load spuštaju igrača ~1 m iznad tla uz namjerni meki pad. Zato:

- lock djeluje samo dok je `_grounded`; tlo se "hvata" kad visina prvi put padne ispod skina,
- **skok pozicije > 1 m u jednom koraku ruši `_grounded`** (nijedno legitimno kretanje to ne radi) → nakon teleporta/loada tlo se ponovno hvata iz zraka.

## Zašto NEMA otpuštanja locka po visini (fix, srpanj 2026)

Staro pravilo "iznad `ungroundHeight` pusti lock" imalo je fatalan slučaj: depenetracija na visokom collideru digne kapsulu iznad praga **u jednom koraku** → lock se otpusti → stojeći na resursu visina više nikad ne padne ispod skina → lock se nikad ne uhvati → igrač **trajno clippan iznad površine**. Depenetracija ne može srušiti `_grounded` (PhysX `maxDepenetrationVelocity` 10 m/s = 0,2 m po koraku < 1 m), pa je jedini izlaz iz locka teleport/load. Silazak s litice sad rješava lock (spust do 0,2 po koraku) umjesto slobodnog pada — na kuglastim planetima neprimjetno.

## Moguća potpitanja

- *„Zašto raycast, a ne udaljenost od centra?"* → planet je poligonalni mesh, ne matematička kugla; analitika je do ~1,3 % R kriva ([[Generiranje planeta]]).
- *„Zašto direktno pisanje `rig.position`?"* → resetira interpolaciju, ali samo u koracima gdje je prekršaj stvarno nastao; običan hod ostaje ispod skina i ne dira poziciju.
