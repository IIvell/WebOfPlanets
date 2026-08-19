---
tags: [mreza-veza, moj-projekt]
---

# Degradacija veza

Dio [[Mreža veza|Mreže veza]]. Svaka `PlanetConnection` ima `Health` 0–100; lifespan tiera pretvara se u štetu: **100 / lifespan po sekundi**.

## Tick umjesto framea

Šteta se akumulira (`_pendingDamage`) i primjenjuje na **fiksnom taktu od 0.25 s**, ne svaki frame. Razlog (komentar u kodu): dva upisa u materijal + bus event *po frameu po vezi* rasli su s brojem veza — a srž igre je graditi veze. Ukupni tempo degradacije je identičan jer se šteta zbraja. Jedina per-frame iznimka: flicker boje ispod 20 zdravlja.

## Nestabilni krajevi (GDD 4.2)

[[Vulkanski planet (Volcanic)|Vulkanski]] i [[Plinski planet (Gaseous)|plinski]] planeti su `IsUnstable`. Množitelj degradacije: `1 + 0.5 × brojNestabilnihKrajeva` → jedan kraj = 1.5× brže, oba = 2×. Implementirano **dijeljenjem lifespana**, pa isti mehanizam vrijedi i pri [[Save-load sustav|loadu]].

## Boja kao UI

Gradijent po trećinama zdravlja: zelena → žuta → narančasta → crvena. Ispod 20: **treperenje** (sinus, brže što je zdravlje niže — 4→12) kao vizualni alarm bez ijednog UI elementa.

## Smrt veze

Na 0 zdravlja: `OnConnectionDestroyed` na [[Event bus (GameEventBus)|bus]], totemi se **odvoje od roditelja** (inače bi nestali isti frame s connection objektom) i raspadnu kroz `DisintegrationEffect`, a `ConnectionManager` ponovno upali [[Potencijalne veze (Kruskal)|potencijalne toteme]] za taj par. Eventi usput: `HealthChanged` svaki tick, `Critical` na ≤ 20 (throttle je na potrošaču, npr. NetworkMapUI).

## Save/load

Sprema se par planeta + tip + zdravlje. `RestoreConnection` vrati **puni lifespan pa štetu do spremljenog zdravlja** — degradacija nastavlja istim tempom kao prije spremanja.

## Moguća potpitanja

- *„Zašto `Destroy(_material)` u `OnDestroy`?"* → `Renderer.material` je po-objektna instanca; bez toga svaka istrunula veza trajno ostavi jedan `Material` u memoriji (curenje).
- *„Zašto se šteta ne sprema kao timestamp?"* → zdravlje je jedan float i preživljava promjene tunablea; timestamp bi ovisio o vremenu sesije.
