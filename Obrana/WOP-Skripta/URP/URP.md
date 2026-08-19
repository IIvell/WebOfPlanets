---
tags: [tehnologije]
---

# URP (Universal Render Pipeline)

Unityjeva standardna implementacija [[Render pipeline|Scriptable Render Pipelinea]]. Verzija u projektu: **17.3.0** (Unity 6).

## Zašto sam odabrao URP

- **[[Built-in pipeline]] je naslijeđena tehnologija** — Unity ga aktivno ne razvija; URP je zadani i preporučeni pipeline za nove Unity 6 projekte.
- **Performantnost** — optimizirani forward renderer, radi dobro i na slabijim računalima; odgovara samostalnoj PC igri.
- **Konfigurabilnost kroz assete** — postavke kvalitete žive u pipeline assetima, mijenjaju se bez diranja koda → vidi [[URP u mom projektu]].
- **Budućnost** — URP shaderi i materijali ostaju kompatibilni s daljnjim razvojem Unityja.

[[HDRP]] nije bio opcija: namijenjen high-end vizualima, nepotrebno za ovaj opseg.

## Povezano

- [[URP u mom projektu]] — konkretna konfiguracija (PC/Mobile asseti, Lit shader)
- [[Linear color space]] — standard uz URP na PC-u
