# GroupRequests mikroservis

## Svrha i odgovornost

`GroupRequests` vodi oglase korisnika koji traže grupu za određeni Warframe predmet. Omogućava javno čitanje oglasa, autentifikovano kreiranje/brisanje sopstvenih oglasa i administratorski pregled/brisanje. Broj aktivnih oglasa zavisi od subscription statusa iz JWT tokena. Nakon kreiranja šalje događaj sistemu notifikacija, a izmene šalje i Audit servisu.

## Arhitektura i struktura

Tok HTTP zahteva je kontroler → FluentValidation → `IGroupRequestsService` → `IGroupRequestsRepository` → EF/PostgreSQL. AutoMapper razdvaja DB entitete, interne DTO objekte i javni model.

- `Commands` — ulazne komande za create/delete.
- `Validation` — FluentValidation pravila.
- `Controllers` — REST API, claims i status kodovi.
- `Services` — poslovna ograničenja i objavljivanje događaja.
- `Repositories` — EF CRUD i filteri.
- `Database` — DbContext, entitet, konfiguracija i migracije.
- `Broker` — RabbitMQ publisher i dva event ugovora.
- `Models` — puni DTO i javna projekcija.
- `Utility/Mappers` — AutoMapper profil.

Servis koristi .NET 10, EF Core/Npgsql 10, AutoMapper 14, FluentValidation 12.1.1 sa SharpGrip auto-validation, JWT Bearer i RabbitMQ.Client 7.2.1.

## Entitet i EF konfiguracija

`GroupRequestEntity`:

| Polje | Tip | Pravilo |
|---|---|---|
| `Id` | `int` | PK, PostgreSQL identity |
| `UserId` | `string` | obavezno; ID iz JWT `sub` claim-a |
| `InGameName` | `string` | obavezno |
| `ItemName` | `string` | obavezno |

Jedinstveni složeni indeks `(UserId, ItemName)` sprečava dupli oglas istog korisnika za isti predmet. Dužine stringova nisu ograničene EF konfiguracijom. `GroupRequestsDbContext` koristi šemu `GroupRequestsSchema`, `DbSet<GroupRequestEntity> GroupRequests`, assembly scanning konfiguracija i zasebnu tabelu istorije migracija u istoj šemi.

Migracije su: `InitialMigration`, `AddIndex` i `AddSchema`. Sve se automatski primenjuju pri startu. Seed ne postoji i nema relacija/foreign key-eva prema UsersDirectory ili ItemsStore bazi; `UserId` i `ItemName` su samo tekstualne reference.

## Komande, modeli i mapiranje

- `AddGroupRequestCommand`: `UserId`, `InGameName`, `ItemName`. Klijent šalje ime i predmet, dok kontroler prepisuje `UserId` iz tokena.
- `DeleteGroupRequestCommand`: `UserId`, `ItemName`; kontroler ga konstruiše iz rute/query-ja i claim-a.
- `GroupRequestDTO`: `UserId`, `InGameName`, `ItemName`; koristi se za sopstveni/admin pregled i servisne rezultate.
- `GroupRequest`: samo `InGameName` i `ItemName`; javna projekcija namerno skriva `UserId`.

AutoMapper definiše `Entity → DTO`, `Entity → GroupRequest` i `DTO → GroupRequest`.

## Validatori

`AddGroupRequestCommandValidator` zahteva neprazne `UserId`, `InGameName` i `ItemName`; sve greške koriste kod `required` i prilagođene kebab-style poruke. `DeleteGroupRequestCommandValidator` isto proverava `UserId` i `ItemName`. Nema maksimuma dužine, provere postojanja korisnika/predmeta niti normalizacije teksta.

Validatori su transient, registrovan je SharpGrip auto-validation, ali ih kontroler dodatno eksplicitno poziva i vraća `400` sa listom `{ ErrorCode, ErrorMessage }`.

## Repozitorijum

`IGroupRequestsRepository` i `GroupRequestsRepository` nude:

- kreiranje entiteta i `SaveChangesAsync`;
- brisanje po `(UserId, ItemName)`, sa `false` ako zapis ne postoji;
- čitanje svih;
- filtriranje tačnim poređenjem po `ItemName` ili `UserId`;
- dohvat prvog zapisa po složenom ključu.

Read metode ne koriste `AsNoTracking`, sortiranje ili paginaciju.

## Servisna poslovna logika

`GroupRequestsService` implementira sve repository operacije i mapiranje. Pri kreiranju prvo učitava sve oglase korisnika:

- korisnik bez aktivne pretplate može imati najviše jedan oglas;
- korisnik čiji JWT claim `subscription_status` tačno glasi `Active` može imati najviše pet (`MaxGroupRequestsForSubscribedUsers = 5`);
- jedinstveni DB indeks dodatno sprečava isti predmet po korisniku.

Nakon uspešnog upisa servis best-effort objavljuje:

- `GroupRequestCreatedEvent` na red `group-request-created`;
- `AuditEventMessage` na red `audit`, akcija `Create`.

Nakon uspešnog brisanja šalje audit događaj sa akcijom `Delete`. Greške publish-a se loguju, ali se ne propagiraju, pa uspešna DB operacija ostaje uspešna. Nema outbox-a ni transakcione veze između baze i brokera.

## RabbitMQ ugovori

`GroupRequestCreatedEvent` sadrži `UserId`, `ItemName`, `InGameName`, `CreatedAt` (UTC pri konstrukciji). Njega konzumira `UserNotification` servis, koji čuva događaj i šalje SignalR obaveštenja korisnicima koji favorizuju predmet.

`AuditEventMessage` sadrži `DateTime`, konstantni `Service = "GroupRequests"`, `Action`, `User`, `Details`; konzumira ga `Audit` servis.

`RabbitMQPublisher` podržava direktno slanje na durable queue i slanje preko durable topic exchange-a. Trenutni servis koristi queue overload. Poruke su JSON/UTF-8, `ContentType = application/json` i persistent. Za svako slanje otvara novu konekciju i kanal. Retry helper pokušava povezivanje do pet puta sa čekanjem od pet sekundi.

## HTTP API

Bazna ruta je `/v1/grouprequests`.

| Metod i ruta | Pristup | Ponašanje |
|---|---|---|
| `GET /` | javno | svi oglasi mapirani na javni model bez `UserId`; `200` |
| `GET /item/{itemName}` | javno | oglasi za tačno ime predmeta, javni model; `200`, uključujući praznu listu |
| `GET /me` | autentifikovan | oglasi korisnika iz `sub` claim-a; `401` ako claim nedostaje |
| `GET /admin?userId=&itemName=` | samo `Admin` | kombinovani filter, filter po jednom polju ili svi puni DTO zapisi |
| `POST /` | autentifikovan | kreira oglas; `201`, `400` validacija/greška, `409` za prekoračeno poslovno ograničenje |
| `DELETE /{itemName}` | autentifikovan | briše sopstveni oglas; `204` ili `404` |
| `DELETE /admin?userId=&itemName=` | samo `Admin` | briše odabrani tuđi oglas; `204`, `404` ili `400` |

Kod POST-a `UserId` dolazi iz `ClaimTypes.NameIdentifier`, a subscription status iz custom claim-a. `InGameName` se prihvata iz request body-ja, ne iz tokena niti UsersDirectory-ja.

## Startup, JWT i konfiguracija

`Program` registruje DbContext, AutoMapper, scoped repozitorijum/servis/publisher, transient validatore, OpenAPI i JWT. Migracije se primenjuju na startu. Middleware redosled je HTTPS redirekcija → autentikacija → autorizacija → kontroleri.

JWT validira issuer, audience, lifetime i HMAC-SHA256 potpis zajedničkim parametrima koje koristi UsersDirectory. Konfiguracija obuhvata `ConnectionStrings:DefaultConnection`, `Jwt:*`, `RabbitMQ:*`, `Logging` i `AllowedHosts`. Development i Compose koriste zajedničku bazu `WarframeLFGBoardDb`; bazni `appsettings.json` navodi zasebnu `GroupRequestsDb`, ali je override-ovan u Development/Docker okruženju.

Nginx prosleđuje `/v1/grouprequests` na `group-requests:8080`. Lokalni launch port je 8082; Compose host port je 8003. Dockerfile radi restore/build/publish na .NET 10 slikama, instalira `libgssapi-krb5-2`, izlaže 8080/8443 i pokreće `GroupRequests.dll`.

## Testovi

`GroupRequests.Tests` sadrži controller i service testove (`GroupRequestsControllerTests`, `GroupRequestsServiceTests`). Docker build kopira test projekat, ali build/publish komande grade samo produkcioni projekat i ne pokreću `dotnet test`.

## Važni detalji i ograničenja

- Subscription odluka koristi stanje ugrađeno u JWT; promena pretplate zahteva novi token da bi je ovaj servis video.
- Provera limita i insert nisu atomski, pa konkurentni zahtevi mogu zaobići limit broja oglasa (ali ne i unique indeks za isti predmet).
- Ne postoji provera da `ItemName` postoji u ItemsStore servisu, niti da `InGameName` pripada korisniku.
- Nema seed-a, paginacije, sortiranja, cache-a, health endpointa ili direktnih HTTP poziva ka drugim servisima.

## Kompletan inventar komponenti

- Ulaz/API: `GroupRequestsController`, `AddGroupRequestCommand`, `DeleteGroupRequestCommand`, `AddGroupRequestCommandValidator`, `DeleteGroupRequestCommandValidator`.
- Poslovni/data sloj: `IGroupRequestsService`, `GroupRequestsService`, `IGroupRequestsRepository`, `GroupRequestsRepository`.
- Baza: `GroupRequestsDbContext`, `GroupRequestEntity`, `GroupRequestEntityConfiguration` i tri migracije sa snapshotom.
- Modeli/mapiranje: `GroupRequestDTO`, `GroupRequest`, `AutoMapperProfile`.
- Broker: `GroupRequestCreatedEvent`, `AuditEventMessage`, `IRabbitMQPublisher`, `RabbitMQPublisher` i `RabbitMQConnectionFactoryExtensions`.
- Hosting: `Program`, `GroupRequests.csproj`, `appsettings.json`, `appsettings.Development.json`, `launchSettings.json`, solution fajl i `Dockerfile`. Ovo je kompletan produkcioni skup; nema consumer-a, seedera, dodatnih utility klasa ni background servisa.
