# Struktura UI poglavlja završnog rada (2026-07-31)

Plan kako 18 UI skripti pokriti kroz 6 stvarno objašnjenih, po mentorovoj uputi:
važnije/zanimljivije detaljno, varijante skupno kroz jednog predstavnika, trivijalne
preskočiti. Redoslijed sekcija ide od zajedničkih obrazaca prema konkretnim ekranima,
tako da se svaki obrazac objasni jednom i kasnije samo referencira.

## X.1 Zajednička arhitektura UI sustava

Uvodna sekcija, bez pojedinačnih ekrana. Ovdje se jednom objasne obrasci koje
sve ostale sekcije samo referenciraju:

Cijeli UI (17 od 18 skripti) gradi svoju uGUI hijerarhiju u kodu (`BuildUI()` s
`new GameObject` + `AddComponent`), s TextMesh Pro za sav tekst i rich-textom
umjesto višestrukih Text objekata. Razlog je isti kao kod runtime bootstrapa
ostalih sustava: izbjegavanje izmjena scene. `MiningProgressUI` je jedina
Inspector-wired iznimka — iskoristiti je kao kontra-primjer i usporedbu pristupa.

**UiFocus** ovdje zaslužuje najviše prostora — najbolja arhitekturna priča u
projektu: prije je isti ~6-linijski blok (kursor, input, Interactor) bio kopiran
u 11 panela, a "je li UI otvoren" se izvodilo iz `Cursor.lockState` kao implicitnog
globalnog flaga — zadnji zatvoreni panel je pobjeđivao, što je rađalo workaroundove
(VictoryUI je silom zatvarao tuđe panele). Zamjena: eksplicitni brojač otvorenih
panela (Acquire/Release), `ReleasedThisFrame` za race unutar istog framea i
`SubsystemRegistration` reset statika. Tri citabilne teme: ref-counting za
ugniježđene modale, same-frame input race, statika i "Enter Play Mode without
domain reload".

Spomenuti i: `GameKeys` kao jedini izvor istine za imena tipki u hint tekstovima
(DRY — prije treća ručno sinkronizirana kopija rasporeda), te namjerno korištenje
unscaled vremena gdje UI mora raditi pod `timeScale == 0` (toasti, damage flash).

## X.2 Obitelj ItemListUI (Template Method)

Detaljno: **ItemListUI** (apstraktna baza) + **InventoryUI** kao izvedenica.
Skupno u 2-3 rečenice: HubStorageUI, StorageInventoryUI.

Najbolji primjer refaktoriranja u projektu, s dokumentiranim dokazom zašto:
tri ~95% identična filea su već imala drift (Keyboard null-guard samo u jednom,
font naslova 18 vs 20). Baza drži izgradnju panela, scroll, input-mode i
integraciju s UiFocusom; izvedenice samo izvor redaka, naslov i donji gumb
(HubStorageUI 59 linija, StorageInventoryUI 55, InventoryUI 35). Vrijedi
spomenuti i ograničenje refaktoriranja: imena klasa i serijaliziranih polja
morala su ostati jer ih scena referencira — stvaran inženjerski uvjet, ne
akademski detalj. InventoryUI pokazuje override koji PROŠIRUJE bazni Update
(toggle tipka + `GameManager.IsPlaying` gate protiv zaključavanja kursora iza
main menija).

## X.3 HUD overlayi vođeni eventima

Detaljno: **AlertsUI**. Skupno: HealthUI, HotbarUI, MiningProgressUI.

Svi rade po istom obrascu: pretplata na GameEventBus u OnEnable, odjava u
OnDisable, osvježavanje prikaza na event — gameplay sustavi ne znaju da UI
postoji. AlertsUI je najbogatiji predstavnik: edge-triggered vs level-triggered
problem (kritična veza raise-a event svaki frame ispod praga, toast se želi samo
pri ULASKU u kritično stanje) riješen HashSet latchom s histerezom, cooldown za
storage-full, FIFO limit toastova, unscaled fade.

Iz ostalih izvući po jednu rečenicu-dvije: HotbarUI — širina durability bara kroz
`anchorMax.x` jer `Image.Type.Filled` bez sprite-a ignorira fillAmount (citabilan
engine-workaround); HealthUI — null-guard za event koji stigne prije BuildUI
(lifecycle hazard); MiningProgressUI — kontra-primjer iz X.1. Usporedba dviju
strategija osvježavanja (mutacija keširanih referenci kod HotbarUI/HealthUI vs
ruši-i-gradi kod CraftingUI/NetworkMapUI) je dobar zaključni paragraf sekcije.

## X.4 Modalni dijalozi i paneli hub računala

Detaljno: **HubProgressUI** (zbog dirty-check obrasca). Skupno: ComputerMenuUI,
ConnectionChoiceUI, MachineTeleporterUI.

HubProgressUI nema evente (Uplink asinkrono puni skladište) pa polla — ali puni
rebuild panela nije besplatan, pa gradi "potpis vidljivog stanja" (string prag +
stanje skladišta) i preskače rebuild kad se potpis nije promijenio: ručno
change-detection rješenje kao alternativa event pretplati, izvrstan materijal za
diskusiju. Uz to: ComputerMenuUI kao čisti navigacijski čvor koji HubProgressUI
sam AddComponenta (runtime injection umjesto scene wiringa); MachineTeleporterUI
kao jedini dijalog s callback API-jem (`Action<Transform>` continuation-passing,
prvoklasan cancel put); ConnectionChoiceUI u rečenici (affordability kroz boje,
closure-capture fix u petlji gumba).

## X.5 Složeni ekrani: CraftingUI i NetworkMapUI

Oba detaljno — najveći fileovi (447 i 404 linije) i nose najviše "mesa".

**CraftingUI**: odvajanje pogleda od logike (transakcija živi u CraftingSystemu,
UI dodaje zvuk/refresh/poruku); ručni layout bez VerticalLayoutGroup/ContentSizeFitter
s obrazloženjem; reparent-na-null prije Destroy zbog odgođenog uništavanja;
AUDIT P1 priča o lokalnom freeCrafting flagu zamijenjenom centralnim
`GameManager.TestingMode`. Uz njega u 2-3 rečenice **ItemInfoUI**: veliki
pattern-matching switch nad tipovima podataka, s dokumentiranim hazardom da
`TwoWayTeleporterMachineData` mora ići prije `TeleporterMachineData` (first-match
po lancu nasljeđivanja).

**NetworkMapUI**: graf planeta i veza (normalizirana world X/Z projekcija, bridovi
kao rotirani Image quadovi, boja po zdravlju). Tri slojne optimizacije za
diskusiju o performansama: vremenski throttle health eventa (0.25 s), dirty flag
koji N eventova sažme u max jedan rebuild po frameu, lijena izgradnja legende tek
na promjenu zooma (izbjegnuta alokacija stringa svaki frame). Plus pretplata samo
dok je mapa otvorena + OnDestroy odjava (curenje na statičkom busu).

## X.6 Samopokretajući ekrani: MainMenuUI i VictoryUI

Kratka sekcija — bootstrap obrazac je već objašnjen u arhitekturnom poglavlju,
ovdje samo primjena: vlastiti canvas runtime, dokumentirani sortingOrder ugovor
(VictoryUI 90 — iznad HUD-a, ispod pauze; MainMenu 100), `_loading` guard da Esc
ne zatvori meni usred učitavanja. VictoryUI ima lijepu priču o djelomično
umirovljenom workaroundu: prisilno zatvaranje tuđih panela je izgubilo razlog
(UiFocus), ali vizualni dio ostaje — komentar čuva povijest odluke.

## Tablica pokrivenosti (za vlastitu evidenciju)

| Skripta | Sekcija | Razina |
|---|---|---|
| UiFocus | X.1 | detaljno |
| ItemListUI + InventoryUI | X.2 | detaljno |
| HubStorageUI, StorageInventoryUI | X.2 | skupno |
| AlertsUI | X.3 | detaljno |
| HealthUI, HotbarUI, MiningProgressUI | X.3 | skupno (1-2 rečenice) |
| HubProgressUI | X.4 | detaljno |
| ComputerMenuUI, ConnectionChoiceUI, MachineTeleporterUI | X.4 | skupno |
| CraftingUI | X.5 | detaljno |
| ItemInfoUI | X.5 | kratko uz CraftingUI |
| NetworkMapUI | X.5 | detaljno |
| MainMenuUI, VictoryUI | X.6 | kratko (obrazac iz X.1) |

## Prijedlog slika

Screenshot po sekciji: hotbar + health traka (HUD), otvoren inventar (ItemListUI),
toast upozorenja, crafting panel s ItemInfo tooltipom, mapa mreže (idealno sa
zdravom i kritičnom vezom), victory ekran. Za X.1 umjesto screenshota dijagram:
UiFocus brojač s dva otvorena panela (tko drži kursor/input).
