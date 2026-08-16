# Audit mikroservis

## Svrha i odgovornost

`Audit` je centralni servis za trajno beleženje i pregled aktivnosti ostalih mikroservisa. Ne poseduje poslovnu logiku korisnika, predmeta ili grupnih zahteva: prima audit događaje asinhrono preko RabbitMQ reda `audit`, čuva ih u PostgreSQL bazi i administratorima/ovlašćenim korisnicima izlaže filtrirani HTTP pregled.

Tok podataka je: mikroservis izdavač → RabbitMQ `audit` → `AuditEventConsumer` → `CreateAuditLogCommand` → `AuditRepository` → PostgreSQL šema `AuditLogsSchema`.

## Tehnologije i paketi

- ASP.NET Core / .NET 10 Web API, nullable reference types i implicit usings.
- Entity Framework Core 10.0.2 i Npgsql 10.0.0.
- RabbitMQ.Client 7.2.1.
- JWT Bearer autentikacija 10.0.0.
- AutoMapper 14.0.0.
- OpenAPI 10.0.1; FluentValidation paketi postoje u projektu, ali servis nema validatore.

## Struktura

- `Program.cs` — DI, baza, JWT, RabbitMQ consumer, middleware i migracije.
- `Controllers/AuditController.cs` — HTTP čitanje logova.
- `Broker/RabbitMQ` — ugovor događaja, consumer, hosted service i retry konekcije.
- `Commands/CreateAuditLogCommand.cs` — interni komandni objekat za upis.
- `Database` — DbContext, entitet i EF konfiguracija.
- `Repository` — upis i filtriranje logova.
- `Models/AuditLog.cs` — izlazni API model.
- `Utility/Mappings` — AutoMapper profil.

## Entitet i model baze

### `AuditLogEntity`

| Polje | Tip | EF pravilo | Značenje |
|---|---|---|---|
| `Id` | `string` | primarni ključ, obavezno | GUID generisan u aplikaciji (`Guid.NewGuid().ToString()`) |
| `DateTime` | `DateTime` | obavezno | UTC vreme koje šalje izvorni servis |
| `Service` | `string` | obavezno, max 64 | naziv izvornog mikroservisa |
| `Action` | `string` | obavezno, max 128 | tip aktivnosti, npr. `Create`, `Delete`, `Login` |
| `User` | `string` | obavezno, max 254 | korisnički ID, email ili oznaka `Admin`, zavisno od izdavača |
| `Details` | `string` | obavezno | tekstualni opis događaja |

`AuditDbContext` izlaže `DbSet<AuditLogEntity> AuditLog`, postavlja podrazumevanu šemu `AuditLogsSchema` i automatski učitava sve `IEntityTypeConfiguration` klase iz assembly-ja. EF istorija migracija je u `AuditLogsSchema.__EFMigrationsHistory`. Migracija `20260614105621_InitialMigration` formira početnu tabelu. Nema relacija, indeksa za filtere ni seed podataka.

## Komande i API modeli

- `CreateAuditLogCommand` sadrži `DateTime`, `Service`, `Action`, `User` i `Details`; consumer ga formira iz RabbitMQ poruke.
- `AuditEventMessage` ima isti oblik i dva konstruktora: prazan radi JSON deserijalizacije i kompletan konstruktor.
- `AuditLog` je izlazni model bez internog `Id` polja. AutoMapper mapira `AuditLogEntity → AuditLog` po imenima.

## Repozitorijum

`IAuditRepository` / `AuditRepository` ima dve operacije:

- `CreateAuditLog(command)` kreira entitet, dodaje ga u DbContext, poziva `SaveChangesAsync` i vraća entitet.
- `Get(from, to, service, action, user)` gradi EF upit sa opcionim filterima. Datumske granice su uključive (`>= from`, `<= to`), a tekstualni filteri koriste tačno poređenje. Rezultat se sortira opadajuće po vremenu, čita bez praćenja (`AsNoTracking`) i mapira u `List<AuditLog>`. Završna materijalizacija koristi sinhroni `ToList()` unutar async metode.

Poseban servisni sloj ne postoji; kontroler koristi repozitorijum direktno.

## RabbitMQ komunikacija

`AuditEventConsumerHostedService` pokreće singleton `IBrokerEventConsumer` kao ASP.NET background service. `AuditEventConsumer`:

1. Otvara konekciju i kanal.
2. Deklariše durable, non-exclusive, non-auto-delete red `audit`.
3. Postavlja QoS/prefetch na jednu poruku.
4. Koristi `AsyncEventingBasicConsumer` sa tagom `audit-consumer` i ručnim ACK-om.
5. UTF-8 telo deserijalizuje u `AuditEventMessage` preko `System.Text.Json`.
6. U novom DI scope-u dobavlja `IAuditRepository`, pravi komandu i upisuje log.
7. Uspeh potvrđuje sa `BasicAckAsync`; null payload ili grešku vraća sa `BasicNackAsync(..., requeue: true)`.

Konekcija pokušava najviše pet ponovnih povezivanja uz razmak od pet sekundi; nakon iscrpljenih retry pokušaja izuzetak se propagira. Consumer ostaje živ preko `Task.Delay(Timeout.Infinite, cancellationToken)` i uredno obrađuje gašenje.

Poznati izdavači reda `audit` su `UsersDirectory`, `ItemsStore`, `GroupRequests` i `UserNotification`. Objavljivanje poslovnog događaja i upis audit loga nisu u istoj transakciji: izdavači uglavnom hvataju grešku i ne poništavaju već završenu poslovnu operaciju.

## HTTP kontroler

Bazna ruta je `v1/audit`.

| Metod i ruta | Pristup | Ulaz | Rezultat |
|---|---|---|---|
| `GET /v1/audit` | bilo koji autentifikovani korisnik (`[Authorize]`) | opcioni query: `from`, `to`, `service`, `action`, `user` | `200 OK` i lista logova |

U kodu nema provere administratorske role, paginacije ni validacije da je `from <= to`. Nginx prosleđuje `/v1/audit` ka kontejneru `audit:8080`.

## Startup, autentikacija i konfiguracija

`Program` registruje DbContext, AutoMapper, scoped repozitorijum, RabbitMQ factory/consumer, JWT autentikaciju, kontrolere i OpenAPI. Na startu sinhrono primenjuje sve EF migracije. OpenAPI endpoint mapira samo u Development okruženju. Pipeline sadrži HTTPS redirekciju i autorizaciju.

JWT validira issuer, audience, rok važenja i HMAC potpis prema `Jwt:Key`, `Jwt:Issuer` i `Jwt:Audience`. Važan detalj: `AddAuthentication` je konfigurisan, ali pipeline nema `app.UseAuthentication()` pre `UseAuthorization()`, pa `[Authorize]` zavisi od ponašanja hosting pipeline-a i ovo je potencijalna greška konfiguracije.

Konfiguracioni ključevi su:

- `ConnectionStrings:DefaultConnection` — obavezan; u Docker Compose-u pokazuje na zajedničku bazu `WarframeLFGBoardDb` na hostu `postgres`.
- `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` — zajednički sa servisom koji izdaje tokene.
- `RabbitMQ:Host`, `Port`, `Username`, `Password`; fallback vrednosti su `localhost:5672`, `guest/guest`.
- standardni `Logging` i `AllowedHosts`.

`appsettings.json` nema connection string, pa se on očekuje iz Development konfiguracije ili environment varijable. Tajne su trenutno zapisane u repozitorijumu/Compose konfiguraciji i za produkciju ih treba izmestiti u secret store.

## Docker i portovi

Višefazni Dockerfile koristi `dotnet/sdk:10.0` za restore/build/publish i `dotnet/aspnet:10.0` za runtime, instalira `libgssapi-krb5-2`, izlaže 8080/8443 i pokreće `Audit.dll`. Lokalni launch profil koristi `http://localhost:8083`; Docker Compose mapira host `8004` na kontejnerski `8080` i čeka zdrave PostgreSQL i RabbitMQ servise.

## Seed, utility, validacija i testovi

- Seed baze ne postoji: logovi nastaju isključivo iz događaja.
- Jedini utility sloj je AutoMapper profil; retry extension je infrastrukturni helper.
- Nema FluentValidation validatora niti ručne validacije audit payload-a.
- Za ovaj mikroservis nema zasebnog test projekta u repozitorijumu.

## Važne operativne osobine

- Durable red i persistent poruke izdavača štite poruke pri restartu brokera, ali beskonačno requeue-ovanje neispravne poruke može napraviti poison-message petlju.
- Consumer nema dead-letter red, deduplikaciju ni idempotency ključ, pa ponovna isporuka može napraviti duplikat.
- Endpoint vraća celu pronađenu listu bez paginacije; uz rast audit tabele ovo postaje memorijski i DB rizik.
- Tačni string filteri i odsustvo indeksa na `DateTime`, `Service`, `Action` i `User` mogu usporiti administrativne pretrage.

## Kompletan inventar komponenti

- `Program` sastavlja aplikaciju; `AuditController` izlaže API.
- `AuditDbContext`, `AuditLogEntity` i `AuditLogEntityConfiguration` čine persistence model.
- `IAuditRepository` i `AuditRepository` čine data-access sloj.
- `CreateAuditLogCommand`, `AuditLog` i `AuditEventMessage` su komandni, izlazni i broker ugovor.
- `AutoMapperProfile` mapira entitet; `IBrokerEventConsumer`, `AuditEventConsumer` i `AuditEventConsumerHostedService` realizuju consumption; `RabbitMQConnectionFactoryExtensions` realizuje retry.
- `Audit.csproj`, oba `appsettings` fajla, `launchSettings.json`, migracija/snapshot i root `Dockerfile` čine build, runtime i DB konfiguraciju. Drugih produkcionih komponenti, seedera, validatora, servisa ili utility klasa nema.
