---
tags: [tehnologije, moj-projekt]
---

# Input System u mom projektu

Kako je [[Input System (novi)]] konkretno postavljen u Web of Planets — **dva sloja**:

## 1. Input Actions — kretanje

- `PlayerInputActions.inputactions` pokriva samo **Movement / Jump / MouseLook**.
- Iz njega je generiran `PlayerInputActions.cs` (`Scripts/Player/Input/`) — **generirani kod, ne uređuje se ručno**. `PlayerController` ga instancira i čita akcije.

## 2. GameKeys — sve ostale tipke

- `Game/GameKeys.cs` — sve tipke izvan asseta (E, I, Q, X, P, R, ESC...) na **jednom mjestu**, zajedno s prikaznim imenima za CONTROLS ekran.
- Prije su tipke bile hardkodirane u **14+ skripti**, a imena su TREĆI put živjela u CONTROLS tekstu — tri ručno sinkronizirana izvora istine → konsolidirano u jedan.
- Null-safe: `Keyboard.current` je `null` bez tipkovnice, pa `WasPressed`/`IsPressed` to provjeravaju.

## Moguća potpitanja

- *„Zašto sve akcije nisu u .inputactions assetu?"* → puna migracija je **svjesno odgođena** (dira asset i regeneraciju wrappera; PLAN-KOD §3) — GameKeys je već jedan izvor istine za one-off tipke.
- *„Što ako netko pozove stari Input.GetKey?"* → iznimka u runtimeu, vidi gotchu u [[Legacy Input Manager]].
- *„Kako igra zna koje tipke prikazati u CONTROLS ekranu?"* → iz istih `GameKeys` konstanti (`InteractName` itd.) — ne može se raz-sinkronizirati.
