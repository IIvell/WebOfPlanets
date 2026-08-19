---
tags: [tehnologije]
---

# Unity 6

Verzija enginea u projektu: **6000.3.10f1**. Unity 6 je generacija koja je zamijenila stara godišnja imena (2021, 2022...) — verzije sad idu 6000.x.

## Zašto sam odabrao Unity 6

- **Aktualna generacija s dugoročnom podrškom (LTS)** — redovite zakrpe; starije godišnje verzije (2021/2022) više se ne održavaju.
- **Moderni standardi su u njoj zadani** — [[URP]] kao render pipeline i [[Input System (novi)]] kao sustav unosa; projekt koristi oboje, pa je sve „prvi izbor" enginea, ne borba protiv njega.
- **Performanse** — Unity 6 donosi optimizacije renderiranja (bolji culling, GPU-vođeno crtanje), korisno za scenu s 30 proceduralnih planeta odjednom.
- **Ekosustav** — aktualna dokumentacija, paketi i alati ciljaju Unity 6; verzije paketa u projektu (URP 17.3.0, Input System 1.18.0, AI Navigation 2.0.10) usklađene su s njom.

## Kontekst projekta

Samostalna PC igra: Mono scripting backend, .NET Standard API, [[Linear color space]], [[URP u mom projektu|PC pipeline asset]].

## ⚠ Gotcha za pitanja

*„Zašto se verzija zove 6000.3, a ne Unity 6?"* → Unity je 2024. napustio godišnje verzioniranje; Unity 6 = 6000.0, a 6000.3 je treće veliko ažuriranje te generacije (Unity 6.3).

## Odgovor u jednoj rečenici

> „Koristim Unity 6 jer je aktualna generacija enginea s dugoročnom podrškom — [[URP]] i novi [[Input System (novi)|Input System]] u njoj su standard, a starije verzije se više ne održavaju."
