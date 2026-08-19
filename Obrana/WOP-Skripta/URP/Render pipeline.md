---
tags: [tehnologije]
---

# Render pipeline

Niz koraka kojima engine za svaki frame pretvori 3D scenu u gotovu 2D sliku na ekranu:

1. **Culling** — odbacivanje objekata koje kamera ne vidi (izvan frustuma, zaklonjeni).
2. **Rendering** — crtanje vidljive geometrije s materijalima, teksturama i svjetlima.
3. **Post-processing** — efekti na gotovoj slici (bloom, tonemapping, anti-aliasing...).

## Opcije u Unityju

| | [[Built-in pipeline\|Built-in]] | [[URP]] | [[HDRP]] |
|---|---|---|---|
| Tip | fiksni, zatvoren | Scriptable RP | Scriptable RP |
| Konfiguracija | ograničena | kroz assete i C# | kroz assete i C# |
| Ciljani hardver | sve (legacy) | širok raspon | samo jak hardver |
| Status | ne razvija se | standard za Unity 6 | high-end vizuali |

**Scriptable Render Pipeline (SRP)** = novija arhitektura gdje je pipeline definiran C# kodom i konfigurira se kroz assete. [[URP]] i [[HDRP]] su Unityjeve dvije gotove implementacije.

## Odgovor u dvije rečenice

> „Render pipeline je proces kojim se scena pretvara u sliku — culling, crtanje geometrije, post-processing. Koristim [[URP]] jer je to moderni standard za Unity 6: performantan je i na slabijim računalima, konfigurira se kroz assete, dok je [[Built-in pipeline|Built-in]] naslijeđena tehnologija koja se više ne razvija."

Vidi: [[URP u mom projektu]]
