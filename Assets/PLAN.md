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

*(checkboxovi ažurirani 15.7.2026. prema AUDIT-2026-07-14.md)*

### KRITIČNO — mora raditi za R1
> Bez ovih mehanika igra ne postoji.

- [x] Sferična gravitacija i kretanje po površini planeta
- [x] Hub planet (početni, statičan)
- [x] Ručno skupljanje resursa
- [x] Gradnja veze između dva planeta
- [x] Degradacija veze (zdravlje 0–100%, vizualni prikaz)
- [x] Kretanje igrača kroz veze (besplatno) vs. bez veze (troši resurse) *(teleportCost postavljen 15.7., skalira s udaljenošću)*
- [x] Centralno skladište na hubu (ograničen kapacitet)
- [x] Računalo na hubu (osnovna mreža — pregled planeta i veza)

---

### VAŽNO — mora raditi za R3
> Čini igru punom, ali prototip funkcionira bez ovih.

- [x] Proceduralna generacija planeta (tip → biom → resursi) *(pojednostavljeno po dopunskom planu: tint + resursna konfiguracija po tipu)*
- [x] Svi tipovi planeta (6 tipova) s različitim resursima *(23.7.: Napušteni/Abandoned tip IZBAČEN zajedno s artefaktima kojima je služio — ostaje 5 tipova)*
- [x] Alati (Faza 2 skupljanja) — nacrti, trošenje alata *(trajnost po slotu + prikaz od 15.7.)*
- [x] Automatizacija skupljanja (Faza 3) — strojevi, kapacitet *(23.7.: kvar strojeva (MachineBreakdown, ×3 na nestabilnim, E = popravak) + kapacitet skladišnog stroja sa zaustavljanjem collectora — T3 zatvoren)*
- [x] Automatski transport resursa kroz veze *(pojednostavljena 1:1 verzija collector→storage, po dopunskom planu)*
- ~~Artefakti — pronalaženje, tipovi, efekti~~ *(IZREZANO 23.7. — odluka: artefakata u igri neće biti; ArtifactType/ArtifactEvent/eventi obrisani iz koda, Abandoned planet izbačen)*
- [ ] Sekundarni hubovi — pretvorba planeta, klasteri
- [ ] Portal Terminal — besplatno putovanje do sekundarnog huba
- [ ] Hub upgrade stablo (Računalo, Skladište, Portal) *(HubProgress pragovi recepata rade — stablo ne postoji)*
- [ ] Skrivene drevne veze — otkrivanje i aktivacija *(23.7.: otkrivanje NE ide preko artefakta — treba drugi mehanizam, npr. skeniranje s Računala/mape)*
- ~~Mrežni fragmenti (artefakt) — otkrivanje skrivenih veza~~ *(IZREZANO 23.7. s artefaktima)*

---

### POŽELJNO — za R4 ako ima vremena
> Poboljšava iskustvo, ali nije gameplay-kritično.

- [ ] Vizualno uljepšavanje huba (kozmetika)
- [ ] Relej stanice na čvorišnim planetima
- [ ] Pojačivači na vezama i strojevima
- ~~Parnjaci artefakata (kombinacija s posebnim bonusom)~~ *(IZREZANO 23.7. s artefaktima)*
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

## Dodatak (3.7.2026.) — Akcijski plan do 15.8.

**Kontekst:** Audit koda na dan 3.7.2026. pokazao je da je sve KRITIČNO za R1 gotovo (uz jednu manju rupu), ali da je većina VAŽNO stavki za R3 ili djelomično odrađena ili postoji samo kao neiskorišten enum/event bez logike (artefakti, sekundarni hubovi, portal terminal, hub upgrade stablo, drevne veze, 4 od 6 tipova planeta). Rok za pisanje rada zahtijeva da igra bude funkcionalno gotova do **15.8.2026.**, ranije od izvornog R3 (2.9.) — sljedećih 6 tjedana svjesno reže opseg VAŽNO/POŽELJNO stavki po istoj logici kao tablica Rizika iznad (jednostavniji fallback umjesto pune verzije).

### Tjedan 1 (3.7–9.7) — Zatvori R1, isporuči R2
- Živi vizualni prikaz degradacije veze: boja veze prati trenutni `Health` (zelena→žuta→narančasta→crvena) uživo, ne samo pri kreiranju; treptanje ispod ~20%
- Bugfixevi, prolaz kroz cijelu core petlju
- **8.7. — R2 milestone**

### Tjedan 2 (10.7–16.7) — Dovrši proceduralnu generaciju
- Proširi generaciju planeta na svih 6 tipova (trenutno samo Rudarski/Organski)
- Za 4 nova tipa: bez punih biome/teren shadera — material/boja tint po tipu + odgovarajuća resource konfiguracija je dovoljna
- Flag "nestabilan" na Vulkanskim/Plinskim planetima (treba za tjedan 3 i degradaciju veza)

### Tjedan 3 (17.7–23.7) — Alati i strojevi do kraja
- Nacrti: veži otključavanje recepata za crafting uz hub napredak (2-3 praga)
- Strojevi: implementiraj kvar (Broken stanje) — šansa po tick-u, veća na nestabilnim planetima
- Skladišni stroj: dodaj kapacitet, zaustavi collector kad je pun

### ~~Tjedan 4 (24.7–30.7) — Artefakti (novi sustav)~~ IZREZANO 23.7.
*Odluka 23.7.2026.: artefakata u igri neće biti. Iz koda obrisano sve artefakt-vezano (ArtifactType, ArtifactEvent, OnArtifactFound/Activated/PairedCombined, OnBlueprintUnlocked, FirstArtifact milestone) i Napušteni/Abandoned tip planeta koji je postojao radi artefakata (spawn lista, materijal, resursna konfiguracija). Tjedan 24.7–30.7 se oslobađa — povući Tjedan 5 ranije i/ili iskoristiti kao buffer.*

### Tjedan 5 (31.7–6.8) — Drevne veze + Hub upgrade stablo
- Skeniranje/otkrivanje i aktivacija drevnih veza s varijabilnim stanjem
- ~~Mrežni fragment (4. tip artefakta) otkriva drevne veze u okolici~~ *(izrezano s artefaktima — otkrivanje riješiti bez artefakata, npr. skeniranje s Računala ili uređaja za mapu u radijusu)*
- Hub upgrade stablo: 3 nivoa računala + 2 nivoa skladišta sa stvarnim efektima (bez punog stabla iz GDD-a)

### Tjedan 6 (7.8–13.8) — Sekundarni hubovi + Portal + buffer
- Sekundarni hub: pretvorba jednog planeta, usporena degradacija i prošireno skladište u klasteru (bez pune klaster logike)
- Portal Terminal: minimalna verzija — besplatan teleport hub → sekundarni hub
- Ostatak tjedna: buffer za integracijske bugove, bez novih featurea
- **15.8. — feature freeze, tag build, materijal za rad**

### Svjesno izvan opsega do 15.8.
Kozmetika huba, relej stanice, monitoring zdravlja mreže (Nivo 3), zvuk/glazba, priča/log fragmenti — ostaju neodrađeni; u radu se mogu opisati kao future work. Automatski transport ostaje na pojednostavljenoj 1:1 verziji (stroj→skladište), bez punog sustava konfigurabilnih ruta.

---

*Dokument se ažurira s napretkom projekta.*
