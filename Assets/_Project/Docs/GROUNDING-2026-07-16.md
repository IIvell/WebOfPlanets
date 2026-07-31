# Prizemljenje objekata na površinu planeta — stanje 16.7.2026.

Zadatak: objekti (resursi, strojevi, markeri veza) su nekad clippali u planet, a nekad
lebdjeli iznad njega. Cilj: da stoje što realističnije na površini.

## Status: velikim dijelom riješeno, čeka finalnu provjeru auditom

Zadnji audit (prije nadogradnje mjerenja): **251/266 objekata sjedi ispravno.**
Preostalih 15 nalaza — vidi "Sutra nastaviti" dolje.

## Nađeni uzroci i popravci

1. **Strojevi/totemi/teleporteri se nisu prizemljivali** — pivot prefaba sjedao je
   točno na hit točku. → `MachinePlacer.SpawnObject` sada prima `planet` i zove
   `SurfacePlacement.GroundToSurface` (dno stvarne geometrije na površinu, radi za
   bilo koji pivot). Svi pozivi ažurirani (collector, storage, smelter, extractor,
   uplink, oba teleportera, respawn totemi).
2. **Resursi su se korigirali samo uz `pivotAtMeshCenter` flag** + scene vrijednost
   `surfaceOffset = 0.1` ih je dizala iznad tla. → Bezuvjetno prizemljenje u oba
   spawnera (`ResourceSpawnManager`, `HubResourceSpawner`); duple `SnapPivotToBase`
   kopije obrisane; flag `pivotAtMeshCenter` uklonjen iz `Item.cs`; polje
   preimenovano `surfaceOffset`→`surfaceGap` (namjerno ruši staru scene vrijednost —
   editor drži scenu u memoriji pa se scena ne smije uređivati na disku).
3. **`GroundToSurface`** (novo u `SurfacePlacement.cs`): najniža točka geometrije po
   stvarnim vrhovima (Read/Write meshevi, keširano po meshu) ili po kutovima
   lokalnog mesh boundsa (tight OBB — world AABB rotiranog objekta laže!), plus
   namjerni "sagitta" ukop `min(w²/2R, 0.3·visine)` da ravno dno ne lebdi na
   rubovima zakrivljene kugle.
4. **Hub je imao CONVEX MeshCollider** (hull ≤255 poligona premošćuje udoline, siječe
   brda → igrač i objekti lebde/tonu baš na Hubu). → `Planet.Awake` runtime prebacuje
   u non-convex; `Planet.fbx.meta` dobio `isReadable: 1` (bez toga cook u buildu pada).
5. **Raycast na površinu često je promašivao** (origin točno na radijusu; plus
   `autoSyncTransforms` je u projektu ISKLJUČEN a world-gen raycasta 1 frame nakon
   kreiranja planeta → collideri u PhysX-u na staroj pozi). → Novi kanonski
   `SurfacePlacement.GetSurfacePoint`: origin s marginom +20, filtrirano na planet,
   na promašaj `Physics.SyncTransforms()` + retry, tek onda analitički fallback.
6. **Markeri veza**: `PlanetConnection.SurfacePoint` je računao radijus iz
   `localScale.x*0.5` — Hub ima localScale 1000 uz stvarni radijus ~19, pa su
   hub-side markeri visjeli ~480 jedinica u svemiru. → Prebačeno na zajednički
   `GetSurfacePoint`; markeri se sada i prizemljuju i orijentiraju po stvarnoj hit
   normali. `HubBase.SnapToSurface` i `GameManager.SpawnHubTotem` fallback također
   idu kroz isti helper. Teleport (`PlanetCreator.TeleportToPlanet`, no-marker grana)
   koristi `GetPlanetRadius` umjesto localScale.
7. **Bijeli "robot" sa screenshotova = model igrača** (Grab-Bot je samo vizual Player
   riga) — njegovo lebdenje/tonjenje na Hubu rješava točka 4. Ako i dalje konstantno
   odstupa na SVIM planetima, to je ručni offset vizuala (editor meni
   "Grab-Bot Pivot → Raise/Lower 0.1m").
8. **Generirani planeti (svi osim Huba): analitički SphereCollider vs poligonalni
   mesh.** `PlanetCreator.SpawnPlanet` = `CreatePrimitive(Sphere)`: collider je
   savršena kugla, a vidljivi mesh (768 trokuta) između vrhova pada do **1.32% R
   ISPOD nje** (izmjereno u Unity 6000.3.10f1: R=50 → raycast 50.000, mesh 49.341 =
   0.66 lebdenja). Sve prizemljeno raycastom sjedalo je na nevidljivu kuglu i
   lebdjelo, a audit to NE VIDI jer mjeri po istom collideru — zato je audit bio
   čist (2/274) dok su objekti na generiranim planetima vidljivo lebdjeli. Ista
   klasa problema kao convex hull na Hubu (točka 4), suprotni smjer. → U
   `SpawnPlanet` SphereCollider zamijenjen non-convex MeshColliderom vidljivog
   mesha. `enabled=false` PRIJE `Destroy` je nužan: resursi se spawnaju
   event-lančano ISTI frame (Planet.Start → RaisePlanetDiscovered →
   ResourceSpawnManager), a Destroy je odgođen do kraja framea pa bi aktivna kugla
   i dalje pobjeđivala raycast (empirijski potvrđeno u standalone buildu; built-in
   sphere mesh je čitljiv i u playeru, cook svih 30 planeta ~1 ms jer PhysX kešira
   po meshu).
9. **Nakon točke 8 sagitta formula bi PRETJERANO ukopavala**: `w²/2R` modelira
   zakrivljenost, ali tlo pod objektom je sada ravan trokut — objekt unutar jednog
   trokuta treba ukop 0. → `GroundToSurface` više ne računa analitički: novi
   `SurfacePlacement.ComputeSink` raycasta 4 rubne točke footprinta i ukopava za
   najveći IZMJERENI razmak ruba od tla (cap 30% visine ostaje; analitička formula
   samo kao fallback kad svi rubni raycastovi promaše). SurfaceAudit koristi isti
   `ComputeSink` pa dijagnostika i dalje mjeri identično kao prizemljenje; uz to
   audit tolerira namjerno ukopavanje radijalno orijentiranih strojeva na nagnutim
   trokutima (tilt slack ~w·(tanθ+sinθ)), inače bi širi strojevi na nagibu ispadali
   lažni "UTONUO". Bonus:
   ovo pokriva i nagnute trokute pod strojevima (MachinePlacer orijentira strojeve
   radijalno, ne po normali trokuta — do ~8° nagiba na 768-trokutnoj sferi — pa se
   niži rub sada ukopa umjesto da visi).
10. **`VolcanicHazardSpawner` je zaobilazio `GetSurfacePoint`** (vlastiti raycast bez
    SyncTransforms retryja + fallback na `localScale*0.5` = analitička kugla), a
    zone se spawnaju isti frame kad i planet. → Preusmjeren na `GetSurfacePoint`;
    usput analitički fallback u `GetSurfacePoint` sada loga upozorenje kad opali,
    da se stvarna pojavljivanja vide uz audit.
11. **'Ice' resurs (fridge.fbx) lebdio, a audit šutio.** Prefab je RIGGAN FBX
    (armature + skinned mesh) — skinned mesh nema MeshFilter, pa je `TryGetExtents`
    padao na `SkinnedMeshRenderer.bounds`: world AABB izveden iz bind-pose boundsa
    u prostoru root bonea, koji kod Blender armature sjeda do ~1.2–2.2 ISPOD
    stvarne geometrije (ovisno o orijentaciji spawna). Prizemljenje posjedne taj
    napuhani AABB na tlo → vidljiva kutija visi (repro u standalone playeru:
    +0.85), a audit mjeri ISTI AABB → gap ≈ 0 → šutnja. → `TryGetExtents` sada za
    skinned mesheve peče stvarne skinnane vrhove: `BakeMesh(baked, true)` +
    `TransformPoint`. VAŽNO: `useScale: true` je nužan — default `BakeMesh` peče
    lossyScale u vrhove pa bi `TransformPoint` skalu primijenio dvaput (krivo za
    svaki spawn s `miningWorldScale`/`pickupWorldScale` != 1, npr. Volcanic_rune
    60×). Bake radi i na non-readable meshu i na ugašenom rendereru (regeneracija),
    u editoru i u buildu (empirijski potvrđeno); `fridge.fbx.meta` svejedno dobio
    `isReadable: 1`. Repro s novim kodom: dno geometrije = 0.000 na površini.
    NAPOMENA: audit PRIJE ove točke NIJE "gledao samo Hub" — pokrivao je sve
    planete, ali je za riggane modele mjerio jednako krivo kao prizemljenje pa
    nije imao što prijaviti; sada ispisuje i pokrivenost po planetima
    ("pokrivenost (N planeta): Planet=…, Planet_00=…, …") da se to vidi u ispisu.

## Dijagnostički alat (novo)

**Tools → Web of Planets → Audit Surface Placement** (u Play modu) —
`Assets/Scripts/Planet/SurfaceAudit.cs` + `Assets/Editor/SurfacePlacementAuditMenu.cs`.
Za svaki `BaseInteractable` ispiše gap dna od površine njegove planete.
NAPOMENA: prva verzija mjerila je world-AABB rotiranog objekta i za velike objekte
prijavljivala LAŽNI ukop do pola dijagonale — sad mjeri identično kao grounding
(`TryGetExtents`, sada internal) i ispisuje i namjerni ukop i radijus planeta.

## Sljedeći koraci (ažurirano 16.7. popodne)

1. ✅ Audit s novim mjerenjem pokrenut: **2/274** — 13× "UTONUO" bio je artefakt
   starog mjerenja (potvrđeno), ostala su samo 2 poznata nalaza:
   - 2× "LEBDI ~0.3": 'Monitor' (NetworkComputerInteractable) i 'Pickaxe'
     (ToolInteractable) na Hubu — RUČNO postavljeni scene objekti uz bazu, kod ih ne
     dira. Odluka i dalje otvorena: spustiti ručno u editoru ILI dodati runtime snap
     (nije napravljeno da se ne dira korisnikova pozicija).
   NAPOMENA: taj audit NIJE dokazivao generirane planete — mjerio je po njihovom
   analitičkom SphereCollideru (točka 8), pa je lebdenje na njima bilo nevidljivo.
2. **Ponovo pokrenuti audit nakon točaka 8–11** (rekompilacija pa Play): sada
   collider = vidljivi mesh na svim planetima, pa audit prvi put stvarno mjeri
   vidljivu površinu generiranih planeta; za riggane modele ('Ice' fridge) mjeri
   stvarnu skinnanu geometriju. U ispisu provjeriti liniju "pokrivenost (N
   planeta)". Uz to vizualno provjeriti na licu mjesta (odletjeti na koji
   generirani planet / T-key stress test) — resursi (posebno 'Ice'), strojevi,
   markeri veza, vulkanske zone.
3. U konzoli tijekom testa paziti na novo upozorenje
   "SurfacePlacement: raycast na ... promašio" — označava da je neki objekt ipak
   prizemljen analitičkim fallbackom.
4. Ako sve čisto: promjene su necommitane (vidi `git status`) — commitati.

## Poznata ograničenja (namjerno ostavljeno)

- Strojevi se orijentiraju radijalno (uspravno), ne po normali trokuta — na
  generiranim planetima do ~8° nagiba tla pod strojem; rub koji bi visio sada se
  ukopa (točka 9). Ako se ikad poželi da strojevi prate teren, MachinePlacer
  treba koristiti hit normal iz FindSurfacePoint (odbacuje je) u svim TryPlace*
  granama.
- Analitički fallbackovi (GetSurfacePoint zadnja linija obrane,
  MachinePlacer.FindSurfacePoint) i dalje vraćaju točku na opisanoj kugli — do
  ~1.3% R iznad vidljivog mesha, ali sada barem logaju upozorenje (GetSurfacePoint).

## Nepovezani nalazi iz audita koda (bonus, nije dio ovog zadatka)

- `PlanetCreator.TeleportToPlanet` (destinationMarker grana) raycasta NEfiltrirano
  (može stati na kamen/stroj kod markera) i koristi tvrdi `+1f` za visinu igrača.
- Spawnani objekti NISU child planeta — bezopasno dok su planeti statični, ali ako se
  ikad doda rotacija/orbita planeta, sve spawnano ostaje u world spaceu.
- Regeneracija resursa (`ItemInteractable`) gasi collidere dok je sakriven, pa se na
  tom mjestu može sagraditi stroj — resurs se poslije vrati unutar stroja.
