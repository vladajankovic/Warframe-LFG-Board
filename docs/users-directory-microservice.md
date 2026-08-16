# UsersDirectory mikroservis

## Svrha i odgovornost

`UsersDirectory` je servis identiteta i korisničkih naloga. Registruje korisnike, čuva profil i hash lozinke, autentifikuje ih, izdaje JWT tokene, omogućava administraciju naloga i upravlja mesečnom pretplatom. Ostali servisi ne pozivaju ga direktno: veruju JWT-u koji on izdaje i iz claim-ova čitaju identitet, rolu i status pretplate.

Osnovni tok je kontroler → FluentValidation → `UserService` → `UsersRepository` → PostgreSQL. Uspešne promene i login proizvode best-effort audit događaje preko RabbitMQ reda `audit`.

## Tehnologije i organizacija

Servis je ASP.NET Core Web API na .NET 10. Koristi EF Core/Npgsql 10, AutoMapper 14, FluentValidation 12.1.1, SharpGrip MVC auto-validation, ASP.NET `PasswordHasher`, JWT Bearer i RabbitMQ.Client 7.2.1.

- `Controllers` — REST endpoint-i i provera claim-ova.
- `Commands` — request/command ugovori.
- `Validators` — pravila emaila, lozinke i update-a.
- `Services` — nalog, token, pretplata i background obnova.
- `Repositories` — EF pristup korisnicima.
- `Database` — DbContext, entitet i konfiguracija.
- `Seed` — inicijalni administrator.
- `Utility` — email/password pravila i AutoMapper.
- `Broker` — RabbitMQ publisher i audit ugovor.
- `Models`/`Enums` — izlazni modeli i subscription enum.

## Entitet i konfiguracija baze

### `UserEntity`

| Polje | Tip / default | EF pravilo i namena |
|---|---|---|
| `Id` | `string`, novi GUID | PK, obavezno; koristi se kao JWT `sub` |
| `Email` | `string` | obavezno, max 254, jedinstven indeks |
| `Password` | `string` | obavezan hash, bez DB max dužine |
| `Role` | `string = "User"` | obavezno, max 50, DB default `User` |
| `InGameName` | `string = ""` | obavezno; EF nema max dužinu |
| `CreatedAt` | `DateTime` | obavezno, UTC pri kreiranju |
| `LastUpdatedAt` | `DateTime` | obavezno, UTC pri izmeni |
| `VerificationCode` | `string` | obavezno, max 64, 32-char GUID bez crtica |
| `IsVerified` | `bool = false` | obavezno, DB default false |
| `SubscriptionStatus` | `Inactive` | enum čuvan kao string (`Inactive`/`Active`) |
| `SubscriptionExpiryDate` | nullable `DateTime` | kraj tekućeg subscription perioda |

`UsersDbContext` koristi šemu `UsersDirectorySchema`, `DbSet<UserEntity> Users`, učitava sve konfiguracije iz assembly-ja i eksplicitno konvertuje subscription enum u string. EF migration history je takođe u ovoj šemi.

Migracije dokumentuju razvoj: `InitialMigration`, `RemovePasswordLengthInDatabase`, `AddUserRole`, `AddUserVerification`, `AddInGameName`, `AddSchema` i `AddSubscriptionFields`. Migracije se automatski izvršavaju pri startu aplikacije.

## Seed baze

`DbSeed.SeedUsers()` se izvršava posle migracija. Idempotentno traži `admin@admin.com`:

- ako ne postoji, kreira verifikovanog korisnika sa rolom `Admin`, praznim in-game imenom, verification kodom i hashovanom početnom lozinkom `admin123`;
- ako postoji ali nije Admin, menja mu rolu;
- ako već ima odgovarajuću rolu, preskače seed.

Početna lozinka ne zadovoljava validator registracije (nema veliko slovo ni specijalni znak) i nalazi se u kodu, što je namenjeno samo razvojnom okruženju.

## Komande i izlazni modeli

| Tip | Polja | Upotreba |
|---|---|---|
| `AddUserCommand` | `Email`, `Password` | registracija i potvrda kredencijala pri brisanju sopstvenog naloga |
| `LoginCommand` | `Email`, `Password` | login |
| `UpdateUserCommand` | `Email`, `Password`, `Role`, `InGameName`, `IsVerified` | administratorski update i interni full update |
| `ChangePasswordCommand` | `Email`, `OldPassword`, `NewPassword` | promena sopstvene lozinke |
| `UpdateInGameNameCommand` | `Email`, `InGameName` | promena sopstvenog imena u igri |

`User` vraća email, rolu, in-game ime, datume, verifikaciju i pretplatu, ali ne vraća `Id`, hash ili verification code. `LoginResponse` dodatno vraća `UserId` i JWT. `SubscriptionInfo` vraća status, datum isteka i opcioni osveženi token. Subscription enum se u JSON-u serijalizuje kao string. AutoMapper ima mapu `UserEntity → User`.

## Validacija i utility

`AddUserCommandValidator` zahteva email i lozinku; email mora proći `EmailUtils`, biti do 254 karaktera, a lozinka mora proći `PasswordUtils`. `ChangePasswordCommandValidator` isto proverava obe lozinke i zabranjuje da stara i nova budu iste. `UpdateUserCommandValidator` zahteva sva polja, validan email/lozinku, rolu i in-game ime; za `bool IsVerified` koristi `NotEmpty`, što u FluentValidation-u odbija `false` i praktično otežava administratorsko postavljanje na false. `UpdateInGameNameCommandValidator` postoji i zahteva email i ime do 50 znakova, ali nije registrovan u `Program.cs`, za razliku od ostala tri validatora.

`PasswordUtils` zahteva 8–256 znakova, najmanje jedno veliko slovo, malo slovo, cifru i znak iz definisanog skupa specijalnih znakova. `EmailUtils` ručno implementira široku RFC 5321/5322 proveru: display name, quoted local part, boundary komentare, IP literal, maksimalne dužine, pravila hostname labela i ne-numerički TLD. Ne radi DNS/proveru postojanja adrese.

## Repozitorijum

`UsersRepository` nudi create, delete, list, get po emailu, full update i update pretplate. Kreiranje postavlja UTC datume i verification code. `UpdateUser` uvek menja lozinku, ime i verifikaciju; rolu menja samo ako postojeći korisnik nije `Admin`, čime štiti admin rolu. `UpdateSubscription` menja status, expiry i `LastUpdatedAt`. Sve izmene odmah pozivaju `SaveChangesAsync`; čitanja nemaju `AsNoTracking`, sortiranje ni paginaciju.

Metode pretpostavljaju da korisnik postoji posle prethodne service provere i dereferenciraju nullable rezultat bez dodatne zaštite.

## `UserService` poslovna logika

- `CreateUser` odbija postojeći email, hashira lozinku ASP.NET Identity hasher-om, kreira nalog, šalje audit `Create` i vraća javni model.
- `LoginUser` nalazi korisnika i verifikuje hash. Uspeh ili `SuccessRehashNeeded` prihvata, ali u drugom slučaju ne upisuje novi hash. Vraća profil i osmočasovni JWT; šalje audit `Login`. Kod trenutno ne proverava `IsVerified` iako postoji `IsUserVerified` metoda.
- `GetAllUsers`/`GetUser` mapiraju entitete u javni model.
- `UpdateUser` zahteva postojeći nalog, po potrebi hashira prosleđenu lozinku, delegira full update i šalje audit `Update`.
- `ChangePassword` proverava stari hash, zabranjuje istu novu vrednost, hashira novu i koristi full update uz očuvanje ostalih polja.
- `UpdateInGameName` koristi full update i čuva postojeći hash, rolu i verifikaciju.
- `DeleteUser` je admin brisanje bez provere lozinke; `DeleteAccount` je self-service brisanje sa proverom hash-a.
- `GetSubscription` vraća trenutno stanje bez tokena.
- `ActivateSubscription` aktivira jedan mesec od trenutnog UTC vremena i vraća novi JWT.
- `CancelSubscription` odmah postavlja status `Inactive`, ali zadržava raniji expiry datum, i vraća novi JWT.

Audit publish je u `try/catch`; failure se loguje i ne poništava poslovnu operaciju. Audit `User` polje nije sasvim standardizovano: neke metode šalju ID, neke email.

## JWT token

`GenerateJwtToken` koristi HMAC-SHA256 i konfiguraciju `Jwt:Key/Issuer/Audience`. Token važi osam sati i sadrži:

- `sub` = interni korisnički ID;
- `email` = email;
- role claim = `Role`;
- `jti` = novi GUID;
- `subscription_status` = `Inactive` ili `Active`.

ItemsStore, GroupRequests, Audit i UserNotification validiraju potpis istim shared secret-om. GroupRequests limit i UserNotification favorites/notification pristup čitaju `subscription_status`; zbog toga promena pretplate postaje vidljiva tek kada klijent prihvati novi token iz activate/cancel odgovora ili se ponovo prijavi.

## Background obnova pretplate

`SubscriptionRenewalService` je hosted service sa `PeriodicTimer` intervalom od 24 sata. Prvi ciklus radi tek posle prvog tick-a. Učitava sve korisnike, bira aktivne čiji je expiry istekao i svakome dodaje jedan mesec na stari expiry, zadržavajući status `Active`. Objavljuje audit `Subscription_Renewed`. Greške pojedinačnog audit publish-a ne prekidaju obnovu; šira greška ciklusa se loguje. Ako servis duže ne radi ili datum kasni više meseci, jedan dnevni ciklus dodaje samo po jedan mesec.

## RabbitMQ

`AuditEventMessage` ima UTC vreme, `Service = "UsersDirectory"`, akciju, user i detalje. `RabbitMQPublisher` deklariše durable red i objavljuje persistent JSON poruku direktnim routing key-em; postoji i neiskorišćen topic-exchange overload. Svaki publish otvara i zatvara konekciju/kanal. Konekcija ima najviše pet retry pokušaja sa razmakom od pet sekundi.

## HTTP API

Bazna ruta je `/v1/directory`.

| Metod i ruta | Pristup | Rezultat / napomena |
|---|---|---|
| `GET /users` | Admin | `200` svi javni korisnički modeli |
| `GET /user?email=` | Admin | `200` ili `404` |
| `POST /register` | javno | `201`; `400` za duplikat/ostale greške |
| `POST /login` | javno | `200 LoginResponse`; nevažeći kredencijali daju `401` |
| `DELETE /user?email=` | Admin | `200` ili `404` |
| `PATCH /user` | Admin | full update; `200` ili `400` |
| `PATCH /user/me/update` | autentifikovan | menja lozinku; email u body-ju mora odgovarati email claim-u, inače `403` |
| `PATCH /user/me/ingamename` | autentifikovan | menja in-game ime uz istu email/claim proveru |
| `DELETE /user/me/delete` | autentifikovan | body sadrži email i lozinku; `200`, `401`, `404`, `403` ili `400` |
| `GET /user/me/subscription` | autentifikovan | trenutni status i expiry |
| `POST /user/me/subscription/activate` | autentifikovan | aktivira pretplatu i vraća novi token |
| `DELETE /user/me/subscription/cancel` | autentifikovan | otkazuje i vraća novi token |

Nginx prosleđuje `/v1/directory` na `users-directory:8080`.

## Startup, konfiguracija i deployment

`Program` registruje DbContext, AutoMapper, repository/service, password hasher, subscription hosted service, RabbitMQ, validatore, seed, JWT, kontrolere i OpenAPI. Na startu primenjuje migracije i seed. U production-u uključuje HSTS; middleware zatim radi HTTPS redirekciju, autentikaciju i autorizaciju.

Obavezni ključevi su `ConnectionStrings:DefaultConnection`, `Jwt:*` i `RabbitMQ:*`. Development/Compose koriste `WarframeLFGBoardDb`; shared JWT ključ i DB/RabbitMQ kredencijali trenutno su u versioned konfiguraciji i treba ih tretirati kao razvojne tajne.

Launch profil je `http://localhost:8081`. Compose mapira host 8001 → 8080 i zavisi od zdravih PostgreSQL/RabbitMQ servisa. Dockerfile koristi višefazni .NET 10 build, instalira `libgssapi-krb5-2`, izlaže 8080/8443 i pokreće `UsersDirectory.dll`.

## Testovi i ograničenja

`UsersDirectory.Tests` trenutno sadrži `UserServiceTests`. Docker build kopira test projekat, ali ne izvršava testove. Ključna ograničenja su: nema refresh tokena/revocation-a, nema email slanja ni endpointa za verification code, nema rate limiting-a/lockout-a, nema normalizacije emaila, nema paginacije i nema distribuirane transakcije/outbox-a za audit događaje.

## Kompletan inventar komponenti

- API/komande: `UsersController`, `AddUserCommand`, `LoginCommand`, `UpdateUserCommand`, `ChangePasswordCommand`, `UpdateInGameNameCommand`.
- Validatori: `AddUserCommandValidator`, `UpdateUserCommandValidator`, `ChangePasswordCommandValidator`, `UpdateInGameNameCommandValidator`.
- Poslovni sloj: `IUsersService`, `UserService`, `SubscriptionRenewalService`.
- Data sloj: `IUsersRepository`, `UsersRepository`, `UsersDbContext`, `UserEntity`, `UserEntityTypeConfiguration`, migracije i snapshot.
- Modeli/enum: `User`, `LoginResponse`, `SubscriptionInfo`, `SubscriptionStatus`.
- Utility/seed: `EmailUtils`, `PasswordUtils`, `AutoMapperProfile`, `DbSeed`.
- Broker: `AuditEventMessage`, `IRabbitMQPublisher`, `RabbitMQPublisher`, `RabbitMQConnectionFactoryExtensions`.
- Hosting: `Program`, `UsersDirectory.csproj`, oba `appsettings` fajla, `launchSettings.json`, solution fajl i `Dockerfile`. Servis nema RabbitMQ consumer niti dodatne kontrolere.
