# Web of Planets — Game Design Document

**Verzija:** 0.1
**Datum:** 2026-03-30
**Engine:** Unity

---

## 1. Koncept igre

Web of Planets je igra istraživanja i upravljanja mrežom u kojoj igrač putuje po planetima, otkriva i gradi veze između njih, skuplja resurse i razvija svoju civilizacijsku mrežu. Kretanje po površini planeta inspirirano je Super Mario Galaxy pristupom (sferična gravitacija), ali u jednostavnijem obliku.

Temelj igre je **teorija grafova/mreža** — planeti su čvorovi (nodes), a veze između njih su bridovi (edges). Igrač upravlja topologijom, zdravljem i protokom resursa kroz tu mrežu.

---

## 2. Osnovna Petlja (Core Loop)

```
Istraži planet
    → Skupljaj resurse ručno
    → Pronađi artefakte
    → Otkrij skrivene drevne veze
        → Aktiviraj ili sagradi vezu
        → Postavi automatsku rutu transporta resursa
        → Nadogradi Hub
            → Otključaj nove nacrte i informacije
                → Proširi mrežu u novi klaster
```

---

## 3. Planeti

### 3.1 Tipovi planeta

| Tip | Primarni Resursi | Sekundarni Resursi | Karakteristike |
|---|---|---|---|
| Rudarski | Ruda, Kamen | Kristali, Rijetki metali | Niža gravitacija, špilje |
| Organski | Drvo, Biomasa | Ljekovite biljke, Smola | Džungla, resursi rastu ciklično |
| Ledeni | Voda, Led | Krioplin, Stari fosili | Skliske površine, ciklično tajanje |
| Plinski | Plin, Energija | Plazma, Eterična para | Plutanje u atmosferi, nestabilno tlo |
| Vulkanski | Magma, Pepeo | Obsidijan, Geotermalna energija | Opasne zone, visoka nestabilnost veza |
| Napušteni | Kamen, Ruda | Artefakti | Ruševine, veća šansa artefakata |

### 3.2 Proceduralna Generacija

- Svaki planet generira se proceduralno pri kreiranju
- Tip planeta određuje dominantni biom, raspodjelu resursa i izgled terena
- Svaki planet ima "potencijal artefakta" (visok / nizak / nema) koji nije vidljiv igraču unaprijed
- Zone koje vizualno izgledaju kao ruševine ili anomalije imaju veću šansu artefakta

---

## 4. Mreža (Grafovi)

### 4.1 Dvije vrste veza

**Drevne (skrivene) veze:**
- Postoje ali su "uspavane" — igrač ih mora otkriti skeniranjem i istraživanjem
- Aktivacija je skuplja nego održavanje
- Stanje pri aktivaciji varira — može biti dobro ili loše ovisno o starosti
- Mrežni fragmenti (artefakti) mogu otkriti skrivene veze u okolici

**Izgrađene (player-made) veze:**
- Igrač bira rutu i tip veze
- Skuplje za izgradnju, ali igrač kontrolira kvalitetu
- Jeftinija gradnja = brža degradacija

### 4.2 Degradacija veze

Svaka veza ima **zdravlje (0–100%)** koje pada s vremenom.

**Faktori koji UBRZAVAJU degradaciju:**
- Preopterećenje protokom resursa
- Blizina nestabilnih planeta (plinski, vulkanski)
- Asteroid pojasevi na ruti
- Duljina veze
- Zanemarivanje (rijetko korištenje)

**Faktori koji USPORAVAJU degradaciju:**
- Sekundarni hub na jednom kraju veze
- Kvalitetniji materijali pri gradnji
- Pojačivači (građevine duž veze)
- Kraće veze kroz posredne planete

**Vizualni prikaz zdravlja:**
- Boja veze: zelena → žuta → narančasta → crvena → nestaje
- Trepćuća veza = kritično stanje

**Posljedice raspada:**
- Automatski transport resursa prestaje
- Besplatno kretanje igrača između tih planeta prestaje

### 4.3 Čvorišni planeti (Relay)

- Planet koji prima i prosljeđuje resurse bez da je hub
- Troši dio resursa kao "prolaznu naknadu"
- Može se izgraditi "relej stanica" na njemu za smanjenje gubitka

---

## 5. Kretanje Igrača

| Situacija | Cijena |
|---|---|
| Planet s aktivnom vezom na hub | Besplatno |
| Planet duž lanca aktivnih veza | Besplatno |
| Planet s vezom do sekundarnog huba | Besplatno |
| Planet bez veze | Resursi za teleport (skuplji što je dalje) |

- Kretanje po površini planeta: sferična gravitacija (Super Mario Galaxy stil), bez kompleksnih fizikalnih mehanika
- Igrač može ručno hodati između zona na planetu

### 5.1 Zdravlje igrača i opasne zone

- Igrač ima zdravlje (0–100), prikazano trakom u HUD-u
- **Vulkanski planeti** imaju zone lave/vulkana raspoređene po površini — igrač koji stoji u zoni prima štetu po sekundi
- Kratka neranjivost nakon primljene štete spriječava da jedan ulazak u zonu odmah isprazni cijelo zdravlje
- Pad zdravlja na 0 = igrač "gine" — trenutno zaustavlja igru (game over state), oporavak/respawn nije još definiran
- Ostali izvori štete (padovi, neprijatelji, druge opasne zone) mogu se dodati kasnije na istom sistemu

---

## 6. Resursi

### 6.1 Skupljanje — tri faze progresije

**Faza 1 — Ručno:**
- Igrač hoda po planetu i direktno skuplja resurse
- Sporo, ali jedini način da se pronađu artefakti
- Igrač upoznaje teren planeta

**Faza 2 — Alati:**
- Bolji alati povećavaju brzinu i radijus skupljanja
- Alati se troše, trebaju popravak ili zamjenu
- Specijalni alati za specifične tipove planeta (bez odgovarajućeg alata niska efikasnost)
- Nacrti za alate otključavaju se kroz hub napredak

**Faza 3 — Automatizacija:**
- Strojevi postavljeni na planetu rade bez prisutnosti igrača
- Svaki stroj skuplja samo ono za što je dizajniran
- Stroj ima kapacitet skladišta — mora se isprazniti (ručno ili automatskim transportom)
- Stroj se može pokvariti, češće na nestabilnim planetima
- Strojevi ne pronalaze artefakte

### 6.2 Skladišni Sistem

- Svaki planet ima lokalno skladište (ograničeni kapacitet)
- Hub ima centralno skladište (proširivo upgradima)
- Sekundarni hub proširuje kapacitet lokalnog klastera
- Puno lokalno skladište = stroj staje

### 6.3 Automatski Transport

- Igrač postavlja "rute" — planet A šalje X resursa na planet B svakih N minuta
- Transport ovisi o zdravlju veze — degradirana veza usporava ili prekida transport
- Preopterećena veza degradira brže (tradeoff: brzina vs. trajnost)

---

## 7. Artefakti

### 7.1 Pronalaženje

- Isključivo ručnim istraživanjem — strojevi ih ne mogu naći
- Potencijal artefakta planete nije vidljiv unaprijed (visok/nizak/nema)
- Zone s ruševinama i anomalijama imaju veću šansu pronalaska
- Rijetki — nije garantirano na svakom planetu

### 7.2 Tipovi Artefakata

| Tip | Efekt |
|---|---|
| Nacrti | Otključavaju nove građevine, alate, ili tipove veza |
| Pojačivači | Postavljaju se na vezu ili stroj, poboljšavaju performanse |
| Mrežni fragmenti | Otkrivaju skrivene drevne veze u okolici |
| Energetski jezgri | Jednokratni — daju veliki boost (gradnja, popravak veze) |
| Parnjaci | Artefakt koji ima parnjak na drugom planetu — kombinirani daju poseban bonus |

---

## 8. Hub Planet

### 8.1 Glavni Hub

Početni planet, temelj cijele mreže. Sadrži:

- **Glavno Računalo** — središnji informacijski centar
- **Centralno Skladište** — proširivo upgradima
- **Portal Terminal** — veze do sekundarnih hubova
- **Kozmetičke Zone** — vizualno uljepšavanje

### 8.2 Sekundarni Hubovi

- Igrač može pretvoriti drugi planet u sekundarni hub
- Limitiran broj sekundarnih hubova (ovisi o razvijenosti glavnog huba)
- Sekundarni hub:
  - Usporava degradaciju veza u svom klasteru
  - Proširuje lokalno skladište klastera
  - Automatizira lokalni transport unutar klastera
  - Dostupan besplatno putem portala s glavnog huba

### 8.3 Upgrade Stablo

```
Računalo (Nivo 1)
│   └── Osnovna mreža, skladište 100
│
├── Skladište (Nivo 2) ──── povećan kapacitet
│
├── Računalo (Nivo 2) ──── otkriva skrivene veze u radijusu
│       └── novi nacrti za alate
│
├── Portal Terminal ──── veza do 1 sekundarnog huba
│       └── Portal (Nivo 2) ──── do 3 sekundarna huba
│
└── Računalo (Nivo 3) ──── automatski monitoring zdravlja mreže
        └── novi nacrti za strojeve
```

### 8.4 Kozmetičke Opcije

- Vrtovi od biljaka s organskih planeta
- Kristalne strukture s rudarskih planeta
- Platforme i tornjevi od različitih materijala
- Tekstura/boja površine huba ovisi o korištenim materijalima
- Isključivo kozmetičko — troši samo standardne resurse, ne artefakte

### 8.5 Priča kroz Hub

- Specifični resursi i artefakti troše se za story progress
- Svaki milestone otključava novi fragment na računalu (log, poruka, mapa)
- Priča se ne priča direktno — igrač sam slaže sliku iz fragmenta

Primjer milestone tablice:

| Milestone | Što se troši | Što se otključava |
|---|---|---|
| Rani napredak | Osnovna ruda + kamen | Nacrt za prvu vezu |
| Srednji napredak | Rijetki metal + Mrežni fragment | Drevna mapa — novi klaster |
| Kasni napredak | Energetski jezgri + par artefakata | Završni log, kraj priče |

---

## 9. Mrežna Strategija

### 9.1 Topološki Rizici

- Previše centralizirana mreža (sve ovisi o jednoj točki) = veliki rizik ako ta točka otkaže
- Igra nagrađuje **redundantnost** — više puteva između klastera
- Neprijatelji/katastrofe mogu napadati veze, ne samo planete

### 9.2 Preporučene Strategije

- Sekundarni hub na "čvorišnom" planetu koji spaja više klastera = stabilnost cijele mreže
- Kraće veze kroz posredne planete umjesto jedne duge direktne veze
- Drevne veze kao "brze putanje" — jeftinije od gradnje, ali nestabilnije

---

## 10. Tehničke Napomene (Unity)

- Sferična gravitacija po uzoru na Super Mario Galaxy pristup
- Proceduralna generacija terena planeta (tip → biom → raspodjela resursa)
- Graf struktura za mrežu — dinamički dodavanje/uklanjanje čvorova i bridova
- Zdravlje veze kao numerička vrijednost, osvježava se svakih N sekundi
- Automatski transport kao coroutine ili event-driven sistem

---

*Dokument će se ažurirati s razvojem projekta.*
