---
tags: [svijet, moj-projekt]
---

# Generiranje svijeta (PlanetCreator)

Dio [[Proceduralna generacija|Proceduralne generacije]]. Datoteka: `Planet/PlanetCreator.cs` (~380 linija).

## Lančani spawn — glavna ideja

Svaki novi planet se **sidri na nasumično odabran već spawnani planet** (ili hub) i mora pasti **unutar dometa veze** od sidra.

**Zašto:** graf potencijalnih veza je time **povezan po konstrukciji** — svaki planet je dostižan iz huba lancem totema. Bez toga igra može biti neprolazna.

**Dokaz da je trebalo:** raniji pristup (spawn uvijek-od-huba, raspon širi od dometa veze) ostavljao je **prosječno ~12 od 30 planeta trajno nedostižno**. Ovo je najbolji primjer u projektu za „problem → mjerenje → redizajn".

Detalji garancije:

- Gornja granica udaljenosti: `min(maxSpawnDistance, MaxConnectionRange × 0.99)` — 1 % margine jer `ConnectionManager` par odbacuje strogim `>` na točnoj granici.
- Par sidren na hub mora imati **čistu hub stranu**: ako hub točka veze padne u exclusion zonu, par se uopće ne spawna (pravilo „oba ili nijedan").
- Domet se čita **runtime lookupom** sa serijaliziranih polja `ConnectionManagera` (scene override: 2000 umjesto default 5000) — ne smije se preseliti u config objekt bez editiranja scene.

## Traženje slobodne pozicije

`FindOpenPosition`: do **30 pokušaja** nasumične točke (`Random.onUnitSphere × udaljenost`), odbaci ako je preblizu postojećem planetu (`200 + skala`). Ako svih 30 padne — **fallback**: postavi na maksimalnu udaljenost svejedno, ali uvjet čiste hub strane i tada pokušava ispoštovati.

> Poanta za obranu: **separacija je estetika, povezanost je garancija** — fallback smije žrtvovati prvo, nikad drugo.

## Nasumični parametri po planetu

Skala 35–100 · gravitacija 10–40 · tip uniformno iz 5 tipova ([[Tipovi planeta]]) · imena `Planet_00` … `Planet_29`.

Debug spawn (tipka iz `GameKeys`) daje imena `GeneratedPlanet_NN` — **jedinstvena**, jer [[Save-load sustav]] referencira planete po imenu; dva ista imena bi na loadu tiho zakačila strojeve i igrača na krivi planet.

## Veza sa save/loadom

`SpawnPlanetFromSave(name, pos, scale, gravity, type)` — isti kod stvaranja, samo s točnim vrijednostima umjesto nasumičnih. Load ide **istim putem kao world-gen**, vidi [[Event-lančani spawn hazarda i mobova]].

## Moguća potpitanja

- *„Kako znate da je graf povezan?"* → indukcijom: hub je povezan; svaki novi planet je unutar dometa od nekog već povezanog → i on je povezan.
- *„Zašto nasumično sidro, a ne uvijek zadnji planet?"* → zadnji bi dao jedan dugi lanac („zmiju"); nasumično sidro daje razgranato stablo, prirodniji raspored.
- *„Što ako igrač uništi vezu u sredini lanca?"* → [[Progresija kroz Hub|ConnectionManager]] uvijek gradi spanning tree, pa mreža ostaje povezana.
