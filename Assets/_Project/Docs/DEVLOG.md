# Web of Planets — Devlog

> ⚠️ **POVIJESNI DOKUMENT (napomena 15.7.2026.)** — opisuje stanje ranog prototipa iz travnja.
> Dio dokumentiranog API-ja više ne postoji (`EnterNewGravityField`, `isTouchingPlanetSurface`,
> `PlanetCameraController`...). Za trenutno stanje projekta vidi **AUDIT-2026-07-14.md** i **PLAN.md**.

## Sesija 1 — 2026-04-10

---

### Postavljanje projekta
- Unity projekt inicijaliziran
- Engine: Unity 6 (koristi `rb.linearVelocity` API)
- Input System: Unity novi Input System (`PlayerInputActions.inputactions`)
  - Action map: `PlayerActionMap`
  - Akcije: `Movement` (Vector2, WASD), `Jump` (Button, Space)

---

### Kod koji je napravljen

#### `Assets/System/PlayerInputActions.inputactions` + `PlayerInputActions.cs`
Auto-generirani wrapper za Unity Input System.

---

#### `Assets/Scripts/Events/EventTypes.cs`
Sve definicije tipova podataka za event sistem:
- **Enumi:** `ResourceType`, `PlanetType`, `ConnectionType`, `ArtifactType`, `MachineState`, `HubLevel`, `MilestoneType`
- **Structovi (event data):** `ResourceCollectedEvent`, `ResourceTransportedEvent`, `ConnectionHealthChangedEvent`, `ConnectionEvent`, `ArtifactEvent`, `MachineEvent`, `TransportRouteEvent`

---

#### `Assets/Scripts/Events/GameEventBus.cs`
Statički event bus — centralno mjesto za sve igrine evente.

**Eventi po kategoriji:**

| Kategorija | Event |
|---|---|
| Player | `OnPlayerLandedOnPlanet`, `OnPlayerLeftPlanet`, `OnPlayerTeleported` |
| Resources | `OnResourceCollected`, `OnResourceTransported`, `OnStorageFull`, `OnTransportRouteCreated`, `OnTransportRouteRemoved` |
| Network | `OnConnectionCreated`, `OnConnectionDestroyed`, `OnConnectionHealthChanged`, `OnConnectionCritical`, `OnAncientConnectionDiscovered`, `OnAncientConnectionActivated` |
| Planets | `OnPlanetDiscovered`, `OnSecondaryHubCreated` |
| Hub | `OnHubUpgraded`, `OnBlueprintUnlocked` |
| Artifacts | `OnArtifactFound`, `OnArtifactActivated`, `OnPairedArtifactCombined` |
| Machines | `OnMachinePlaced`, `OnMachineBroken`, `OnMachineRepaired` |
| Game State | `OnMilestoneReached`, `OnStoryFragmentUnlocked` |

**Korištenje:**
```csharp
// Pretplata
GameEventBus.OnResourceCollected += HandleResource;

// Okidanje
GameEventBus.Raise(new ResourceCollectedEvent { Type = ResourceType.Ore, Amount = 5, Planet = planetTransform });
```

---

#### `Assets/Scripts/Planet/Planet.cs`
Komponenta za planet objekt. Postavlja `Rigidbody.isKinematic = true` kako planet ne bi padao zbog Unity gravitacije.

**Setup u sceni:**
- Dodati na Planet GameObject koji ima Rigidbody
- Planet treba Collider (non-trigger) za fizičko hodanje

---

#### `Assets/Scripts/Player/PlayerController.cs`
Kretanje igrača po površini planeta — Super Mario Galaxy stil (sferična gravitacija).

**Kako radi:**
- `gravityUp` = smjer od centra planeta prema igraču (normala površine)
- Vlastita gravitacija (`rb.AddForce`) prema centru planeta, Unity gravitacija isključena
- Kretanje (WASD) relativno na kameru, projicirano na površinu planeta
- Rotacija tijela (`rb.MoveRotation`) prati `gravityUp` — igrač se "lijepi" za površinu
- Visual (mesh) se rotira prema smjeru kretanja
- Skok: impuls u smjeru `gravityUp`
- Detekcija tla: `OnTriggerEnter/Exit` (planet treba trigger collider za ovo)

**Inspector polja:**

| Polje | Opis | Default |
|---|---|---|
| `speed` | Brzina kretanja | 8 |
| `acceleration` | Brzina ubrzavanja/zaustavljanja | 60 |
| `jumpForce` | Sila skoka | 10 |
| `gravity` | Sila vlastite gravitacije | 25 |
| `alignSpeed` | Brzina poravnavanja s površinom | 10 |
| `visualTurnSpeed` | Brzina okretanja mesha | 15 |
| `currentPlanet` | Transform planeta | — |
| `playerVisual` | Transform mesha igrača | — |
| `cameraTransform` | Transform Main Camera | — |

**Rigidbody postavke (postavljene u kodu):**
- `useGravity = false`
- `freezeRotation = true`
- `collisionDetectionMode = Continuous`
- `interpolation = Interpolate`

**Javni API:**
- `isTouchingPlanetSurface` — bool, je li igrač na površini
- `EnterNewGravityField()` — poziva se pri prelasku na drugi planet

---

#### `Assets/Scripts/Player/PlanetCameraController.cs`
Kamera koja prati igrača oko planeta — bird's eye view.

**Kako radi:**
- Pozicionira se iznad igrača duž `gravityUp` vektora
- `LateUpdate` → gleda prema igraču, `up` = `gravityUp`
- Glatko prati igrača (`Vector3.Lerp` + `Quaternion.Slerp`)
- Rotacija kamere isključena (bez kontrole mišem)

**Inspector polja:**

| Polje | Opis | Default |
|---|---|---|
| `player` | Transform igrača | — |
| `planetCenter` | Transform planeta | — |
| `distance` | Horizontalna udaljenost | 5 |
| `heightOffset` | Visina iznad igrača | 20 |
| `followSmoothing` | Glatkoća praćenja | 8 |
| `rotationSmoothing` | Glatkoća rotacije | 10 |

**Setup u sceni:** komponenta se stavlja na **Main Camera** objekt.

---

### Struktura scene (trenutna)

```
Scene
├── Planet          ← Planet.cs, Rigidbody (kinematic), Collider, Trigger Collider
├── Player          ← PlayerController.cs, Rigidbody, Collider
│   └── PlayerVisual    ← mesh igrača
└── Main Camera     ← PlanetCameraController.cs
```

---

### Poznati problemi / TODO

- [ ] Player se blago glitcha na površini planeta — treba bolja detekcija tla
- [ ] Kamera mjestimično vibrira kad player vibrira
- [ ] `GameEventBus` i `PlanetCameraController` daju lažne IDE greške u VS Code (OmniSharp) — kod se ispravno kompajlira u Unityju
- [ ] Kretanje kamere mišem nije implementirano (namjerno isključeno)
- [ ] Nema još nijednog gameplay sustava (resursi, veze, artefakti)
