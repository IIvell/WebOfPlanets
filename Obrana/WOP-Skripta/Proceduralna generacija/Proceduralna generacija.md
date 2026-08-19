---
tags: [moc, svijet, moj-projekt]
---

# Proceduralna generacija

Hub bilješka za sve o **generiranju svijeta u runtimeu**. Cijeli svijet (osim Hub planeta u sceni) nastaje kodom pri pokretanju — scena je prazna ljuska, vidi [[Runtime bootstrap pattern]].

## Lanac generiranja

```
PlanetCreator.Start()
  └─ 30 planeta, lančani spawn      → [[Generiranje svijeta (PlanetCreator)]]
       └─ svaki planet = primitivna sfera + komponente
                                     → [[Generiranje planeta]]
            └─ Planet.Start() diže OnPlanetDiscovered
                 ├─ ResourceSpawnManager → resursi   → [[Spawnanje resursa]]
                 ├─ VolcanicHazardSpawner → hazardi  ─┐
                 └─ EnemyMobSpawner → mobovi         ─┴→ [[Event-lančani spawn hazarda i mobova]]
```

Svako postavljanje objekta na kuglu ide kroz [[Prizemljenje na sferu (SurfacePlacement)]].

## Teme

- [[Generiranje svijeta (PlanetCreator)]] — lančani spawn i garancija povezanosti
- [[Generiranje planeta]] — sfera, collideri, materijali, nasumičnost
- [[Spawnanje resursa]] — gustoća po tipu planeta iz ScriptableObjecta
- [[Prizemljenje na sferu (SurfacePlacement)]] — zajednička matematika površine
- [[Event-lančani spawn hazarda i mobova]] — zašto load radi „sam od sebe"

## Rečenica za obranu

> „Svijet od 30 planeta generira se pri pokretanju: planeti se spawnaju lančano tako da je graf mogućih veza povezan po konstrukciji, svaki planet je primitivna sfera s nasumičnim tipom, veličinom i gravitacijom, a resursi, hazardi i mobovi se ne spawnaju direktno nego reagiraju na event `OnPlanetDiscovered` — pa isti kod radi i za novu igru i za učitavanje savea."

## Ključne brojke (šalabahter)

| Parametar | Vrijednost |
|---|---|
| Planeta na startu | 30 |
| Skala planeta | 35–100 |
| Gravitacija | 10–40 |
| Udaljenost spawna | 1500–5000 (stisnuto pod domet veze × 0.99) |
| Min. separacija | 200 + skala planeta |
| Pokušaja smještanja | 30, pa fallback |
| Tipovi planeta | 5: Mining, Organic, Ice, Volcanic, Gaseous |

## Moguća potpitanja

- *„Je li generacija deterministička (seed)?"* → nije, čisti `Random`; [[Save-load sustav]] zato sprema svaki planet i resurs pojedinačno umjesto seeda.
- *„Zašto ne proceduralni teren/mesh?"* → planete su kugle s teksturama iz asseta (proceduralne teksture izbačene u srpnju 2026.); fokus rada je na sustavima, ne na terenu.
