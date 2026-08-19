---
tags: [arhitektura, moj-projekt, vazno]
---

# Runtime bootstrap pattern

Dio [[Arhitektura projekta]]. **Ovo je dominantni arhitektonski obrazac projekta** — ako me pitaju „što je specifično u vašoj arhitekturi", odgovor je ovo.

## Ideja

Umjesto da sustave ručno slažem u sceni (GameObject → dodaj komponentu → namjesti reference u Inspectoru), **sustavi se sami stvaraju i konfiguriraju iz koda kad igra krene**:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
static void Bootstrap()
{
    var go = new GameObject("AudioManager");
    go.AddComponent<AudioManager>();
    Object.DontDestroyOnLoad(go);
}
```

`[RuntimeInitializeOnLoadMethod]` je Unityjev atribut koji pozove statičku metodu automatski pri pokretanju igre — bez ikakvog objekta u sceni.

## Opseg u projektu

**18 skripti** koristi taj atribut. Sustavi koji se sami bootstrapaju: `AudioManager`, `VfxManager`, `MainMenuUI`, `VictoryUI`, `SpaceSun`, `SpaceSkybox`, `EnemyMobSpawner`, `GasMaskVisual`, `UiScale`, `Interactor`…

## Zašto — 3 razloga koja navodim na obrani

1. **Scena ostaje minimalna.** `SampleScene.unity` je YAML od par tisuća linija umjesto par desetaka tisuća.
2. **Nema mergeanja scene.** `.unity` fajl je praktički nemoguće spojiti u gitu; kod se spaja normalno.
3. **Reference se ne mogu izgubiti.** Reference namještene u Inspectoru pucaju pri refaktoringu (postanu `Missing`); reference iz koda ili kompajliraju ili ne.

Uz to: svijet je ionako **proceduralan** (30 planeta iz `PlanetCreator`), pa u sceni i nema što stajati unaprijed.

## Faze `RuntimeInitializeLoadType`

| Faza | Kad se izvodi | Gdje je koristim |
|---|---|---|
| `SubsystemRegistration` | najranije, prije učitavanja scene | **reset statičkih polja** |
| `AfterSceneLoad` | nakon što je scena učitana | stvaranje sustava |

`SubsystemRegistration` je nužan zbog **Enter Play Mode Options** — ako je domain reload isključen, statička polja **zadrže vrijednost iz prošlog pokretanja** i igra se ponaša čudno u drugom play modeu. Primjer: `SaveSystem.ResolveCache.Clear()`.

## Cijena obrasca (moram priznati trade-off)

- Sustavi nisu vidljivi u sceni prije pokretanja — teže je „vidjeti" što postoji.
- Postavke se mijenjaju u kodu, ne u Inspectoru → dizajner bez pristupa kodu ne može ništa štimati.
- Redoslijed inicijalizacije između više bootstrapa nije garantiran.

## Veza sa save/loadom

Zato [[Save-load sustav]] **ne smije reloadati scenu** — reload bi obrisao sve runtime-stvorene sustave. Svijet se ruši i gradi u mjestu.

## Moguća potpitanja

- *„Kako testirate nešto što ne postoji u sceni?"* → play mode + konzola; audit alati u `Assets/Editor/`.
- *„Zašto ne bootstrap scena?"* → to je uobičajena alternativa (prazna scena koja učita glavnu); ovdje bi bila drugi entry point za jednu igru s jednom scenom.
- *„Što je domain reload?"* → ponovno učitavanje .NET domene pri ulasku u play mode; resetira statiku, ali traje. Isključivanje ubrzava iteraciju uz cijenu ručnog resetiranja statike.
