---
tags: [tehnologije]
---

# Legacy Input Manager

Stari Unityjev sustav unosa — statička klasa `UnityEngine.Input` (`Input.GetKey`, `Input.GetAxis`) + virtualne osi definirane u Project Settings.

## Zašto NE za moj projekt

- Naslijeđena tehnologija — ne razvija se; paralela s [[Built-in pipeline]] kod render pipelinea.
- Identifikacija **stringovima** (`"Horizontal"`, `"Jump"`) — greška se otkrije tek u runtimeu.
- Tipke tvrdo vezane u kodu i postavkama; rebinding i podrška za više uređaja zahtijevaju ručni rad.
- Samo polling model — svaka skripta svaki frame ispituje stanje.

## ⚠ Gotcha za pitanja

U projektu je uključen **samo novi sustav** (`activeInputHandler: 1`) → poziv `Input.GetKey` baca iznimku u runtimeu. Vidi [[Input System (novi)]].
