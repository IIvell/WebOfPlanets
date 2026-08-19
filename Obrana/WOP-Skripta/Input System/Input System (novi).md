---
tags: [tehnologije]
---

# Input System (novi)

Unityjev noviji paket za unos (`com.unity.inputsystem`, u projektu **1.18.0**) — zamjena za [[Legacy Input Manager]].

## Ključna ideja: akcije umjesto tipki

Kod ne pita „je li pritisnut W" nego sluša **akciju** („Move"). Akcije žive u `.inputactions` assetu i vežu se na tipkovnicu, gamepad, touch... bez promjene gameplay koda.

## Zašto novi, a ne stari

- **[[Legacy Input Manager]] je naslijeđena tehnologija** — ne razvija se; novi je standard za Unity 6. Isti argument kao [[URP]] vs [[Built-in pipeline]].
- **Type-safe generirani C# wrapper** umjesto magičnih stringova (`Input.GetAxis("Horizontal")`) — tipfeler se vidi pri kompajliranju, ne u runtimeu.
- **Akcije odvojene od uređaja** — podrška za više uređaja i runtime rebinding bez diranja koda.
- **Event-driven** umjesto obveznog pollinga svakog framea (polling i dalje postoji gdje je jednostavniji — vidi [[Input System u mom projektu]]).

## ⚠ Gotcha za pitanja

Projekt ima `activeInputHandler: 1` = uključen **samo novi sustav**. Stari `Input.GetKey` se kompajlira, ali u runtimeu baca `InvalidOperationException`.

## Odgovor u jednoj rečenici

> „Koristim novi Input System jer je standard za Unity 6 — akcije su odvojene od konkretnih tipki i uređaja, generirani wrapper je type-safe umjesto stringova, a stari Input Manager je naslijeđena tehnologija koja se više ne razvija."
