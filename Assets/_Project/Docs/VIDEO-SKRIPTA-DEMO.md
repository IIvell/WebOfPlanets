# Web of Planets — podsjetnik za snimku mentoru

Ovo nije scenarij za čitanje, nego redoslijed što pokazati i o čemu reći par rečenica.
Govori normalno, svojim riječima. Ako nešto zaboraviš — nema veze, poanta je da mentor vidi
što postoji i kaže fali li mu nešto.

Prije snimanja samo: `TestingMode` isključen, i uzmi save koji je već malo napredovao
da ne rudariš deset minuta pred kamerom.

---

## 1. Ukratko što je igra (~30 s)

Pusti Play, zavrti kameru po Hubu.

Reci: 3D survival-factory, hodaš po sferičnim proceduralnim planetima, rudariš, gradiš strojeve
i povezuješ planete u mrežu koja s vremenom propada. Cilj je otključati svih 5 razina Huba.
Sve je jedna scena, ostalo se generira u runtimeu.

## 2. Kretanje po sferi

Obiđi planet do "dna", skoči par puta.

Gravitacija ide prema središtu planeta, ne prema dolje. Zato mogu obići planet u krug.
Generira se 30 planeta u 5 tipova: rudarski, organski, ledeni, vulkanski, plinoviti.

## 3. Rudarenje i alati

Drži **E** na čvoru, pokaži krug napretka. Otvori inventar **I**, **Q** za info o predmetu.

Dio resursa se kupi odmah, dio treba držati E. Čvorovi imaju tier i klasu — bez pijuka nema rude,
bez sjekire nema drva, za vulkanske rune treba Rune Drill. To drži igrača da ne preskoči faze.

## 4. Hub — izrada i napredak

**E** na računalo. Pokaži HUB PROGRESS pa CRAFTING, izradi nešto.

Recepti su otključani po tieru. Napredak Huba ide na 5 razina, svaka traži druge resurse —
prvo kamen i ruda, pa ingoti i drvo, pa led i plin, pa vulkanske rune. Ideja je da te to
natjera da posjetiš svaki tip planeta. Peti tier = kraj igre.

## 5. Strojevi

**P** postavi, **X** podigni. Pokaži kolektor kako radi ciklus i baca u Storage.

Kolektori skupljaju s površine, ekstraktori proizvode pasivno, talionica radi ingote.
Hub Uplink šalje stvari u Hub Storage s drugog planeta — jedini način da hraniš Hub bez nošenja.
Strojevi se mogu pokvariti i treba ih popravljati, češće na nestabilnim planetima.

## 6. Mreža veza — ovdje se najviše zadrži

**E** na totem potencijalne veze, pokaži izbor tri razine. Izgradi jednu.
Uzmi Network Scanner i **P** za mapu. Pa **E** na vezu za putovanje.

Ovo je glavna stvar u igri. Tri razine veze — slaba, srednja, jaka, svaka svoja cijena i vijek.
Veze **propadaju** s vremenom, mijenjaju boju i na kraju puknu. Vulkanski i plinoviti planeti
su nestabilni pa ubrzavaju propadanje. Znači mreža se mora održavati, i stalno biraš
isplati li se trajna veza ili je jeftinije platiti teleport.

## 7. Opasnosti i smrt

Uđi u lava zonu na vulkanskom, pokaži mob koji te juri, stavi masku pa sleti na plinoviti.

100 HP, nema liječenja. Lava zone rade štetu, plinoviti planeti su cijeli otrovni osim s maskom,
mobovi te jure ali su sporiji od tebe. Smrt = **R** i vraćaš se na Respawn Totem, inventar ostaje.

## 8. Kraj

Otvori mapu s par veza.

Petlja je: istraži, rudari, izradi alat, postavi strojeve, poveži planet, hrani Hub, otključaj tier —
i sve to dok ti mreža propada pod nogama.

---

## Ako pita nešto tehnički

- Skoro svi sustavi se sami pokreću iz koda umjesto da su složeni u sceni.
- Spremanje je jedan JSON; učitavanje ne reloada scenu nego ponovno izgradi svijet istim kodom.
- Sustavi komuniciraju preko statičkog event busa.
- Strojevi, resursi i recepti su ScriptableObjecti, balans se mijenja bez diranja koda.
- Iz GDD-a nisu napravljeni: artefakti, skrivene drevne veze, automatske transportne rute.
  Dobra prilika da pitaš mentora treba li nešto od toga dodati.

**Tipke:** `E` interakcija · `I` inventar · `Q` info · `P` postavi/mapa/maska · `X` podigni ·
`R` respawn · `C` kamera · `Esc` pauza · `1–9` hotbar
