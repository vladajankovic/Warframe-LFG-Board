# ItemsStore mikroservis

## Svrha i odgovornost

`ItemsStore` je katalog Warframe relikvija i resursa. Javno izlaže listanje i pretragu kataloga, dok administratorima omogućava CRUD operacije. Pri prvom startu praznu bazu puni podacima iz javnog Warframe API-ja i Warframe Wiki stranice. Administrativne izmene objavljuje Audit servisu preko RabbitMQ-a.

Tok zahteva je kontroler → FluentValidation → servis → repozitorijum → EF/PostgreSQL. Seed koristi typed `HttpClient` utility → spoljne izvore → EF bulk insert.

## Tehnologije i struktura

ASP.NET Core/.NET 10, EF Core i Npgsql 10, AutoMapper 14, FluentValidation 12.1.1 + SharpGrip auto-validation, JWT Bearer, RabbitMQ.Client 7.2.1 i typed `HttpClient`.

- `Controllers` — odvojeni REST API-ji za relics/resources.
- `Commands` i `Validators` — write ugovori i pravila.
- `Services` — poslovna validacija, mapiranje i audit događaji.
- `Repository` — EF CRUD.
- `Database` — entiteti, enum, konfiguracije i migracije.
- `Seed` — inicijalni import kataloga.
- `Utility` — API/wiki klijent, normalizacija stringa, pravila relikvija i mapiranje.
- `Broker` — RabbitMQ publisher i audit payload.
- `Models` — API izlazi i DTO-i eksternih izvora.

## Entiteti i EF konfiguracija

### `RelicEntity`

| Polje | Tip | Pravilo |
|---|---|---|
| `Id` | `int` | PK, identity |
| `Name` | `string` | obavezno, max 30; prikazno ime, npr. `Lith A1` |
| `RelicName` | `string` | obavezno, max 30, unique; normalizovan snake_case ključ |
| `RelicType` | `RelicType` | obavezni enum |
| `RelicCode` | `string` | obavezno, max 10 |

`RelicType` vrednosti su `UNKNOWN`, `LITH`, `MESO`, `NEO`, `AXI`, `REQUIEM`, `VANGUARD`. `EnumMember` anotacije navode lowercase vrednosti, ali EF ih bez eksplicitne konverzije standardno čuva kao integer.

### `ResourceEntity`

| Polje | Tip | Pravilo |
|---|---|---|
| `Id` | `int` | PK, identity |
| `ResourceName` | `string` | obavezno, max 128, unique; snake_case lookup ključ |
| `Name` | `string` | obavezno, max 128; prikazno ime |
| `Description` | nullable string | max 512 |
| `Region` | nullable string | max 64 |
| `Category` | nullable string | max 128 |
| `Group` | nullable string | max 128 |

`ItemsDbContext` izlaže `Relics` i `Resources`, koristi šemu `ItemsStoreSchema`, assembly scanning konfiguracija i `ItemsStoreSchema.__EFMigrationsHistory`. Migracioni sled obuhvata `InitialMigration`, `AddRelicName`, `AddResourceEntity`, `UpdateDatabase`, `UseIdentityIds`, `ResourceNameNotUnique`, `AddResourceName`, `AddResourceCategoryFields`, `RenameColumnToRegion` i `AddSchema`. Sve migracije se primenjuju na startu.

## API modeli, DTO-i i komande

- `AddRelicCommand`: `Name`, enum `Type`, `Code`; koristi se i za create i update.
- `AddResourceCommand`: `Name`, `Description`, `Region`, `Category`, `Group`; koristi se i za create i update.
- `Relic`: `Id`, `Name`, lowercase string `RelicType`, `RelicCode`.
- `Resource`: `Id`, `Name`, `Description`, `Region`, `Category`, `Group`; interni `ResourceName` se ne vraća.
- `RelicDTO`: `Name`, `Type`, oblik odgovora spoljnog Warframe API-ja.
- `ItemDTO`: `Name`, `UniqueName`, `Description`, `Type`, `Category`, `ExcludeFromCodex` za puni API katalog.
- `WikiResourceItemDTO`: `Name`, `Region`, opcioni `Category`, `Group` iz parsiranog Wiki HTML-a.

AutoMapper mapira `RelicEntity → Relic`, uz enum u lowercase string, i `ResourceEntity → Resource` po imenima.

## Validacija i utility

`AddRelicCommandValidator` zahteva ime (max 30), ispravan enum, code (max 10). `IsInEnum` prihvata i `UNKNOWN`, dok dodatna servisna validacija to odbija. `AddResourceCommandValidator` zahteva ime (max 128) i opis (max 512); poruka za prazan opis greškom kaže da je `name` obavezan, a max-length poruka za opis prikazuje dužinu imena umesto opisa. Region/category/group nemaju FluentValidation pravila iako DB ima limite.

`StringUtils.ToSnakeCase`/`ToKebabCase` pretvaraju tekst u lowercase i spajaju segmente razdvojene whitespace-om. Ne uklanjaju interpunkciju niti normalizuju Unicode.

`RelicUtils`:

- mapira lowercase tip u enum;
- izvlači prvi token kao tip i drugi kao code;
- smatra relikviju validnom samo ako ime ima tačno dva tokena, tip nije `UNKNOWN`, odgovara `command.Type`, i normalizovano `TYPE Code` odgovara imenu.

## Spoljni izvori i seed

`WarframeApiUtils` koristi `https://api.warframestat.us/items`:

- `GetAllItems` učitava kompletan `List<ItemDTO>`;
- `GetAllRelics` isti endpoint deserijalizuje kao `List<RelicDTO>`, filtrira `Type == "Relic"` i imena koja se završavaju sa `Intact`.

Za resurse šalje HTTP/1.1 GET ka `https://wiki.warframe.com/w/Resources`, sa curl user-agentom. HTML parsira regex-ima po tabovima, navbox tabelama, grupama i `data-param-name`, uklanja deo HTML entiteta/tagova i deduplikuje imena case-insensitive. Ovo je osetljivo na promene Wiki markup-a i nije puni HTML parser.

`DbSeed` se izvršava posle migracija:

- `SeedRelics` potpuno preskače import ako postoji makar jedna relikvija. Za `* Intact` rezultate odvaja poslednju reč, izvlači tip/code, pravi prikazno i snake_case ime, odbacuje nevalidne zapise i bulk-insertuje ostale.
- `SeedResources` potpuno preskače import ako postoji makar jedan resurs. Spaja Wiki metadata sa opisom iz punog API kataloga (prvi red opisa), zatim ručno dodaje `Credits` i bulk-insertuje listu.

Spoljni pozivi nemaju eksplicitnu retry politiku, timeout konfiguraciju ili fallback dataset. Greška seed-a se loguje i propagira, pa može sprečiti startup. Nema periodičnog osvežavanja kataloga.

## Repozitorijumi

`RelicRepository` kreira `RelicName` pomoću snake_case-a; get/delete koriste isti normalizovani ključ. Update menja samo `RelicType` i `RelicCode`, ne menja prikazno ili lookup ime. `ResourceRepository` analogno generiše `ResourceName`; update menja opis i metadata polja, ali ne ime. Oba repozitorijuma imaju get-all bez sortiranja/paginacije/`AsNoTracking` i izvršavaju `SaveChangesAsync` po write operaciji.

Interfejsi `IRelicRepository` i `IResourceRepository` eksplicitno izlažu create/get/get-all/update/delete.

## Servisi

`RelicService` pre create/update poziva `RelicUtils.IsRelicValid`. Create odbija postojeći normalizovani naziv; update zahteva postojeći zapis. Type/code filteri prvo učitavaju sve relikvije iz baze, zatim filtriraju u memoriji, umesto SQL filtera.

`ResourceService` odbija create duplikata i update nepostojećeg zapisa. Oba servisa mapiraju entitete u API modele i nakon uspešnog create/update/delete pokušavaju da objave audit događaj. Publish failure se samo loguje i ne poništava DB promenu.

Audit payload ima `Service = "ItemsStore"`, UTC vreme, akcije `Create`/`Update`/`Delete`, konstantni `User = "Admin"` i detalj sa imenom predmeta. Stvarni admin identitet iz JWT-a se ne prenosi.

## HTTP API

### Relikvije — `/v1/itemstore/relics`

| Metod i ruta | Pristup | Rezultat |
|---|---|---|
| `GET /` | javno | `200`, sve relikvije |
| `GET /relic/{relicName}` | javno | `200` ili `404`; ruta prihvata lookup ime koje se snake-case normalizuje |
| `GET /type/{relicType}` | javno | lowercase tip se mapira u enum; `200` ili `404` za prazno/nepoznato |
| `GET /code/{relicCode}` | javno | code se pretvara u uppercase; `200` ili `404` |
| `POST /` | Admin | `201`, ili `400` za duplikat/nevalidno |
| `DELETE /relic/{relicName}` | Admin | `204` ili `404` |
| `PATCH /` | Admin | `200`; controller `InvalidOperationException` vraća kao `404`, ostalo `400` |

### Resursi — `/v1/itemstore/resources`

| Metod i ruta | Pristup | Rezultat |
|---|---|---|
| `GET /` | javno | `200`, svi resursi |
| `GET /resource/{resourceName}` | javno | `200` ili `404` |
| `POST /` | Admin | `201` ili `400` |
| `DELETE /resource/{resourceName}` | Admin | `204` ili `404` |
| `PATCH /` | Admin | `200`, `404` za nepostojeći, ostalo `400` |

SharpGrip automatska validacija je registrovana, a kontroleri se oslanjaju na MVC validation pipeline. Nginx prosleđuje `/v1/itemstore` ka `items-store:8080`.

## RabbitMQ infrastruktura

Publisher deklariše durable red, serijalizuje JSON u UTF-8, postavlja `ContentType` i persistent delivery, i objavljuje na default exchange sa routing key-em `audit`. Postoji i generički durable topic exchange overload koji se trenutno ne koristi. Svaki publish otvara konekciju i kanal. Retry extension pravi do pet ponovnih pokušaja na pet sekundi.

## Startup, JWT, konfiguracija i Docker

`Program` registruje DbContext, mapper, oba repository/service para, RabbitMQ, validatore, typed HTTP klijent, seed, JWT, OpenAPI i kontrolere. Startup redosled je migracije → relic seed → resource seed → middleware. Pipeline uključuje HTTPS redirekciju, autentikaciju i autorizaciju.

JWT samo validira tokene koje izdaje UsersDirectory: issuer, audience, lifetime i shared HMAC ključ. Konfiguracija obuhvata `ConnectionStrings:DefaultConnection`, `Jwt:*`, `RabbitMQ:*`, logging i hosts. Development/Compose koriste zajednički `WarframeLFGBoardDb`, dok bazni appsettings navodi `ItemsStoreDb`. Tajne su trenutno u versioned konfiguraciji.

Launch profil koristi port 8080; Compose mapira host 8002 → container 8080. Dockerfile radi višefazni .NET 10 restore/build/publish, instalira `libgssapi-krb5-2`, izlaže 8080/8443 i pokreće `ItemsStore.dll`.

## Testovi i ograničenja

`ItemsStore.Tests` pokriva oba kontrolera, oba servisa, oba repozitorijuma, oba validatora, `RelicUtils` i AutoMapper profil. Docker build kopira test projekat, ali ga ne izvršava.

Važni rizici su potpuna zavisnost prvog starta od eksternih izvora, regex Wiki parser, učitavanje svih relikvija za filtere, nema paginacije/cache-a, nema provere referenci iz GroupRequests-a, nema outbox-a za audit događaje i neusklađen nullable API model sa nullable DB poljima resursa.

## Kompletan inventar komponenti

- API/komande: `RelicsController`, `ResourcesController`, `AddRelicCommand`, `AddResourceCommand`, `AddRelicCommandValidator`, `AddResourceCommandValidator`.
- Servisi: `IRelicService`/`RelicService` i `IResourceService`/`ResourceService`.
- Repozitorijumi: `IRelicRepository`/`RelicRepository` i `IResourceRepository`/`ResourceRepository`.
- Baza: `ItemsDbContext`, `RelicEntity`, `ResourceEntity`, `RelicType`, `RelicEntityTypeConfiguration`, `ResourceEntityTypeConfiguration`, migracije i snapshot.
- Modeli: `Relic`, `Resource`, `RelicDTO`, `ItemDTO`, `WikiResourceItemDTO`.
- Utility/seed: `RelicUtils`, `StringUtils`, `WarframeApiUtils`, `AutoMapperProfile`, `DbSeed`.
- Broker: `AuditEventMessage`, `IRabbitMQPublisher`, `RabbitMQPublisher`, `RabbitMQConnectionFactoryExtensions`.
- Hosting: `Program`, `ItemsStore.csproj`, oba `appsettings` fajla, `launchSettings.json`, solution fajl i `Dockerfile`. Servis nema RabbitMQ consumer ili druge background servise.
