---
tags: [tehnologije]
---

# Built-in pipeline

Stari, fiksni [[Render pipeline]] Unityja — proces renderiranja je zatvoren i ne može se mijenjati, konfiguracija je ograničena.

- Naslijeđena tehnologija: Unity ga više aktivno ne razvija, novi projekti počinju s [[URP]]-om.
- Shaderi mu nisu kompatibilni sa SRP-ovima.

## ⚠ Gotcha za pitanja

Built-in shader u [[URP]] projektu renderira se **ružičasto** — pipeline ga ne zna izvršiti. Zato se svi shaderi i materijali u projektu pišu za URP (`Universal Render Pipeline/Lit`), vidi [[URP u mom projektu]].
