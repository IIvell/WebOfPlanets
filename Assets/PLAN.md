# Web of Planets — Plan Projekta

**Početak:** 30. ožujka 2026.
**Engine:** Unity

---

## Rokovi

| # | Datum | Cilj |
|---|---|---|
| R1 | 1. srpnja 2026. | Playable Prototype |
| R2 | 8. srpnja 2026. | Polished Prototype / Demo |
| R3 | 2. rujna 2026. | Beta |
| R4 | 9. rujna 2026. | Final |

---

## Prioriteti Mehanika

### KRITIČNO — mora raditi za R1
> Bez ovih mehanika igra ne postoji.

- [ ] Sferična gravitacija i kretanje po površini planeta
- [ ] Hub planet (početni, statičan)
- [ ] Ručno skupljanje resursa
- [ ] Gradnja veze između dva planeta
- [ ] Degradacija veze (zdravlje 0–100%, vizualni prikaz)
- [ ] Kretanje igrača kroz veze (besplatno) vs. bez veze (troši resurse)
- [ ] Centralno skladište na hubu (ograničen kapacitet)
- [ ] Računalo na hubu (osnovna mreža — pregled planeta i veza)

---

### VAŽNO — mora raditi za R3
> Čini igru punom, ali prototip funkcionira bez ovih.

- [ ] Proceduralna generacija planeta (tip → biom → resursi)
- [ ] Svi tipovi planeta (6 tipova) s različitim resursima
- [ ] Alati (Faza 2 skupljanja) — nacrti, trošenje alata
- [ ] Automatizacija skupljanja (Faza 3) — strojevi, kapacitet
- [ ] Automatski transport resursa kroz veze
- [ ] Artefakti — pronalaženje, tipovi, efekti
- [ ] Sekundarni hubovi — pretvorba planeta, klasteri
- [ ] Portal Terminal — besplatno putovanje do sekundarnog huba
- [ ] Hub upgrade stablo (Računalo, Skladište, Portal)
- [ ] Skrivene drevne veze — otkrivanje i aktivacija
- [ ] Mrežni fragmenti (artefakt) — otkrivanje skrivenih veza

---

### POŽELJNO — za R4 ako ima vremena
> Poboljšava iskustvo, ali nije gameplay-kritično.

- [ ] Vizualno uljepšavanje huba (kozmetika)
- [ ] Relej stanice na čvorišnim planetima
- [ ] Pojačivači na vezama i strojevima
- [ ] Parnjaci artefakata (kombinacija s posebnim bonusom)
- [ ] Automatski monitoring zdravlja mreže (Računalo Nivo 3)
- [ ] Vizualni "mrežni pogled" (zoom out, graf prikaz)
- [ ] Zvukovi i glazba koji reagiraju na veličinu mreže

---

## Raspored po Fazama

---

### FAZA 1 — Temelji
**30. ožujka → 11. svibnja (6 tjedana)**

Cilj: sve što je KRITIČNO mora raditi.

| Tjedan | Zadatak |
|---|---|
| 1–2 | Sferična gravitacija, kretanje po planetu, kamera |
| 3 | Hub planet, osnovno UI, skladište |
| 4 | Ručno skupljanje resursa, lokalno skladište |
| 5 | Gradnja veze, vizualni prikaz zdravlja veze |
| 6 | Degradacija veze, kretanje kroz veze, teleport s cijenom |

---

### FAZA 2 — Sadržaj i Mehanika
**11. svibnja → 22. lipnja (6 tjedana)**

Cilj: dodati sve VAŽNE mehanike.

| Tjedan | Zadatak |
|---|---|
| 7 | Proceduralna generacija planeta (barem 3 tipa) |
| 8 | Alati — nacrti, skupljanje Faza 2 |
| 9 | Strojevi — automatizacija skupljanja, Faza 3 |
| 10 | Automatski transport resursa kroz veze |
| 11 | Skrivene drevne veze — skeniranje, aktivacija |
| 12 | Artefakti — pronalaženje, tipovi, efekti na nacrtima |

---

### FAZA 3 — Poliranje za R1/R2
**22. lipnja → 8. srpnja (2 + 1 tjedan)**

| Tjedan | Zadatak |
|---|---|
| 13 | Bugfixevi, balans resursa, UI poliranje |
| **1. srpnja** | **R1 — Playable Prototype** |
| 14 | Feedback iz R1, hitni popravci |
| **8. srpnja** | **R2 — Polished Prototype** |

---

### FAZA 4 — Proširenje
**8. srpnja → 18. kolovoza (6 tjedana)**

Cilj: dodati preostale VAŽNE mehanike i ostatak sadržaja.

| Tjedan | Zadatak |
|---|---|
| 15 | Sekundarni hubovi — pretvorba, klaster mehanika |
| 16 | Portal Terminal, hub upgrade stablo |
| 17 | Preostala 3 tipa planeta, balans resursa |
| 18 | Mrežni fragmenti, kompletna artefakt mehanika |
| 19 | Priča — fragmenti na računalu, milestone potrošnja artefakata |
| 20 | Bugfixevi, optimizacija |

---

### FAZA 5 — Poliranje za R3/R4
**18. kolovoza → 9. rujna (3 tjedna)**

| Tjedan | Zadatak |
|---|---|
| 21 | POŽELJNE mehanike (koliko stane), vizualni polish |
| **2. rujna** | **R3 — Beta** |
| 22 | Feedback iz R3, finalni bugfixevi |
| **9. rujna** | **R4 — Final** |

---

## Vizualni Pregled Vremenskog Okvira

```
Ožujak    Travanj       Svibanj       Lipanj        Srpanj    Kolovoz      Rujan
|---------|-------------|-------------|-------------|---------|------------|---------|
[== FAZA 1: Temelji ==][== FAZA 2: Sadržaj ==][F3][R1][R2][== FAZA 4 ==][F5][R3][R4]
```

---

## Rizici

| Rizik | Vjerovatnoća | Utjecaj | Plan B |
|---|---|---|---|
| Proceduralna generacija oduzme previše vremena | Srednja | Visok | Ručno kreirani planeti za R1, proceduralna za R3 |
| Degradacija veze kompleksna za balans | Visoka | Srednji | Jednostavan linearni decay za R1, kompleksniji za R3 |
| Automatizacija transporta buggy | Srednja | Visok | Samo ručni transport za R2, auto za R3 |
| Nema dovoljno vremena za priču | Niska | Nizak | Priča je POŽELJNO, ne KRITIČNO |

---

*Dokument se ažurira s napretkom projekta.*
