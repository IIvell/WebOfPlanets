---
tags: [svijet, moj-projekt]
---

# Spawnanje resursa

Dio [[Proceduralna generacija|Proceduralne generacije]]. Datoteka: `Planet/ResourceSpawnManager.cs` (+ `ResourcePlacement` u istoj datoteci).

## Podaci: PlanetResourceSettings

ScriptableObject ([[ScriptableObject podaci]]) — po tipu planeta lista unosa:

```
ResourceEntry:  item · minDensity · maxDensity · pickupChance
```

**Broj instanci = gustoća × radijus planeta** (`Random.Range(min, max) * radius`, min. 1). Veći planet → više resursa, prirodno skaliranje bez posebnog koda. Balansiranje = izmjena `.asset` polja, nula kompajliranja.

`pickupChance` bira po instanci: **pickup** verzija (odmah u ruke, bez timera) ili **mining** verzija (ruda se kopa). Vidi [[Resursi]].

## Tok spawna

1. Pretplata na `OnPlanetDiscovered` ([[Event-lančani spawn hazarda i mobova]]); hub se preskače.
2. Nasumičan smjer `Random.onUnitSphere` → točka na površini preko [[Prizemljenje na sferu (SurfacePlacement)|SurfacePlacementa]].
3. **Bježanje od totema veza**: ako je točka blizu markera veze (`OverlapSphere`, r=4), baca se novi smjer — do 8 pokušaja, pa spawna svejedno. Namjerno se **ne** provjeravaju drugi resursi, da se ne iskrivi gustoća. (Razlog: redoslijed Start korutina nije definiran, pa se resurs znao stvoriti *unutar* totema.)
4. Rotacija: `FromToRotation(item.surfaceUpAxis, hitNormal)` — model „ustane" po normali terena.
5. `ResourcePlacement.Spawn` — zajednički završni korak.

## ResourcePlacement — jedan put za tri pozivatelja

Svjež spawn, hub dekor (`HubResourceSpawner`) i **povratak iz save datoteke** idu **istim kodom**: instanciranje, skala, bezuvjetno prizemljenje po stvarnoj geometriji, brisanje Rigidbodyja, box collider po geometriji ako prefab nema svoj, `ItemInteractable.Init`.

Izbor *točke* ostaje na pozivateljima jer se namjerno razlikuje: hub izbjegava zonu baze (i preskače ako ne uspije), regularni spawn bježi od totema (i spawna svejedno).

> Ista filozofija kao [[Strojevi i MachineFactory|MachineFactory]]: jedan put stvaranja → objekt iz savea identičan svježem.

## Veza sa save/loadom

[[Save-load sustav]] označi load-ane planete obrađenima (`MarkProcessed`) **prije** njihovog `Planet.Start`-a → svježi spawn se preskače, spremljeni raspored se vraća kroz `SpawnSavedResource`. Regeneracijski timeri se ne spremaju (svjesno pojednostavljenje).

## Moguća potpitanja

- *„Zašto gustoća × radijus, a ne fiksan broj?"* → fiksan broj bi mali planet zatrpao, a veliki ostavio praznim; površina raste s radijusom.
- *„Zašto se briše Rigidbody resursa?"* → resurs je statičan na površini; rigidbody bi ga fizika mogla otkotrljati niz kuglu.
- *„Zašto box collider po geometriji, a ne default?"* → 'Ice' model (skinned mesh bez collidera) s default 1×1×1 kockom na pivotu imao je collider pomaknut od vizuala — nije se dao pokupiti gledajući ga.
