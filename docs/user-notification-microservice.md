# UserNotification mikroservis

## Svrha i odgovornost

`UserNotification` upravlja korisničkim favoritima i kratkotrajnim obaveštenjima o novim group request oglasima. Korisnik sa aktivnom pretplatom bira predmete koje prati. Kada `GroupRequests` objavi događaj za takav predmet, servis ga čuva, pronalazi pratioce i aktivnim SignalR konekcijama šalje real-time poruku. REST API omogućava pregled pending obaveštenja i njihovo pojedinačno dismiss-ovanje.

Tok događaja je: `GroupRequests` → RabbitMQ red `group-request-created` → consumer → tabela notifications → favorites lookup → SignalR user grupe. Favorites izmene dodatno proizvode događaje za `Audit` servis.

## Tehnologije i struktura

Servis koristi ASP.NET Core/.NET 10, EF Core/Npgsql 10, JWT Bearer, SignalR, RabbitMQ.Client 7.2.1 i OpenAPI. Projekat referencira AutoMapper/FluentValidation pakete, ali nema mapper profile ni validatore.

- `Controllers` — favorites i notification REST API.
- `Hubs` — autorizovani SignalR hub.
- `Commands` — komanda za add/remove favorite.
- `Services` — favorites pravila, pending logika i cleanup background task.
- `Repositories` — EF CRUD/upiti.
- `Database` — tri entiteta, relacija i konfiguracije.
- `Broker/Events` — ulazni group request i izlazni audit payload.
- `Broker/RabbitMQ` — consumer, hosted wrapper, publisher i connection retry.
- `Models` — grupisani favoriti i izlaz notification-a.

## Entiteti i relacije

### `UserFavoriteEntity`

| Polje | Tip | Pravilo |
|---|---|---|
| `Id` | `string` | PK, GUID generisan u aplikaciji |
| `UserId` | `string` | obavezno, identitet iz JWT `sub` |
| `ItemName` | `string` | obavezno |

Složeni unique indeks `(UserId, ItemName)` sprečava dupli favorite.

### `UserNotificationEntity`

| Polje | Tip | Pravilo |
|---|---|---|
| `Id` | `string` | PK, GUID |
| `GroupRequestUserId` | `string` | obavezno; autor oglasa |
| `ItemName` | `string` | obavezno |
| `InGameName` | `string` | obavezno |
| `CreatedAt` | `DateTime` | obavezno, dolazi iz događaja |
| `DismissedUsers` | lista | navigation kolekcija dismiss zapisa |

Indeks `(ItemName, CreatedAt)` ubrzava pending query.

### `UserNotificationDismissalEntity`

Ima složeni PK `(NotificationId, UserId)`, indeks na `UserId`, obavezni `DismissedAt` i many-to-one vezu ka notification-u. Foreign key koristi cascade delete, pa cleanup notification-a briše i njegove dismissal zapise.

`NotificationsDbContext` izlaže `Favorites`, `Notifications` i `NotificationDismissals`, koristi šemu `UserNotificationsSchema`, assembly scanning konfiguracija i zasebnu migration-history tabelu. Migracije su `InitialMigration` i `AddNotificationDismissals`; primenjuju se na startu. Seed ne postoji.

String dužine nisu ograničene EF konfiguracijom. Ne postoje foreign key-evi ka UsersDirectory, ItemsStore ili GroupRequests bazama.

## Komande i modeli

- `AddOrRemoveUserFavoritesCommand`: `UserId`, `ItemName`; oba kontroler sastavlja iz JWT-a i rute, pa nema direktnog body input-a.
- `UserFavorites`: `UserId` i `List<string> FavoriteItems`; administratorski endpoint grupiše redove po korisniku.
- `GroupRequestNotification`: `Id`, `GroupRequestUserId`, `ItemName`, `InGameName`, `CreatedAt`; koristi se i za REST pending odgovor i SignalR payload.
- `GroupRequestCreatedEvent`: isti poslovni podaci bez notification ID-a; `CreatedAt` se postavlja na UTC kod izdavača.
- `AuditEventMessage`: UTC vreme, konstantni `Service = "UserNotification"`, akcija, user i detalji.

Nema FluentValidation validatora. `itemName` i `notificationId` se proveravaju samo implicitno kroz route binding/repository rezultat; ne proverava se postojanje predmeta u ItemsStore-u.

## Favorites repozitorijum i servis

`UserFavoritesRepository` implementira create, tačan get po `(UserId, ItemName)`, list all, list user, pronalaženje distinct pratilaca predmeta, pojedinačno brisanje i brisanje svih favorita korisnika. Sve write operacije pozivaju `SaveChangesAsync`; read upiti ne koriste `AsNoTracking`.

`UserFavoritesService`:

- `Create` je idempotentan na nivou servisa: ako favorite postoji, vraća postojeći entitet bez novog audit događaja;
- pre inserta učitava sve favorite korisnika i dozvoljava najviše 10 (`MAX_FAVORITES`);
- `Delete` briše ako postoji;
- `GetAll` grupiše sve DB redove u jedan model po korisniku;
- `GetAllFromUser` uvek vraća model, po potrebi sa praznom listom.

Uspešan novi favorite šalje audit akciju `Added`, a brisanje `Removed`. Publish je best-effort: greška se loguje i ne poništava DB izmenu. Provera broja i insert nisu atomski, pa paralelni zahtevi mogu preći limit, dok unique indeks štiti samo duplikate.

## Notification repozitorijum i servis

`UserNotificationsRepository.Create` čuva svaki group request događaj, čak i ako trenutno nema pratilaca. `GetPendingForUser` vraća obaveštenja:

- čiji je `ItemName` u korisnikovim favoritima;
- čiji je `CreatedAt >= notBefore`;
- za koja ne postoji dismissal tog korisnika;
- sortirana opadajuće po `CreatedAt`.

Ako lista predmeta nema elemente, odmah vraća praznu listu. `Dismiss` prvo proverava notification; nepostojeći vraća false, već dismissed vraća true, inače upisuje dismissal sa UTC vremenom. `DeleteOlderThan` koristi EF `ExecuteDeleteAsync`, a cascade uklanja dismissal zapise.

`UserNotificationsService.GetPendingForUser` se oslanja na subscription claim:

- ako status nije aktivan, briše sve favorite korisnika i vraća prazno;
- ako je aktivan, učitava favorite, računa vremenski cutoff i mapira pending entitete u API modele.

Retention se čita prvo iz environment ključa `CLEANUP_NOTIFICATIONS_AFTER`, zatim `Notifications:CleanupAfterMinutes`, a fallback je 5 minuta.

## RabbitMQ consumer

`NotificationEventConsumerHostedService` pokreće singleton consumer kao background service. Consumer:

1. Otvara retry konekciju i kanal.
2. Deklariše durable red `group-request-created`.
3. Postavlja prefetch/QoS na jednu poruku i ručni ACK.
4. Deserijalizuje UTF-8 JSON u `GroupRequestCreatedEvent`.
5. U scoped obradi čuva notification.
6. Iz favorites repozitorijuma dobavlja distinct korisnike koji prate `ItemName`.
7. Svakoj SignalR grupi `user:{userId}` paralelno šalje metodu `GroupRequestCreated` i isti notification model.
8. Tek nakon kompletne obrade potvrđuje poruku; greška izaziva NACK sa `requeue: true`.

Ako nema pratilaca, notification ipak ostaje sačuvan i poruka se potvrđuje. Retry konekcije je najviše pet pokušaja uz pauzu od pet sekundi. Poison poruka nema dead-letter zaštitu, a ponovno izvršavanje posle delimičnog uspeha nije idempotentno i može napraviti dupli notification/SignalR send.

## SignalR hub

`UserNotificationHub` je `[Authorize]` hub na `/hubs/user-notifications`. Pri konekciji čita `ClaimTypes.NameIdentifier` i connection dodaje u grupu `user:{id}`. Server šalje klijentu naziv metode `GroupRequestCreated`.

Za WebSocket/SSE klijente JWT može stići kroz query `access_token`: `JwtBearerEvents.OnMessageReceived` ga prihvata samo kada putanja počinje hub rutom. Nginx za hub uključuje HTTP/1.1 Upgrade/Connection zaglavlja i 3600-sekundne read/send timeout-e.

Hub ne proverava `subscription_status`; aktivne konekcije mogu primiti real-time događaj ako u bazi još imaju favorite. REST pending poziv briše favorite neaktivnog korisnika, ali sam consumer ne proverava subscription status jer nema direktnu vezu ka UsersDirectory servisu.

## Background cleanup

`NotificationCleanupService` radi jednom odmah pri startu i zatim svake minute. Iz retention konfiguracije računa cutoff i briše sva starija obaveštenja u jednom DB upitu. Cancellation se propagira; druge greške se loguju i sledeći interval nastavlja. I servis za pending i cleanup koriste isti retention parametar, pa korisnik ne dobija starije zapise ni pre nego što ih cleanup fizički ukloni.

## HTTP API

### Favorites — `/v1/user-notification/favorites`

| Metod i ruta | Pristup | Ponašanje |
|---|---|---|
| `POST /{itemName}` | autentifikovan + `subscription_status=Active` | dodaje/vrati postojeći favorite; `200`; `400` bez aktivne pretplate ili preko limita |
| `GET /` | Admin | svi favoriti grupisani po user ID-u; `200` |
| `GET /me` | autentifikovan | favoriti trenutnog `sub` korisnika; `200` |
| `DELETE /{itemName}` | autentifikovan | briše svoj favorite; `200` ili `404` |

### Notifications — `/v1/user-notification/notifications`

Ceо kontroler zahteva autentikaciju.

| Metod i ruta | Ponašanje |
|---|---|
| `GET /` | čita `sub` i subscription claim; `200` lista pending, ili `401` bez ID-a |
| `POST /{notificationId}/dismiss` | idempotentno dismiss-uje za trenutnog korisnika; `204`, `404` ili `401` |

Nginx prosleđuje `/v1/user-notification` i posebnu hub rutu na `user-notification:8080`.

## RabbitMQ publisher za audit

Favorites servis objavljuje persistent JSON na durable redu `audit`. Publisher ima i neiskorišćen topic-exchange overload, za svaki publish otvara novu konekciju/kanal i koristi connection retry helper. Audit poruke konzumira `Audit` mikroservis.

## Startup, JWT i konfiguracija

`Program` registruje DbContext, oba repozitorijuma i servisa, RabbitMQ publisher/consumer, oba hosted servisa, JWT, kontrolere, SignalR i OpenAPI. Na startu automatski primenjuje migracije. Middleware je HTTPS redirekcija → autentikacija → autorizacija → kontroleri/hub.

Konfiguracioni ključevi:

- `ConnectionStrings:DefaultConnection` — u Development/Compose-u zajednička `WarframeLFGBoardDb`;
- `Jwt:Key/Issuer/Audience` — isti parametri kao UsersDirectory;
- `RabbitMQ:Host/Port/Username/Password`;
- `CLEANUP_NOTIFICATIONS_AFTER` ili `Notifications:CleanupAfterMinutes`;
- standardni logging/hosts.

Bazni `appsettings.json` nema connection string; očekuje environment/Development override. Versioned JWT i infrastrukturalne lozinke su razvojne vrednosti i treba ih izmestiti u secrets.

## Deployment i testovi

Launch profil koristi `http://localhost:8085`; Compose mapira host 8005 → container 8080. Višefazni Dockerfile koristi .NET 10 SDK/runtime, instalira `libgssapi-krb5-2`, izlaže 8080/8443 i pokreće `UserNotification.dll`.

`UserNotification.Tests` pokriva favorites/notifications kontrolere, servise i notifications repository. Nisu evidentirani testovi za favorites repository, RabbitMQ consumer, SignalR hub ili cleanup service. Docker build kopira test projekat, ali ne izvršava `dotnet test`.

## Ključna ograničenja

- Status pretplate je snapshot iz JWT-a; servis nema revocation ni direktnu proveru UsersDirectory-ja.
- Nema outbox/inbox idempotency obrasca, dead-letter reda, paginacije ili health endpointa.
- Notification se čuva za svaki oglas, ne samo za predmete sa pratiocima.
- Kreiranje favorite-a ne potvrđuje da predmet postoji; user/item veze su tekstualne.
- Retention od pet minuta je vrlo kratak i cleanup radi hard delete.

## Kompletan inventar komponenti

- API/real-time: `UserFavoritesController`, `UserNotificationsController`, `UserNotificationHub`.
- Komande/modeli: `AddOrRemoveUserFavoritesCommand`, `UserFavorites`, `GroupRequestNotification`.
- Servisi: `IUserFavoritesService`/`UserFavoritesService`, `IUserNotificationsService`/`UserNotificationsService`, `NotificationCleanupService`.
- Repozitorijumi: `IUserFavoritesRepository`/`UserFavoritesRepository`, `IUserNotificationsRepository`/`UserNotificationsRepository`.
- Baza: `NotificationsDbContext`, `UserFavoriteEntity`, `UserNotificationEntity`, `UserNotificationDismissalEntity`, `UserFavoriteEntityConfiguration`, `UserNotificationEntityConfiguration`, `UserNotificationDismissalEntityConfiguration`, migracije i snapshot.
- Broker: `GroupRequestCreatedEvent`, `AuditEventMessage`, `IBrokerEventConsumer`, `NotificationEventConsumer`, `NotificationEventConsumerHostedService`, `IRabbitMQPublisher`, `RabbitMQPublisher`, `RabbitMQConnectionFactoryExtensions`.
- Hosting: `Program`, `UserNotification.csproj`, oba `appsettings` fajla, `launchSettings.json`, solution fajl i `Dockerfile`. Nema seedera, validatora ili AutoMapper profila.
