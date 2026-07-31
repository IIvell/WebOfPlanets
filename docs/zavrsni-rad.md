<!--
  ZAVRŠNI RAD — Web of Planets
  Sveučilište u Rijeci, Fakultet informatike i digitalnih tehnologija
  Mentor: izv. prof. dr. sc. Miran Pobar
  Stil citiranja: IEEE | Ciljani opseg: 35–45 stranica
  Napomena: uvodne stranice (naslovnica, zadatak, sažetak) dovršavaju se na kraju,
  kad je tijelo rada gotovo. Sadržaj se generira automatski pri prijelomu u Word.
-->

# Naslovnica

>>> POPUNITI: [Ime i prezime autora te točan naziv studija za naslovnicu]
    Očekivana duljina: podaci za naslovnicu (autor, studij, akademska godina)
    Natuknice koje bi trebalo pokriti: puno ime i prezime; naziv studija
    (prijediplomski studij Informatika?); JMBAG; akademska godina obrane <

Predložena struktura naslovnice (prema priloženim radovima FIDIT-a):
Sveučilište u Rijeci · Fakultet informatike i digitalnih tehnologija ·
naziv studija · **Ime Prezime** · **Razvoj računalne igre Web of Planets** ·
Završni rad · Mentor: izv. prof. dr. sc. Miran Pobar · Rijeka, 2026.

---

# Zadatak za završni rad

*(Umeće se potpisani obrazac fakulteta — stranica se ostavlja rezervirana.)*

---

# Sažetak

U sklopu ovog završnog rada razvijena je *Web of Planets*, jednoigračka
trodimenzionalna računalna igra preživljavanja i automatizacije, izrađena u
okruženju Unity 6 s cjevovodom Universal Render Pipeline. Igrač se kreće po
površinama sferičnih, proceduralno stvorenih planeta, prikuplja resurse,
izrađuje alate i strojeve te povezuje planete u mrežu veza koje s vremenom
propadaju; cilj igre je nadogradnja središnjeg čvorišta kroz pet pragova
napretka. Rad opisuje korištene tehnologije, dizajn igre i programska
rješenja njezinih sustava: proceduralno generiranje svijeta s jamstvom
povezivosti grafa planeta, sferičnu gravitaciju i zadržavanje igrača uz
površinu, mrežu veza temeljenu na razapinjućem stablu, podatkovno vođene
strojeve i recepte, proceduralno sintetizirani zvuk bez zvučnih datoteka te
sustav spremanja koji svijet obnavlja u mjestu, bez ponovnog učitavanja
scene. Sva logika igre — približno devedeset skripti u jeziku C# — napisana
je za potrebe rada.

**Ključne riječi:** računalna igra, Unity, C#, proceduralno generiranje,
sferična gravitacija, teorija grafova, ScriptableObject, sustav spremanja

---

# Sadržaj

*(Generira se automatski pri prijelomu u Word.)*

---

# 1. Uvod

Računalne igre danas čine jedan od najvećih segmenata industrije zabave, kako
po prihodima tako i po broju korisnika [1]. Usporedno s rastom industrije
razvijali su se i alati za izradu igara: suvremeni pogonski sustavi (engl.
*game engine*) poput Unityja i Unreal Enginea preuzimaju na sebe prikaz
grafike, simulaciju fizike, obradu ulaza i rad sa zvukom, čime omogućuju da
i pojedinac u razumnom roku razvije tehnički zaokruženu trodimenzionalnu
igru [2]. Upravo je to
polazište ovog rada: cjelokupna igra opisana u nastavku djelo je jednog
autora, od dizajna sustava do programskog koda.

>>> POPUNITI: [Zašto si odabrao baš ovu temu i ovakvu igru? Što te osobno
    motiviralo?]
    Očekivana duljina: 1 odlomak / 100–150 riječi
    Natuknice koje bi trebalo pokriti: odakle ideja o planetima kao mreži
    (GDD navodi teoriju grafova kao temelj — je li to došlo s nekog kolegija?);
    inspiracija sferičnom gravitacijom iz Super Mario Galaxy (navedena u GDD-u);
    zašto kombinacija survival i factory žanra; jesi li otprije imao iskustva
    s Unityjem ili je ovo prvi veći projekt <

Cilj ovog završnog rada bio je razviti funkcionalnu računalnu igru *Web of
Planets* — jednoigračku 3D igru preživljavanja i automatizacije smještenu u
svemir — te dokumentirati njezina programska rješenja. Igrač se u njoj kreće
po površinama sferičnih, proceduralno stvorenih planeta, ručno prikuplja
resurse, izrađuje alate i strojeve koji prikupljanje automatiziraju te
povezuje planete u mrežu veza koje s vremenom propadaju i traže održavanje.
Dugoročni cilj igre jest nadogradnja središnjeg čvorišta (huba) do pobjede.
Igra je izrađena u okruženju Unity 6 s cjevovodom Universal Render Pipeline,
a sva logika — približno devedeset skripti u jeziku C# — napisana je za
potrebe ovog rada.

Uz sam opseg sustava, rad se od tipične studentske igre razlikuje po nekoliko
tehničkih odluka koje su detaljno obrađene u praktičnom dijelu: gravitacija
nije globalna nego sferična (svaki planet privlači tijela prema svom
središtu), svijet se ne slaže ručno u sceni nego se u cijelosti generira
proceduralno pri pokretanju, mreža veza među planetima modelirana je kao graf
s jamstvom povezanosti, gotovo svi sustavi stvaraju se programski pri
pokretanju umjesto uređivanjem scene, a spremanje i učitavanje igre izvedeno
je bez ponovnog učitavanja scene — svijet se ruši i ponovno gradi istim
kodom kojim je i nastao. Zvučni efekti ne koriste nijednu zvučnu datoteku:
sintetiziraju se proceduralno u stvarnom vremenu.

Rad je organiziran na sljedeći način. U drugom poglavlju opisane su
tehnologije i alati korišteni u razvoju: Unity 6, Universal Render Pipeline,
programski jezik C# sa skriptnim modelom Unityja te novi sustav ulaza (Input
System). Treće poglavlje donosi dizajn igre: koncept, osnovnu petlju,
tipove planeta i sustav progresije. Četvrto, središnje poglavlje prolazi
kroz razvoj igre sustav po sustav — od arhitekture projekta i proceduralnog
generiranja svijeta, preko gravitacije, mreže veza, rudarenja, inventara i
strojeva, do korisničkog sučelja, zvuka i sustava spremanja — uz isječke
stvarnog koda iz projekta. Peto poglavlje sadrži zaključak s osvrtom na
postignuto i mogućnosti daljnjeg razvoja.

---

**[Za popuniti u ovom poglavlju]**
- POPUNITI blok: osobna motivacija za odabir teme (1 odlomak).
- Izvori: [1] (statistika industrije — čeka tvoj odabir u Literaturi) i
  [2] (Unity dokumentacija — predložen unos u Literaturi).
- Provjeriti točan broj skripti prije predaje (trenutno ~90) i po potrebi
  ažurirati formulaciju "približno devedeset".

---

# 2. Korištene tehnologije

U ovom poglavlju opisane su tehnologije na kojima je igra izgrađena. Zajedničko
im je da su industrijski standard i besplatno dostupne studentima, a opseg
opisa ograničen je na ono što praktični dio rada stvarno koristi.

## 2.1. Unity 6 i Universal Render Pipeline

Unity je višeplatformski pogonski sustav za razvoj igara koji objedinjuje
prikaz 2D i 3D grafike, simulaciju fizike, obradu ulaza, rad sa zvukom i
sustav za izgradnju korisničkih sučelja [2].
Rad u Unityju organiziran je oko koncepta scene: scena sadrži objekte
(*GameObject*), a ponašanje svakog objekta određuju komponente koje su na
njega pridodane — od ugrađenih (transformacija, fizikalno tijelo, sudarač)
do vlastitih skripti. Ovaj kompozicijski model, u kojem se funkcionalnost
slaže dodavanjem komponenti umjesto nasljeđivanjem, temelj je arhitekture
cijelog projekta.

Igra je razvijena u Unityju verzije 6000.3.10f1 (Unity 6). Za prikaz grafike
korišten je *Universal Render Pipeline* (URP) verzije 17.3.0 — Unityjev
skriptabilni cjevovod iscrtavanja namijenjen širokom rasponu platformi, koji
zamjenjuje stariji ugrađeni cjevovod (*Built-in Render Pipeline*) [3].
Izbor URP-a ima praktične posljedice po projekt: svi
materijali moraju koristiti URP-ove sjenčare (u projektu je to pretežno
*Universal Render Pipeline/Lit*), a postavke prikaza definirane su u zasebnim
assetima cjevovoda. Projekt sadrži dvije takve konfiguracije — jednu za PC i
jednu rezervnu za mobilne uređaje — te koristi linearni prostor boja.

*[Slika: sučelje Unity Editora s otvorenim projektom Web of Planets —
snimiti prozore Scene, Hierarchy, Inspector i Project]*

## 2.2. Programski jezik C# i skriptni model Unityja

Sva logika igre napisana je u jeziku C#, objektno orijentiranom jeziku
platforme .NET koji je jedini službeno podržani skriptni jezik Unityja
[4].
Projekt koristi Mono izvršno okruženje i razinu kompatibilnosti .NET
Standard.

Dvije su klase skriptnog modela posebno važne za ovaj rad. Prva je
`MonoBehaviour`, bazna klasa svih skripti koje se pridodaju objektima u
sceni. Unity na njoj poziva metode životnog ciklusa unaprijed određenim
redoslijedom: `Awake` pri stvaranju objekta, `Start` prije prvog ažuriranja,
`Update` jednom po iscrtanoj sličici te `FixedUpdate` u stalnom koraku
fizikalne simulacije [5]. Razlika između posljednje dvije metode u projektu je strogo
poštovana: sve što pomiče fizikalna tijela (gravitacija, kretanje igrača)
izvodi se u `FixedUpdate`, a očitavanje ulaza i logika sučelja u `Update`.

Druga je klasa `ScriptableObject` — objekt koji, za razliku od
`MonoBehaviour`, ne živi u sceni nego kao samostalna datoteka (asset) u
projektu, pa služi za odvajanje podataka od logike [6]. U projektu je na ovaj način definirano
pedesetak podatkovnih asseta: opisi strojeva, vrste resursa, recepti za
izradu predmeta, alati i uređaji. Dizajnerske vrijednosti (cijene, brzine,
kapaciteti) time se mijenjaju u Inspectoru, bez diranja koda, a ista
C# klasa (npr. opis stroja) ima više instanci-asseta za različite strojeve.
Detaljnije o toj organizaciji govori potpoglavlje 4.7.

*[Slika: mapa Data/ u prozoru Project s podmapama Machines, Resources,
Recipes, Tools — prikaz ScriptableObject asseta]*

## 2.3. Sustav ulaza (Input System)

Za obradu ulaza korišten je isključivo Unityjev noviji paket *Input System*
(verzija 1.18.0), koji zamjenjuje naslijeđeni *Input Manager* i klasu
`UnityEngine.Input` [7]. Umjesto
izravnog ispitivanja tipki, ulaz se opisuje assetom akcija: datoteka
`PlayerInputActions.inputactions` definira akcije `Movement` (WASD),
`Jump` i `MouseLook`, a iz nje je generiran istoimeni C# omotač preko kojeg
skripte čitaju vrijednosti akcija. Prednost ovakvog pristupa je odvajanje
logike ("skoči") od konkretne tipke te mogućnost kasnijeg dodavanja drugih
uređaja bez izmjene koda.

Tipke izvan tog asseta (interakcija, inventar, postavljanje strojeva itd.)
namjerno nisu razbacane po skriptama, nego su centralizirane u statičkoj
klasi `GameKeys`, zajedno s prikaznim imenima za ekran s kontrolama:

```csharp
public static class GameKeys
{
    public const Key Interact      = Key.E;
    public const Key Inventory     = Key.I;
    public const Key PickupMachine = Key.X;
    // ...

    // Null-safe provjere — Keyboard.current je null bez tipkovnice.
    public static bool WasPressed(Key key)
        => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
}
```

Time postoji jedan izvor istine za raspored tipki: prije ove konsolidacije
iste su tipke bile hardkodirane u četrnaestak skripti, a njihova imena
održavana odvojeno i u tekstu izbornika s kontrolama, što je bio čest izvor
nedosljednosti.

*[Slika: asset PlayerInputActions.inputactions otvoren u Input Actions
editoru — akcije Movement, Jump, MouseLook]*

## 2.4. Pomoćni alati i sadržaj trećih strana

Korisničko sučelje izgrađeno je Unityjevim sustavom uGUI uz paket TextMesh
Pro za prikaz teksta [8]. Od
vizualnog sadržaja igra koristi gotove 3D modele i teksture iz besplatnih
paketa (modeli svemirske opreme, vegetacije, planeta i resursa), koji su u
projektu odvojeni u zasebnu mapu i nisu mijenjani — dok su, nasuprot tome,
sav programski kod, zvučni efekti (proceduralno sintetizirani, bez ijedne
zvučne datoteke) i efekti čestica nastali u sklopu ovog rada.

>>> POPUNITI: [U kojem si uređivaču koda pisao skripte (Visual Studio,
    Rider...?) i jesi li koristio sustav za verzioniranje (Git)?]
    Očekivana duljina: 2–3 rečenice
    Natuknice koje bi trebalo pokriti: projekt ima instalirane integracije i
    za Visual Studio i za Rider pa iz koda ne mogu utvrditi koji si stvarno
    koristio; navesti i alate koje si koristio uz Editor (npr. MCP for Unity
    za rad s Editorom — odluči želiš li ga spomenuti u radu) <

---

**[Za popuniti u ovom poglavlju]**
- POPUNITI blok: uređivač koda, verzioniranje, odluka o spominjanju
  MCP for Unity alata.
- Izvori: [2]–[8], predloženi unosi (službena dokumentacija) postoje u
  Literaturi — dopuniti samo datume pristupa.
- 3 predviđene slike (screenshotovi sučelja Editora) — njih unityMCP ne
  može snimiti (snima prikaz igre, ne prozore Editora); snimi ih ručno
  (Win+Shift+S) ili mi daj pristup ekranu pa ih snimim ja.
- Provjeriti točan broj ScriptableObject asseta prije predaje (trenutno 52).

# 3. Opis igre Web of Planets

*Web of Planets* je jednoigračka trodimenzionalna igra preživljavanja i
automatizacije. Prije početka programiranja dizajn je razrađen u zasebnom
dokumentu (*game design document*, GDD), koji je tijekom razvoja služio kao
referenca, ali se od njega odstupalo gdje je to opseg zahtijevao. Ovo
poglavlje opisuje konačni, implementirani dizajn igre.

## 3.1. Koncept i žanr

Igrač se nalazi u malom svemiru sastavljenom od tridesetak sitnih planeta.
Po svakom planetu može hodati cijelom površinom: gravitacija nije globalna
"prema dolje" nego sferična, usmjerena prema središtu planeta na kojem se
igrač trenutno nalazi — pristup inspiriran igrom Super Mario Galaxy, naveden
kao uzor već u dizajnerskom dokumentu. Iz žanra automatizacije (tzv.
*factory* igre, poput naslova Factorio ili Satisfactory) igra preuzima
središnju ideju napredovanja od ručnog rada prema automatiziranoj
proizvodnji: igrač isprva svaki resurs iskopa sam, a kasnije gradi strojeve
koji to rade umjesto njega [9]. Iz žanra preživljavanja dolaze zdravlje igrača,
neprijatelji i opasni okoliši pojedinih planeta.

Ono što igru konceptualno izdvaja i po čemu je dobila ime jest mreža:
planeti su čvorovi, a veze koje igrač među njima gradi bridovi grafa. Veze
nisu trajne — s vremenom propadaju, i to brže ako im je krajnja točka
nestabilan (vulkanski ili plinoviti) planet — pa igrač uz širenje mreže
mora upravljati i njezinim zdravljem. Upravljanje topologijom mreže time
postaje ravnopravna mehanika uz rudarenje i izradu predmeta.

## 3.2. Osnovna petlja igre

Osnovna petlja povezuje istraživanje, prikupljanje i progresiju:

1. igrač na trenutnom planetu ručno prikuplja resurse alatom;
2. prikupljeno odlaže u skladište središnjeg čvorišta (huba);
3. ulaganjem resursa otključava sljedeći prag napretka huba, koji donosi
   nove recepte za alate i strojeve te veći kapacitet skladišta;
4. novim alatima i strojevima može doseći resurse i planete koji su mu
   dotad bili nedostupni (npr. plinoviti planet zahtijeva plinsku masku);
5. gradnjom veza i teleportera širi mrežu do novih planeta — i petlja
   kreće ispočetka, na zahtjevnijem planetu.

Oko te se glavne petlje vrte dvije sporedne: automatizacija (postavljeni
strojevi prikupljaju i prerađuju resurse i dok se igrač bavi drugim) i
održavanje (veze degradiraju i traže popravak, strojevi se mogu pokvariti,
neprijatelji i opasne zone troše zdravlje igrača, a pogibija vraća igrača na
točku oživljavanja).

*[Slika 4: Osnovna petlja igre — `docs/slike/slika-04-osnovna-petlja.svg`]*

## 3.3. Tipovi planeta i resursi

Svijet se sastoji od početnog hub planeta i pet tipova običnih planeta.
Hub je sigurna baza: na njemu nema neprijatelja, a nalaze se skladište,
računalo za upravljanje mrežom i napretkom te početni alat. Svaki obični
planet pri otkrivanju dobiva 3–5 neprijateljskih mobova koji čuvaju
njegove resurse.

| Tip planeta | Primarni resursi | Posebnosti |
|---|---|---|
| Rudarski | kamen, ruda | početna, najsigurnija grana |
| Organski | drvo, biljke | resursi za drugi prag napretka |
| Ledeni | led | resurs trećeg praga |
| Plinoviti | plin | atmosfera šteti igraču bez plinske maske; nestabilan kraj veze |
| Vulkanski | vulkanske rune | zone opasnosti na površini; nestabilan kraj veze |

Uz sirovine postoje i prerađeni resursi — središnji je metalni ingot, koji
nastaje taljenjem rude u talionici i pojavljuje se kao sastojak gotovo svih
kasnijih pragova i recepata. Raspored resursa nije dekorativan: pragovi
napretka huba dizajnirani su tako da svaki traži resurse s tipa planeta na
koji igrač dotad još nije morao kročiti, čime progresija vodi igrača kroz
sve grane svijeta (rudarski → organski → ledeni i plinoviti → vulkanski).

*[Slika 5: Planeti pet tipova — `docs/slike/slika-05a-organski.png` …
`slika-05e-vulkanski.png` (po jedan planet svakog tipa)]*

## 3.4. Progresija i cilj igre

Progresija je vezana uz pragove napretka huba, kojih je pet. Svaki prag
troši određene resurse iz hub skladišta i zauzvrat otključava recepte te
povećava kapacitet skladišta:

| Prag | Zahtijeva | Otključava |
|---|---|---|
| 1 | 10 kamena, 6 rude | Collector Machine, Ore Collector, Network Scanner, +25 skladišta |
| 2 | 6 ingota, 5 drva, 4 biljke | Drill, Hub Uplink, Teleporter, Gas Mask, +25 skladišta |
| 3 | 8 ingota, 6 leda, 4 plina | Ore/Gas Extractor, Cryo Harvester, Rune Drill, Respawn Totem, +50 skladišta |
| 4 | 10 ingota, 4 rune | Blast Furnace, Eternal Pickaxe, Network Computer, +50 skladišta |
| 5 | 12 ingota, 6 runa, 6 plina, 6 leda | Two-Way Teleporter, +100 skladišta |

Otključavanjem petog, posljednjeg praga igra je dobivena i prikazuje se
pobjednički ekran. Poraz u klasičnom smislu ne postoji: kad zdravlje igrača
padne na nulu, igrač se oživljava na hubu ili na totemu za oživljavanje ako
ga je izgradio, pa je cijena pogibije izgubljeno vrijeme i pozicija, a ne
kraj igre.

>>> POPUNITI: [Koliko se finalna igra razlikuje od prvotnog dizajna i zašto?]
    Očekivana duljina: 1 odlomak / 100–150 riječi
    Natuknice koje bi trebalo pokriti: GDD (v0.1, ožujak 2026.) predviđa
    artefakte, "drevne veze" koje se otkrivaju skeniranjem, relay planete i
    šesti tip planeta (napušteni) — ničeg od toga nema u finalnoj igri;
    opiši kako si odlučivao što izbaciti (opseg? vrijeme? testiranje?);
    event bus i danas sadrži rezervirane evente za artefakte — je li plan
    da se jednom dodaju? <

---

**[Za popuniti u ovom poglavlju]**
- POPUNITI blok: razlika GDD-a i finalne igre (odluke o opsegu).
- Izvor: [9] (žanr factory igara — čeka tvoj odabir u Literaturi; po želji
  i referenca na Super Mario Galaxy, Nintendo 2007.).
- Slike snimljene i povezane: dijagram petlje (slika 4, SVG) i pet tipova
  planeta (slike 5a–5e).
- Ovo poglavlje uvodi prve dvije tablice — ako ih zadržimo, predložak
  fakulteta vjerojatno traži i "Popis tablica" na kraju rada (dodat ću ga).
- Provjeriti u igri: nastaje li resurs "voda" (Water_liquid asset postoji)
  preradom leda i gdje se koristi — u kodu se ne referencira imenom, pa
  nisam mogao potvrditi.

# 4. Razvoj igre

Ovo poglavlje prolazi kroz izgradnju igre sustav po sustav, redoslijedom koji
prati ovisnosti: od arhitekture projekta i stvaranja svijeta, preko kretanja
i mreže veza, do gospodarstva igre (resursi, strojevi, hub) i infrastrukture
(sučelje, zvuk, spremanje). Isječci koda preuzeti su doslovno iz projekta i
kraćeni samo uklanjanjem dijelova nebitnih za objašnjenje.

## 4.1. Arhitektura projekta

Sav kod igre — devedesetak skripti — nalazi se u mapi `Assets/_Project/
Scripts`, podijeljen u trinaest domenskih mapa (`Planet`, `Machines`,
`Player`, `UI`, `Game`...). Sve skripte dijele jedan imenski prostor
`WebOfPlanets` i prevode se u jedan sklop (*assembly*); mape služe samo
preglednosti. Podaci su odvojeni od koda: dizajnerske vrijednosti žive u
ScriptableObject assetima u mapi `Assets/_Project/Data`.

Najvažnija arhitektonska odluka projekta jest da igra koristi **jednu jedinu
scenu**, i to gotovo praznu: u njoj se nalaze samo igrač, hub planet i
nekoliko sistemskih objekata. Sve ostalo — glavni izbornik, zvuk, efekti
čestica, otrovna atmosfera, cijeli proceduralni svijet — stvara se
programski pri pokretanju. Dvadesetak sustava koristi za to Unityjev
mehanizam samopokretanja:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
private static void Bootstrap()
{
    if (Instance != null) return;
    new GameObject("AudioManager").AddComponent<AudioManager>();
}
```

Metoda označena atributom `RuntimeInitializeOnLoadMethod` poziva se
automatski nakon učitavanja scene, pa sustav sam stvori svoj objekt i
konfigurira se u kodu, bez ijedne izmjene scene. Statička stanja koja bi
preživjela ponovno pokretanje u Editoru resetiraju se istim mehanizmom u
fazi `SubsystemRegistration`. Ovakav pristup ima i dalekosežnu posljedicu
opisanu u potpoglavlju 4.12: učitavanje spremljene igre ne smije ponovno
učitati scenu, jer bi runtime-stvoreni sustavi nestali.

Domene međusobno komuniciraju preko statičkog sabirničkog razreda
`GameEventBus` s tridesetak događaja i pripadnim strukturama podataka.
Sustav koji nešto objavi ne zna tko ga sluša: primjerice, inventar pri
dodavanju predmeta samo podigne događaj, a na njega neovisno reagiraju
sučelje i zvuk — domenska klasa tako ne ovisi o audio kodu ni u vrijeme
prevođenja. Sabirnica na jednom mjestu provodi i sitnu politiku: događaj
kritičnog stanja veze izveden je iz događaja promjene zdravlja.

```csharp
public static void Raise(ConnectionHealthChangedEvent e)
{
    OnConnectionHealthChanged?.Invoke(e);
    if (e.Health <= 20f) OnConnectionCritical?.Invoke(e);
}
```

Globalno stanje igre drži `GameManager`: enumeracija `GameState` (Playing,
Paused, GameOver, Victory) mijenja se kroz jednu metodu koja ujedno
zaustavlja simulaciju (`Time.timeScale`), statičko svojstvo `IsPlaying`
služi kao ulazna brana svim skriptama koje čitaju upravljačke tipke, a
prekidač `TestingMode` na jednom mjestu čini sve troškove u igri besplatnima
(izrada, veze, teleporti, pragovi, održavanje) — nezamjenjiv pri testiranju
kasnih faza igre.

## 4.2. Proceduralno generiranje svijeta

Svijet stvara `PlanetCreator`: pri pokretanju spawna 30 planeta (polje
`startingPlanets`) nasumičnog promjera 35–100 jedinica, nasumične gravitacije
10–40 m/s² i nasumičnog tipa od pet mogućih, s materijalom prema tipu.

Ključni problem generiranja nije izgled, nego **povezivost**. Veze se mogu
graditi samo između planeta unutar zadanog dometa, pa bi naivno razbacivanje
planeta oko huba stvaralo otoke do kojih igrač nikad ne može doći — mjerenja
tijekom razvoja pokazala su da je pri spawnu svih planeta oko huba u prosjeku
12 od 30 planeta bilo trajno nedostupno. Rješenje je ulančani spawn: svaki
novi planet sidri se na *nasumično odabrani već stvoreni* planet i mora pasti
unutar dometa veze od sidra, čime je graf potencijalnih veza povezan po
konstrukciji.

```csharp
float chainMaxDist = connectionManager != null
    ? Mathf.Min(maxSpawnDistance, connectionManager.MaxConnectionRange * 0.99f)
    : maxSpawnDistance;

for (int i = 0; i < startingPlanets; i++)
{
    Vector3 anchor = _spawnedPositions.Count > 0
        ? _spawnedPositions[Random.Range(0, _spawnedPositions.Count)]
        : origin;
    SpawnPlanet(anchor, i, chainMaxDist, ...);
}
```

Faktor 0,99 nije slučajan: `ConnectionManager` odbija par planeta strogim
uspoređivanjem s dometom, pa bi planet postavljen točno na granicu ostao bez
veze. Sama pozicija bira se odbacivanjem (*rejection sampling*): do 30
pokušaja traži se točka dovoljno udaljena od svih postojećih planeta, a ako
svi pokušaji propadnu, planet se ipak postavlja — uz poštovanje uvjeta
povezivosti, ali bez jamstva razmaka. Prioriteti su, dakle, izričiti:
povezivost je tvrda invarijanta, razmak samo estetika.

Zanimljiv je detalj i sudarač planeta. Primitivna sfera u Unityju ima
analitički `SphereCollider`, no vidljiva mreža poligonalne sfere mjestimično
ponire do ~1,3 % polumjera *ispod* te idealne sfere — sve što se na površinu
postavlja raycastom lebdjelo bi. Zato se pri stvaranju planeta analitički
sudarač zamjenjuje ne-konveksnim `MeshColliderom` nad stvarnom geometrijom,
čime se fizikalna površina izjednačava s vidljivom. Konačno, svaki planet
dobiva jedinstveno ime (`Planet_00`, `Planet_01`...): imena su identitet u
datoteci spremanja, pa bi dva istoimena planeta pri učitavanju tiho spojila
veze i strojeve na pogrešan planet.

*[Slika 6: Dio proceduralno stvorenog svijeta — `docs/slike/slika-06-svemir.png`]*

## 4.3. Sferična gravitacija i kretanje igrača

Gravitacija je podijeljena u dvije uloge. Komponenta `Attractor` određuje
*kamo je dolje*: poravnava os tijela s radijalnim smjerom od središta
planeta, glatkom sfernom interpolacijom rotacije.

```csharp
public void Attract(Transform body, Rigidbody rb)
{
    Vector3 gravityUp = (body.position - transform.position).normalized;
    Quaternion targetRotation =
        Quaternion.FromToRotation(body.up, gravityUp) * body.rotation;
    rb.MoveRotation(Quaternion.Slerp(body.rotation, targetRotation,
        50f * Time.fixedDeltaTime));
}
```

Samo privlačenje izvodi `PlayerController`: Unityjeva ugrađena gravitacija
je isključena, a prema središtu aktivnog planeta dodaje se akceleracija
`Planet.Gravity`. Atraktori svih planeta osim onog na kojem igrač stoji drže
se ugašenima — u protivnom bi igrač, i sam atraktor, hodanjem rotirao
planete.

Najviše razvojnog truda odnio je **surface lock**, mehanizam koji igrača
drži uz površinu. Problem: kad se kapsula igrača gurne u sudarač resursa ili
stroja, PhysX-ovo razdvajanje (*depenetration*) povlači je preko ruba prema
gore, pa se bez dodatne kontrole moglo "popeti" na objekte i trajno ostati
iznad površine. Rješenje mjeri visinu dna kapsule raycastom koji prihvaća
*isključivo* sudarač planeta (objekti na površini nisu tlo) i višak iznad
dopuštene visine `surfaceSkin` (0,15) uklanja, ograničeno na `maxSnapPerStep`
(0,2) po fizikalnom koraku:

```csharp
float altitude = Vector3.Dot(feet - hit.point, up);

if (!_grounded) { _grounded = altitude <= surfaceSkin; return; }
if (altitude <= surfaceSkin) return;

float outward = Vector3.Dot(rig.linearVelocity, up);
if (outward > 0f) rig.linearVelocity -= up * outward;

rig.position = pos - up * Mathf.Min(altitude - surfaceSkin, maxSnapPerStep);
```

Bitna je odluka da visina *ne* oslobađa zaključavanje: ranija je inačica
imala prag iznad kojeg se lock otpuštao, no depenetracija je na visokom
objektu mogla u jednom koraku izbaciti kapsulu iznad praga i igrač bi trajno
"kliznuo" iznad površine. Jedini legitimni izlaz iz prizemljenog stanja
ostao je skok pozicije veći od jednog metra u jednom koraku — a to se događa
samo pri teleportu, oživljavanju ili učitavanju igre. Silazak s ruba litice
sada obavlja sam lock, spuštanjem do 0,2 jedinice po koraku, što je na
malim sferičnim planetima neprimjetno.

Kretanje po ledenim planetima koristi poseban model ubrzanja i usporavanja
(klizanje do zaustavljanja konstantnim usporenjem), a za igrača se u kodu
stvara fizikalni materijal bez trenja — trenje PhysX-a na bridovima
poligonalne sfere inače bi lokalno "žderalo" naslijeđenu brzinu klizanja.
Kamera je čisto prateća, bez vlastite rotacije: stoji iza smjera igrača, s
radijalom planeta kao "gore", a glatkoća praćenja neovisna je o broju sličica
zahvaljujući eksponencijalnom faktoru `1 − e^(−k·Δt)`.

>>> POPUNITI: [Koji ti je problem u razvoju kretanja bio najteži i kako si
    ga dijagnosticirao?]
    Očekivana duljina: 1 odlomak / 100–150 riječi
    Natuknice koje bi trebalo pokriti: komentari u PlayerControlleru
    dokumentiraju cijelu sagu surface locka (uklonjeni ungroundHeight,
    penjanje po objektima) — opiši kako je izgledalo loviti taj bug;
    preimenovanje polja iceAcceleration/iceDeceleration jer su serijalizirane
    vrijednosti iz scene pregazile nove defaulte ("beskonačno" klizanje od
    23 s); poravnanje sudarača s vizualnim modelom robota (model pomaknut
    ~1,3 od pivota) <

## 4.4. Mreža veza među planetima

Mrežom upravlja `ConnectionManager`. Nakon stvaranja svijeta on među svim
parovima planeta unutar dometa (u sceni 2000 jedinica) odabire koje će
parove ponuditi igraču kao *potencijalne veze*, označene totemima na obje
površine. Ponuda se ne gradi pohlepno: najprije se nad svim kandidatima,
uzlazno sortiranima po duljini, gradi **razapinjuće stablo Kruskalovim
algoritmom** s union–find strukturom [10], što
jamči da je svaki planet dosegljiv iz huba lancem totema. Tek se potom
dodaju kratke dodatne veze, i to samo dok su oba kraja ispod mekog limita
`maxPotentialPerPlanet` (3):

```csharp
foreach (var e in edges)
{
    if (Find(e.i) == Find(e.j)) { extras.Add(e); continue; }
    if (!SpawnPotentialPair(all[e.i].transform, all[e.j].transform)) continue;
    parent[Find(e.i)] = Find(e.j);
    degree[e.i]++; degree[e.j]++;
}

foreach (var e in extras)
{
    if (degree[e.i] >= maxPotentialPerPlanet ||
        degree[e.j] >= maxPotentialPerPlanet) continue;
    ...
}
```

Interakcijom s totemom igrač bira jednu od tri razine veze, koje se
razlikuju u cijeni, životnom vijeku i debljini snopa: slaba (besplatna,
60 s), srednja (1 kamen, 180 s) i jaka (1 ruda, 600 s). Veza je vizualno
snop od tri valjka — po jedan uspravni iznad svakog totema i kosi spojni
segment — jer je jedan kosi valjak od vrha do vrha izgledao iskrivljeno na
totemu.

Degradaciju provodi `PlanetConnection`: zdravlje veze (0–100) pada brzinom
`100/životni vijek` u sekundi, a šteta se primjenjuje u fiksnim otkucajima
od 0,25 s umjesto svake sličice — dvije izmjene materijala i događaj na
sabirnici po sličici *po vezi* loše bi skalirali s brojem veza. Boja snopa
prelazi iz zelene preko žute i narančaste u crvenu, a ispod 20 % zdravlja
snop treperi. Nestabilni planeti ubrzavaju propadanje: svaki vulkanski ili
plinoviti kraj veze dodaje faktor 0,5, pa veza s jednim nestabilnim krajem
propada 1,5×, a s dva 2× brže. Kad zdravlje padne na nulu, veza podiže
događaj uništenja i nestaje — a njezini se potencijalni totemi ponovno
aktiviraju, pa se na istom mjestu može graditi iznova. Uz veze, igrač se
može i teleportirati bez veze: cijena teleporta raste s udaljenošću,
množiteljem `1 + ⌊d/2000⌋`.

*[Slika 7: Veza između dva planeta, zdrava (zelena) i kritična (crvena) —
`docs/slike/slika-07a-veza.png`, `slika-07b-veza-kriticna.png`]*

## 4.5. Resursi i rudarenje

Resurse na planete postavlja `ResourceSpawnManager`, pretplaćen na događaj
otkrivanja planeta. Broj primjeraka svakog resursa skalira se s veličinom
planeta — gustoća je definirana po jedinici polumjera u konfiguracijskom
ScriptableObjectu (po tipu planeta), pa veliki planet nosi razmjerno više
resursa:

```csharp
float radius = SurfacePlacement.GetPlanetRadius(planetTransform);
foreach (var entry in config.resources)
{
    int count = Mathf.Max(1, Mathf.RoundToInt(
        Random.Range(entry.minDensity, entry.maxDensity) * radius));
    for (int i = 0; i < count; i++)
        SpawnOne(entry, planetTransform);
}
```

Svaki primjerak nasumično postaje ili trenutno podizljiv (*pickup*) ili
rudarska inačica koja traži alat i vrijeme; vulkanske rune uvijek su rudarska
inačica. Pozicija se bira nasumičnim smjerom (`Random.onUnitSphere`) i
projekcijom na površinu kroz zajednički pomoćni razred `SurfacePlacement` —
jedino kanonsko mjesto u projektu za "postavi objekt na sferu". On raycasta
prema unutra s visine iznad planeta, prihvaća samo pogodak u sudarač samog
planeta te objekt prizemljuje tako da mu *najniža točka stvarne geometrije*
legne na površinu, neovisno o pivotu modela, uz izmjereno upuštanje da ravno
dno na zakrivljenom tlu ne izgleda kao da lebdi.

Rudarenje je model "drži tipku": `Interactor` raycastom pronađe cilj i dok
igrač drži tipku interakcije akumulira vrijeme, množeno faktorom brzine
opremljenog alata:

```csharp
_holdTimer += Time.deltaTime * PlayerToolSystem.GetSpeedMultiplier();
float progress = Mathf.Clamp01(_holdTimer / _currentTarget.HoldTime);
GameEventBus.Raise(new MiningProgressEvent { Progress = progress, IsMining = true });

if (_holdTimer >= _currentTarget.HoldTime)
{
    _currentTarget.Interact();
    ...
}
```

Smije li alat uopće rudariti resurs određuju klasa i rang alata: resurs
definira potrebni alat, a prihvaća se svaki alat iste klase (rudarska /
drvosječna) s jednakim ili višim rangom. Alati su ScriptableObject asseti
(Pickaxe i Axe: 2× brzina, 100 trajnosti, rang 1; Drill: 3×, 150, rang 2;
Rune Drill: 5×, 300, rang 3; Eternal Pickaxe: 3×, beskonačna trajnost).
Trajnost se smanjuje po iskopanom resursu, a istrošeni alat nestaje. Dio
resursa se nakon rudarenja regenerira (drvo 10 s, biljke 5 s, plin 8 s):
umjesto uništavanja, objektu se privremeno sakriju rendereri i sudarači.

*[Slika 8: Rudarenje s trakom napretka i HUD-om — `docs/slike/slika-08-rudarenje.png`]*

## 4.6. Inventar i izrada predmeta

Igra razdvaja dva inventara. `InventorySystem` je neograničeni inventar
materijala — hrpe (*stackovi*) resursa bez limita broja mjesta — dok je
`QuickSlotInventory` traka od devet fiksnih mjesta za alate i strojeve,
birana tipkama 1–9. Razdvajanje ima praktičan razlog: materijali se troše u
količinama i ne zauzimaju "ruke", a predmeti u traci su ono što igrač
aktivno koristi.

Posebnost trake proizlazi iz podatkovnog modela: alat je ScriptableObject
*asset*, dakle jedan dijeljeni objekt za sve primjerke u igri, pa asset ne
može nositi trajnost pojedinog primjerka. Instanca alata je zato samo mjesto
u traci — uz polje predmeta traka drži i paralelno polje trajnosti po mjestu:

```csharp
public const int SlotCount = 9;
[SerializeField] private QuickSlotItem[] slots = new QuickSlotItem[SlotCount];

// Trajnost po slotu — alat je ScriptableObject asset, pa je slot jedina "instanca"
private readonly int[] _durabilities = new int[SlotCount];
```

Izrada predmeta odvojena je od sučelja u servisni razred `CraftingSystem`:
recepti se automatski otkrivaju među assetima u mapi `Resources/Recipes`,
svaki nosi sastojke, rezultat i prag huba od kojeg je dostupan
(`unlockTier`). Transakcija izrade pisana je obrambeno — rezultat se najprije
pokuša smjestiti u traku, a sastojci se troše tek potom, da se pri punoj
traci ne izgube:

```csharp
public static bool TryCraft(CraftingRecipe recipe)
{
    if (!GameManager.TestingMode &&
        (!recipe.IsUnlocked || !recipe.CanAfford())) return false;

    QuickSlotItem result = GetResultItem(recipe);
    if (result == null) return false;
    if (!QuickSlotInventory.Instance.TryAdd(result, out _)) return false;

    if (!GameManager.TestingMode)
        recipe.ConsumeIngredients();
    return true;
}
```

## 4.7. Strojevi i automatizacija

Svaki stroj čine dva dijela: ScriptableObject s podacima (prefab, mjerilo,
intervali, cijene održavanja i popravka, šansa kvara) i MonoBehaviour s
ponašanjem, dodan objektu pri postavljanju. Postavljanje vodi
`MachinePlacer`: tipkom P odabrani se stroj iz trake postavlja na površinu
ispred igrača (točka na radijali, uspravno na normalu), tipkom X vraća u
traku. Vrsta stroja raspoznaje se C# uzorkovanjem tipa nad podatkovnim
assetom — podatkovni tipovi namjerno *nemaju* zajedničku baznu klasu, jer
je upravo tip ono po čemu se grana:

```csharp
switch (item)
{
    case MachineData collector:          TryPlaceCollector(collector, index); break;
    case StorageMachineData storage:     ... break;
    case SmelterMachineData smelter:     ... break;
    // Podklasa mora ići prije TeleporterMachineData case-a.
    case TwoWayTeleporterMachineData twoWay: ... break;
    case TeleporterMachineData teleporter:   ... break;
    ...
}
```

Četiri proizvodna stroja (kolektor, talionica, ekstraktor, uplink) dijele
baznu klasu `ProductionMachine` — zajednički kostur ciklusa u `Update`,
vezanje komponente kvara, popravak i trošak održavanja. Kostur je izvučen u
baznu klasu nakon što se, ručno kopiran u sva četiri stroja, već počeo
razilaziti. Reprezentativan je ciklus ekstraktora, koji pasivno proizvodi
resurse iz atmosfere:

```csharp
protected override void TryCycle()
{
    if (_stored.TotalStacked() >= data.maxStored) { _state = MachineState.Idle; return; }
    if (_breakdown.RollBreakdown())               { _state = MachineState.Broken; return; }
    if (!TryConsumeMaintenance(data.maintenanceCost))
    {
        _state = MachineState.Idle;
        return;
    }
    Produce();
    _state = MachineState.Active;
}
```

Talionica istim ritmom pretvara sirovine iz ulaznog spremnika u prerađene
resurse po receptima (ruda → ingot), a kolektor periodično skuplja resurse s
površine planeta u vlastiti ili povezani skladišni stroj. Strojevi se mogu
pokvariti: komponenta `MachineBreakdown` svaki *radni* ciklus baca kockicu
(prazan stroj se ne troši), a na nestabilnim planetima šansa kvara množi se
s 3. Pokvareni stroj stoji, tamno-crveno obojen, dok ga igrač ne popravi —
uz namjerno asimetričnu ekonomiju: održavanje se plaća iz hub skladišta
(stroj radi sam, daleko od igrača), a popravak iz igračeva inventara (igrač
je fizički uz stroj).

Sve strojeve u svijet postavlja isključivo statička tvornica
`MachineFactory`, koja je ujedno **jedina** tablica rezervnih boja i
mjerila po vrsti stroja. To je izravna posljedica otklonjenog defekta:
`MachinePlacer` i `SaveSystem` prije su držali svaki svoju kopiju tablice
boja, kopije su se razišle i dvosmjerni su se teleporteri nakon učitavanja
pojavljivali u boji običnih. Popravak je jedan zajednički pristupnik:

```csharp
public static Color TeleporterColorFor(TeleporterMachineData data) =>
    data is TwoWayTeleporterMachineData ? TwoWayGateColor : TeleporterColor;
```

Teleporteri su dvije vrste. Obični se gradi u paru automatski: ulaz pred
igračem, izlaz na strani huba okrenutoj planetu; dvosmjerni se postavlja u
dva koraka na dva različita planeta, a predmet se iz trake troši tek kad
oba kraja postoje — do tada je ulaz u stanju mirovanja, a odustajanje ga
uklanja bez povrata (predmet nikad nije ni potrošen).

*[Slika 9: Strojevi na planetu, među njima pokvarena talionica s crvenim
tonom — `docs/slike/slika-09-strojevi.png`]*

## 4.8. Hub i progresija

Gospodarstvo igre slijeva se u hub skladište (`HubStorage`). Njegov je
kapacitet zbroj baznog (100) i bonusa svih otključanih pragova:

```csharp
[SerializeField] private int maxCapacity = 100;

// Bazni kapacitet + bonus otključanih hub pragova.
public int MaxCapacity => maxCapacity + HubProgress.StorageBonus;
```

Da igrač ne bi svaki resurs nosio pješice, gradi `UplinkMachine`: tipkom E
istovari cijeli inventar u međuspremnik stroja, koji potom svakih 5 sekundi
šalje po 2 predmeta u hub skladište; ako je skladište puno, ostatak čeka
sljedeći ciklus. Hub skladište tako postaje središnji resursni bazen iz
kojeg se plaćaju održavanje strojeva, zahtjevi veza i — najvažnije —
pragovi napretka.

Pragove vodi statički razred `HubProgress` (opisani u 3.4): svaki je prag
definiran zahtjevima, bonusom skladišta i popisom recepata koje otključava,
objedinjenima u jednoj strukturi nakon što su ranije živjeli kao tri ručno
sinkronizirana polja. Otključavanje troši resurse iz hub skladišta i podiže
događaj na sabirnici, na koji reagiraju sučelja i zvuk; otključavanjem
petog praga pobjednički ekran završava igru. Recepti su vezani na pragove
poljem `unlockTier` na assetu recepta, pa dodavanje novog recepta ne traži
nikakvu izmjenu koda.

## 4.9. Opasnosti, neprijatelji i zdravlje igrača

Zdravlje igrača (100) vodi `PlayerHealth`. Sva šteta prolazi kroz jednu
metodu s prozorom neranjivosti od 0,5 s nakon svakog pogotka, koji ujedno
određuje ritam svih izvora štete:

```csharp
public void TakeDamage(float amount)
{
    if (IsDead || amount <= 0f) return;
    if (Time.time < _invulnerableUntil) return;

    _invulnerableUntil = Time.time + damageInvulnerability;
    CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
    ...
    if (CurrentHealth <= 0f) Die();
}
```

Smrt ne završava igru: simulacija se zamrzne, a tipkom R igrač se oživljava
punog zdravlja na aktivnom totemu za oživljavanje (podrazumijevano na hubu),
sa sačuvanim inventarom — cijena pogibije je izgubljeni put, ne napredak.
Oživljavanje umjesto ponovnog učitavanja scene i ovdje je nužno: reload bi
izgubio proceduralni svijet i izgrađenu mrežu.

Izvori štete vezani su uz tipove planeta. Na vulkanskim planetima spawna se
2–5 lava-zona koje kruže površinom po velikoj kružnici — os rotacije okomita
je na radijalu točke spawna, a kutna brzina izvodi se iz linearne da zone na
malim planetima ne obilaze sporije. Zona nanosi 15 štete u sekundi, uz
zanimljiv detalj: sve zone dijele *jedan statički* otkucaj štete, jer je
svaka zona s vlastitim timerom u preklopu dviju zona štetu udvostručavala.
Plinoviti planeti štete cijelom površinom: samopokrenuti sustav
`GasPlanetAtmosphere` svake sekunde oduzima 5 zdravlja igraču koji na takvom
planetu stoji bez plinske maske u traci, uz poček od 3 s nakon dolaska da
igrač u prolazu stigne otići neozlijeđen. Maska je zaštitna oprema — tipkom
P stavlja se na glavu i vrijedi dok god je u traci.

Neprijatelji su mobovi vezani uz planet: svaki obični planet pri otkrivanju
dobiva 3–5 mobova koji miruju dok im se igrač ne približi na 12 jedinica,
tada ga progone po površini konstantnom brzinom 2,5 — namjerno malo manjom
od igračeve 3, pa se od njih uvijek može pobjeći — i odustaju tek na 18
jedinica, da potjera ne treperi na rubu radijusa detekcije. Štetu (10)
nanose dodirom, ritmom kojeg ograničava prozor neranjivosti.

*[Slika 10: Vulkanski planet s lava-zonom i igračem na površini —
`docs/slike/slika-10-vulkanski.png`]*

## 4.10. Korisničko sučelje

Cijelo sučelje izgrađeno je uGUI sustavom, ali bez ijednog UI elementa u
sceni ili prefabu: svaki panel programski gradi svoju hijerarhiju objekata
(`RectTransform`, `Image`, `Button`, `TextMeshProUGUI`) u `Awake`, po istom
obrascu kao ostali samopokrenuti sustavi. Glavni izbornik tako pri
pokretanju stvori vlastiti canvas iznad svih ostalih i služi i kao početni
ekran i kao pauza; njegov ekran s kontrolama imena tipki čita iz `GameKeys`,
jedinog izvora istine za raspored tipki.

S desetak panela koji se otvaraju preko igre (izrada, inventar, karta mreže,
računalo huba...) pojavio se klasičan problem: tko upravlja kursorom i
gasi upravljanje igračem dok je sučelje otvoreno? Isprva je isti blok koda
bio kopiran u jedanaest panela, a "je li neki panel otvoren" izvodilo se iz
stanja kursora kao implicitne globalne zastavice — zadnji panel koji se
zatvorio "pobjeđivao" je. Rješenje je razred `UiFocus`, jedini vlasnik tog
protokola, s brojačem otvorenih panela:

```csharp
public static void Release(PlayerController pc, PlayerCamera cam, Interactor it)
{
    _lastReleaseFrame = Time.frameCount;
    _openPanels = Mathf.Max(0, _openPanels - 1);
    if (_openPanels > 0) return;

    Apply(pc, cam, it, uiOpen: false);
}
```

Upravljanje se igri vraća tek kad se zatvori *posljednji* panel, a zapamćeni
broj sličice rješava utrku istog okvira: panel koji se na Esc zatvori i
glavni izbornik koji bi se na isti pritisak otvorio.

Karta mreže (`NetworkMapUI`) je 2D projekcija svijeta: pozicije planeta
normaliziraju se po X i Z osi u pravokutnik karte, planeti se crtaju kao
kružići (hub veći), a veze kao rotirani i rastegnuti `Image` elementi
obojeni prema zdravlju od crvene do zelene. Osvježavanje je namjerno
prigušeno: događaj promjene zdravlja stiže svaku sličicu po svakoj vezi
koja propada, pa se puna obnova karte izvodi najviše svakih 0,25 s.

*[Slika 11: Karta mreže i glavni izbornik — `docs/slike/slika-11a-karta-mreze.png`,
`slika-11b-glavni-izbornik.png`]*

## 4.11. Zvuk i vizualni efekti

Projekt ne sadrži nijednu zvučnu datoteku: svih 14 zvučnih efekata i
ambijentalna glazba sintetiziraju se pri pokretanju. Podjela odgovornosti je
stroga — `AudioSynth` je čisti DSP kod bez ovisnosti o sceni (statičke
funkcije nad poljem uzoraka), a `AudioManager` orkestracija: izvori zvuka,
pretplate na sabirnicu događaja i pravila prigušivanja. Temeljni gradivni
blok je sinusni oscilator s klizanjem frekvencije, obavijnicom snage i
kratkim napadom od 4 ms (bez njega početak tona "pucketa"):

```csharp
for (int i = 0; i < len; i++)
{
    float t = (float)i / len;
    float freq = Mathf.Lerp(startFreq, endFreq, t);
    phase += 2.0 * Math.PI * freq / SampleRate;

    float env = Mathf.Pow(1f - t, decayPow) * Mathf.Min(1f, i / attackSamples);
    buf[start + i] += amp * env * (float)Math.Sin(phase);
}
```

Efekti su "recepti" nad tim blokovima: udarac rudarenja je niski sinus uz
kratki prasak filtriranog šuma, smrt dugi padajući ton s vibratom, teleport
brzi uzlazni sweep. Glazba je zahtjevnija — 26 sekundi stereo ambijenta s
dva akorda (d-mol i B-dur) koji se izmjenjuju, slojevima detuniranih padova
i "svemirskim vjetrom" (šum kroz niskopropusni filtar sporo mijenjanog
otvora) — pa se računa na pozadinskoj niti da ne koči pokretanje igre, a rep
se pretapa u početak da točka ponavljanja bude nečujna.

Efekti čestica slijede isti princip: `VfxManager` u kodu stvara četiri
`ParticleSystem` objekta (iskre rudarenja, teleport, prašina postavljanja,
dim kvara) i sam crta mekanu kružnu teksturu čestice (bez nje su čestice
kvadrati). Emisija je ručna, po smjeru normale površine — jedan sustav tako
pokriva sve smjerove, što je na sferičnim planetima nužno jer je "gore"
svugdje drugačije.

## 4.12. Spremanje i učitavanje

Igra se sprema u jednu JSON datoteku (`webofplanets_save.json` u
`Application.persistentDataPath`), serijaliziranu Unityjevim
`JsonUtility`jem. Sustav je podijeljen u četiri partial-datoteke po
odgovornosti: shema datoteke (DTO), snimanje, obnavljanje i zajednički
pomoćnici. DTO razredi su *format datoteke* — polja se ne smiju preimenovati
bez migracije. Identitet objekata u datoteci nose imena: planeti svojim
jedinstvenim imenom, asseti imenom datoteke, a međusobne veze strojeva
(kolektor→skladište, teleporterski par) indeksom u listi strojeva.

Središnja odluka cijelog sustava: **učitavanje ne učitava scenu ponovno**.
Runtime-stvoreni sustavi (izbornik, zvuk, efekti) reload bi izbrisao, pa se
umjesto toga proceduralni svijet ruši u mjestu i ponovno gradi *istim
kodom kojim je i nastao*:

```csharp
// 1) Sruši proceduralni svijet (Destroy se izvršava na kraju framea).
cm.ResetForLoad();
DestroyAll<CollectorMachine>();
...
foreach (var ps in data.planets)
{
    Transform planet = planetCreator.SpawnPlanetFromSave(
        ps.name, ps.position, ps.scale, ps.gravity, (PlanetType)ps.type);
    if (rsm != null) rsm.MarkProcessed(planet);
}

yield return null; // stari objekti stvarno uništeni; Planet.Start odrađen
yield return null; // hazardi/mobovi spawnani, PhysX poze sinkane
```

Tu se isplaćuje arhitektura iz 4.1: svaki obnovljeni planet u svom `Start`
podigne događaj otkrivanja, na koji se lava-zone i mobovi spawnaju sami,
istim kodom kao pri stvaranju novog svijeta. Iznimka su resursi — oni se
spremaju pojedinačno (predmet, pozicija, inačica), pa se svježe spawnanje za
učitane planete preskače oznakom `MarkProcessed`. Strojevi se obnavljaju
kroz istu tvornicu `MachineFactory` kojom se i postavljaju, a asseti se
razrješavaju po tipu i imenu među već učitanima, s predmemorijom jer se
razrješavanje poziva u petljama.

Redoslijed koraka obnavljanja je osjetljiv i dokumentiran: planeti →
spremljeni resursi → potencijalni totemi → aktivne veze → strojevi (pa tek
onda njihove međusobne veze) → inventari i prag huba → igrač. Dvije pauze
od jedne sličice u sredini nisu ukras: Unityjev `Destroy` izvršava se na
kraju sličice, pa bi se bez čekanja novi svijet gradio dok stari još
postoji. Namjerno se *ne* sprema sve: napredak rudarenja u tijeku i timeri
regeneracije resursa izgube se pri učitavanju (resurs u regeneraciji vraća
se vidljiv) — svjesno pojednostavljenje formata, ne propust.

---

**[Za popuniti u ovom poglavlju]**
- POPUNITI blok u 4.3: najteži problem u razvoju kretanja (surface lock,
  klizanje po ledu, poravnanje sudarača).
- Izvor: [10] (Cormen i dr. — predložen unos u Literaturi). Po potrebi mogu
  na konkretna mjesta dodati i Unity dokumentaciju za
  RuntimeInitializeOnLoadMethod / JsonUtility / ParticleSystem.
- Slike 6–11 snimljene su kroz unityMCP i povezane u tekstu
  (`docs/slike/`). Ako želiš druge kadrove, mogu ponoviti snimanje.
- Po želji: kratko potpoglavlje o testiranju/alatima za provjeru (audit
  menu itemi u Editoru) — reci ako želiš da ga dodam kao 4.13.

# 5. Zaključak

Cilj rada — razviti funkcionalnu 3D igru preživljavanja i automatizacije i
dokumentirati njezina programska rješenja — ostvaren je u cijelosti. Igra
sadrži sve planirane središnje sustave: proceduralno generiran svijet od
tridesetak sferičnih planeta pet tipova, kretanje sa sferičnom gravitacijom,
mrežu veza s degradacijom i jamstvom povezivosti, gospodarski krug od ručnog
rudarenja do automatizirane proizvodnje, progresiju kroz pet pragova huba s
uvjetom pobjede te potpuno spremanje i učitavanje stanja. Uz to je nekoliko
odluka igru učinilo tehnički samostalnom: zvuk se sintetizira proceduralno
bez ijedne zvučne datoteke, sučelje i efekti grade se programski, a scena
ostaje gotovo prazna.

>>> POPUNITI: [Osvrt na proces: što ti je razvoj dao, što je bilo najteže,
    što bi danas napravio drugačije?]
    Očekivana duljina: 1–2 odlomka / 150–250 riječi
    Natuknice koje bi trebalo pokriti: kod dokumentira nekoliko krupnih
    prepravki (surface lock, izvlačenje ProductionMachine bazne klase nakon
    razilaženja kopija, konsolidacija tipki u GameKeys, UiFocus umjesto 11
    kopiranih blokova) — koje su ti lekcije iz toga; koliko su pomogli
    interni auditi i planovi (AUDIT/PLAN dokumenti u projektu); bi li opet
    birao runtime bootstrap pristup <

Prostora za daljnji razvoj ima i on je dijelom već pripremljen u kodu:
sabirnica događaja sadrži rezervirano sučelje za artefakte, drevne veze,
automatske transportne rute i sekundarne hubove — mehanike predviđene
dizajnerskim dokumentom, a svjesno odgođene radi opsega. Prirodni su koraci
i proširenje proceduralne raznolikosti planeta (teren, špilje), balansiranje
ekonomije na temelju testiranja s igračima te priprema samostalne PC verzije
za distribuciju.

>>> POPUNITI: [Završna rečenica/odlomak zaključka — osobni ton po želji]
    Očekivana duljina: 2–3 rečenice
    Natuknice koje bi trebalo pokriti: čime si najzadovoljniji; planiraš li
    nastaviti razvoj igre nakon obrane <

---

# 6. Literatura

*(IEEE stil; poredak po prvom pojavljivanju u tekstu. Datume pristupa
dopuniti pri predaji. Unosi [1] i [9] traže tvoj odabir konkretnog izvora.)*

[1] >>> POPUNITI: odaberi izvještaj o industriji videoigara (npr. Newzoo
    Global Games Market Report ili Statista) — dodaj točan naslov, godinu
    i URL <

[2] Unity Technologies, "Unity Manual", [Mrežno]. Dostupno:
    https://docs.unity3d.com/Manual/ [Pristupljeno: __.__.2026.]

[3] Unity Technologies, "Universal Render Pipeline overview", [Mrežno].
    Dostupno: https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.3/manual/
    [Pristupljeno: __.__.2026.]

[4] Microsoft, "C# language documentation", [Mrežno]. Dostupno:
    https://learn.microsoft.com/en-us/dotnet/csharp/ [Pristupljeno: __.__.2026.]

[5] Unity Technologies, "MonoBehaviour — Order of execution for event
    functions", [Mrežno]. Dostupno:
    https://docs.unity3d.com/Manual/ExecutionOrder.html [Pristupljeno: __.__.2026.]

[6] Unity Technologies, "ScriptableObject", [Mrežno]. Dostupno:
    https://docs.unity3d.com/Manual/class-ScriptableObject.html
    [Pristupljeno: __.__.2026.]

[7] Unity Technologies, "Input System Manual", [Mrežno]. Dostupno:
    https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/manual/
    [Pristupljeno: __.__.2026.]

[8] Unity Technologies, "Unity UI (uGUI)", [Mrežno]. Dostupno:
    https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/
    [Pristupljeno: __.__.2026.]

[9] >>> POPUNITI: odaberi tekst o žanru factory/automation igara (članak,
    recenzija ili akademski rad; po želji dodatno referenca na Super Mario
    Galaxy kao uzor za sferičnu gravitaciju) <

[10] T. H. Cormen, C. E. Leiserson, R. L. Rivest i C. Stein, *Introduction
    to Algorithms*, 4. izd., Cambridge, MA: MIT Press, 2022. (poglavlje o
    minimalnim razapinjućim stablima)

---

# 7. Popis slika

*(Radni popis — brojevi i stranice dodjeljuju se pri prijelomu u Word.
Datoteke su u `docs/slike/`; slike 1–3 treba snimiti ručno jer prikazuju
sučelje Editora, ostale su snimljene.)*

1. Sučelje Unity Editora s otvorenim projektom (§2.1) — **snimiti ručno**
2. Mapa Data/ sa ScriptableObject assetima (§2.2) — **snimiti ručno**
3. Asset PlayerInputActions u Input Actions editoru (§2.3) — **snimiti ručno**
4. Dijagram osnovne petlje igre (§3.2) — `slika-04-osnovna-petlja.svg`
5. Planeti svih pet tipova (§3.3) — `slika-05a` … `slika-05e`
6. Dio proceduralnog svijeta (§4.2) — `slika-06-svemir.png`
7. Veza: zdrava i kritična (§4.4) — `slika-07a-veza.png`, `slika-07b-veza-kriticna.png`
8. Rudarenje s trakom napretka (§4.5) — `slika-08-rudarenje.png`
9. Strojevi, uklj. pokvareni (§4.7) — `slika-09-strojevi.png`
10. Vulkanski planet s lava-zonom (§4.9) — `slika-10-vulkanski.png`
11. Karta mreže i glavni izbornik (§4.10) — `slika-11a-karta-mreze.png`, `slika-11b-glavni-izbornik.png`

Dodatno snimljeno, po želji: početak igre na hub planetu —
`slika-00-hub-pocetak.png` (može poslužiti u §3.1 ili §4.1).

# 8. Popis tablica

1. Tipovi planeta i njihovi resursi (§3.3)
2. Pragovi napretka huba (§3.4)

# 9. Prilozi

Uz rad se prilažu razvijena igra (izvršna verzija za PC) i cjelokupni Unity
projekt sa svim komponentama korištenima u razvoju.

>>> POPUNITI: [Kako ćeš priložiti igru i projekt — repozitorij (GitHub URL),
    medij ili arhiva? Dodaj poveznicu/napomenu ovdje.] <

## Popis korištenog sadržaja trećih strana

Igra koristi gotove 3D modele i teksture iz besplatnih paketa navedenih u
nastavku; sav programski kod, zvučni efekti, glazba i efekti čestica
izrađeni su u sklopu ovog rada.

| Sadržaj u projektu | Izvor | Autor | Licenca |
|---|---|---|---|
| SpaceKit — modeli svemirske opreme i likova (uklj. model neprijatelja) | Kenney, *Space Kit*, kenney.nl/assets/space-kit | Kenney | CC0 |
| Graveyard — modeli ruševina i rekvizita | Kenney, *Graveyard Kit*, kenney.nl/assets/graveyard-kit | Kenney | CC0 |
| Tekstura Jupitera (`8k_jupiter.jpg`) | Solar System Scope, solarsystemscope.com/textures | Solar System Scope | CC BY 4.0 |
| Drill — model bušilice (`SM_Drill_01`) | Sketchfab, *Drill "Soviet"*, sketchfab.com/3d-models/drill-soviet-659d98a6051b4438a66a30b96931234e | Sergey Khanin | CC BY 4.0 |
| Gas — plinska maska, cilindar, spremnik | Sketchfab — [POPUNITI: poveznice iz Sketchfab › Downloads] | [POPUNITI] | [POPUNITI] |
| Magma — blokovi magme i runski kamen | Sketchfab — [POPUNITI] | [POPUNITI] | [POPUNITI] |
| PlanetTextures — teksture ledenog, vulkanskog i kamenog planeta | Sketchfab — [POPUNITI] | [POPUNITI] | [POPUNITI] |
| PlanetModels — modeli planeta ("Planeta", "Qo'noS") | Sketchfab — [POPUNITI] | [POPUNITI] | [POPUNITI] |
| Forest — modeli vegetacije (105 modela) | [POPUNITI: paket nije identificiran — provjeri povijest preuzimanja] | [POPUNITI] | [POPUNITI] |
| ResourceModels — modeli resursa i predmeta (79 modela) | [POPUNITI: paket nije identificiran] | [POPUNITI] | [POPUNITI] |

Napomene: CC BY licence zahtijevaju navođenje autora — točne poveznice i
autore preuzetih Sketchfab modela pronaći u vlastitom Sketchfab računu
(Profil → Downloads). Model "Qo'noS" je fan-model planeta iz franšize Star
Trek — za javnu distribuciju igre razmotriti zamjenu.
