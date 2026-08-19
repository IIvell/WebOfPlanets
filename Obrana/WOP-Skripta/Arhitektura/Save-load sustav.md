---
tags: [arhitektura, moj-projekt]
---

# Save-load sustav

Dio [[Arhitektura projekta]]. Datoteka: `Assets/_Project/Scripts/Game/SaveSystem.cs` — statična `partial` klasa, četiri logička bloka: **core / Dto / Capture / Restore**.

- Format: **JSON**, jedan slot
- Put: `Application.persistentDataPath/webofplanets_save.json`

`Application.persistentDataPath` je platformski put za trajne podatke igrača (na Windowsu `%AppData%/../LocalLow/<Company>/<Product>`) — jedina lokacija u koju build smije pisati.

## Što se sprema

Proceduralni planeti · aktivne veze (tip + zdravlje) · strojevi s vezama (collector→storage, teleporter parovi) i njihovi interni bufferi te `Broken` stanje · respawn totemi · hub skladište · inventar · hotbar (s trajnošću) · hub prag · igrač · resursi pojedinačno (item, pozicija, pickup/mining).

## Ključna odluka: load NE reloada scenu

Standardno rješenje je `SceneManager.LoadScene` pa popuni stanje. **Kod mene bi to obrisalo sve sustave stvorene [[Runtime bootstrap pattern|runtime bootstrapom]]** (MainMenuUI, VfxManager, AudioManager…).

Zato load:

1. **sruši proceduralni svijet u mjestu** (uništi planete, veze, strojeve),
2. **ponovno ga izgradi kroz iste kodne puteve kao world-gen.**

Posljedica koju vrijedi istaknuti: `Planet.Start()` opet digne `OnPlanetDiscovered` ([[Event bus (GameEventBus)]]) pa se **vulkanske zone i mobovi spawnaju sami** — save o njima ne zna ništa i ne mora znati.

## Razrješavanje asseta

Objekti se ne serijaliziraju — sprema se **ime asseta**, a `Resolve<T>(name)` ga nađe po **tipu + imenu** među učitanima iz `Resources`. Tipizirano (imena se ponavljaju), keširano (poziva se u petljama, a asseti se u runtimeu ne mijenjaju). Vidi [[ScriptableObject podaci]].

## Svjesna pojednostavljenja (nisu bugovi)

- **Mining progress u tijeku** se ne sprema.
- **Regeneracijski timeri resursa** se ne spremaju → resurs u regeneraciji se vrati vidljiv.
- Hub dekor resursi se ne diraju (drži ih `HubResourceSpawner`).

> Ako me pitaju za to: to je dokumentirana odluka trošak/korist — dobitak bi bio neprimjetan igraču, a shema fajla bi narasla.

## Pravilo pri razvoju

Novo spremljeno polje znači **tri izmjene odjednom**: Dto (shema), Capture (snimanje), Restore (obnova). I: **stari save fajlovi se moraju i dalje učitati** (nedostajuće polje → default vrijednost).

## Moguća potpitanja

- *„Zašto JSON, a ne binarno?"* → čitljivo pri debuggiranju, `JsonUtility` je ugrađen, veličina nije problem. Binarno bi bilo brže i teže za varanje.
- *„Kako biste spriječili varanje?"* → checksum ili enkripcija; za single-player igru nije prioritet.
- *„Više slotova?"* → shema to podržava, samo bi trebao drugi naziv datoteke + UI za odabir.
