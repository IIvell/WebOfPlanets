---
tags: [arhitektura, moj-projekt, teorija]
---

# Assembly-CSharp (bez asmdefova)

Dio [[Arhitektura projekta]].

## Što je assembly u Unityju

Unity kompajlira C# u **.dll assemblyje**. Bez ikakve konfiguracije nastaju dva zadana:

- **`Assembly-CSharp`** — sve u `Assets/` osim `Editor` mapa → ide u build igre.
- **`Assembly-CSharp-Editor`** — sve u mapi `Editor/` → **ne ide u build**, postoji samo u Editoru.

`.asmdef` (Assembly Definition) je datoteka kojom se dio projekta izdvoji u vlastiti assembly i eksplicitno navede o kojima ovisi.

## Kod mene

**Nema nijednog `.asmdef`.** Sve iz `Assets/_Project/Scripts/` je `Assembly-CSharp`, `Assets/Editor/` je `Assembly-CSharp-Editor`.

**Posljedice:**

- Bilo koja runtime skripta smije referencirati bilo koju drugu — nema granice koju bi se moglo prekršiti.
- Editor kod vidi runtime kod, **ali ne obrnuto**.
- Nema test assemblyja → **nema automatiziranih testova**; verifikacija je play mode + konzola + `Assets/Editor/*Audit*` alati.

## Zašto nisam dodao asmdefove

Trade-off je **brzina kompajliranja vs. sloboda referenciranja**:

| Bez asmdefova (moj slučaj) | S asmdefovima |
|---|---|
| Svaka izmjena rekompajlira sve | Rekompajlira se samo dirnuti assembly + ovisni |
| Nema arhitektonske discipline | Ovisnosti su eksplicitne i provjerene |
| Nula konfiguracije | Treba održavati graf ovisnosti |

Na projektu ove veličine (~13 600 linija) rekompajl traje par sekundi pa dobitak ne bi opravdao trošak. **Na timskom projektu bih ih dodao.**

> ⚠️ Dodavanje ijednog `.asmdef` pod `Scripts/` **razbilo bi** trenutne slobodne cross-folder reference.

## Gotcha koji moram znati

Stray `using UnityEditor;` u runtime skripti **kompajlira se u Editoru, ali ruši player build** — jer `UnityEditor.dll` ne postoji u buildu. Zato se svaka takva upotreba omata u:

```csharp
#if UNITY_EDITOR
    // editor-only kod
#endif
```

Primjer u projektu: `Planet/SurfaceAudit.cs`.

## Moguća potpitanja

- *„Koliko assemblyja ima vaš build?"* → jedan moj (`Assembly-CSharp`) + paketni assemblyji (URP, [[Input System (novi)]], TMP…).
- *„Zašto Editor mapa nije u `_Project`?"* → mapa se **mora** zvati točno `Editor` da Unity prepozna posebno ponašanje.
