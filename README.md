# LifeAdmin

Aplikacja do śledzenia subskrypcji (Netflix, Spotify, siłownia…): pokazuje najbliższe terminy płatności, sumy kosztów i przypomina o zbliżających się opłatach.

Projekt do nauki ASP.NET Core Minimal API i Entity Framework Core.

## Funkcje

- **Konta użytkowników**: rejestracja i logowanie (cookie). Każdy widzi tylko swoje subskrypcje.
- **Subskrypcje**: dodawanie, edycja w tabeli, usuwanie, wyszukiwanie i sortowanie.
- **Cykle rozliczeń**: tygodniowy, miesięczny, kwartalny, roczny. Najbliższy termin jest liczony automatycznie od daty startu.
- **Status terminu**: kolorowa etykieta („dziś”, „jutro”, „za 3 dni”, „za 2 tygodnie”).
- **Wstrzymywanie**: wstrzymana subskrypcja zostaje w historii, ale nie wlicza się do sum.
- **Podsumowanie kosztów**: miesięcznie, rocznie i w najbliższych 30 dniach, osobno dla każdej waluty.
- **Przypomnienia w tle**: raz dziennie sprawdzane są terminy w ciągu 3 dni (na razie wpis w logu).
- **Swagger** do testowania API.

## Technologie

- .NET 10, ASP.NET Core Minimal API
- Entity Framework Core 10 + SQL Server (LocalDB lokalnie, SQL Server 2022 w Dockerze)
- Frontend: HTML, CSS i JavaScript bez frameworków (`wwwroot/ui`)
- Docker i Docker Compose

## Struktura projektu

```
Data/          AppDbContext (EF Core)
Endpoints/     endpointy: /auth, /subscriptions, /reminders
Migrations/    migracje bazy danych
Models/        encje i DTO
Services/      logika dat, mapowanie, przypomnienia (BackgroundService)
Validation/    walidacja DataAnnotations
wwwroot/ui/    interfejs w przeglądarce (index.html, login.html)
Program.cs     konfiguracja aplikacji
```

## Uruchomienie

### Opcja 1: lokalnie (Windows + LocalDB)

Wymagania: [.NET 10 SDK](https://dotnet.microsoft.com/download) i SQL Server LocalDB (instaluje się razem z Visual Studio).

```powershell
dotnet tool install --global dotnet-ef   # jednorazowo
dotnet ef database update                # tworzy bazę LifeAdminDb
dotnet run
```

Otwórz http://localhost:5132/ui i załóż konto.

### Opcja 2: Docker

Wymagania: [Docker Desktop](https://www.docker.com/products/docker-desktop/).

```bash
docker compose up --build
```

Otwórz http://localhost:8080/ui. Tabele w bazie tworzą się automatycznie przy pierwszym uruchomieniu.

| Komenda | Działanie |
|---|---|
| `docker compose down` | zatrzymuje kontenery, dane zostają |
| `docker compose down -v` | zatrzymuje kontenery i kasuje bazę |

Baza w Dockerze jest dostępna z SSMS pod adresem `localhost,14330` (login `sa`).

> ⚠️ Hasło do bazy w `docker-compose.yml` i `appsettings.Docker.json` jest przykładowe. Używaj go tylko lokalnie.

## API

Pełna dokumentacja: http://localhost:5132/swagger

| Metoda | Adres | Opis |
|---|---|---|
| POST | `/auth/register` | rejestracja |
| POST | `/auth/login` | logowanie |
| POST | `/auth/logout` | wylogowanie |
| GET | `/auth/me` | zalogowany użytkownik |
| GET | `/subscriptions` | lista (`search`, `sort`, `active`, `page`, `pageSize`) |
| GET | `/subscriptions/{id}` | jedna subskrypcja |
| GET | `/subscriptions/upcoming?days=7` | terminy w najbliższych dniach |
| POST | `/subscriptions` | dodanie |
| PUT | `/subscriptions/{id}` | edycja |
| PATCH | `/subscriptions/{id}/status` | wstrzymanie / wznowienie |
| DELETE | `/subscriptions/{id}` | usunięcie |
| GET | `/reminders` | wysłane przypomnienia |
| POST | `/reminders/run` | ręczne sprawdzenie przypomnień |

Endpointy `/subscriptions` i `/reminders` wymagają zalogowania.

## Konfiguracja

Przypomnienia ustawisz w `appsettings.json`:

```json
"Reminders": {
  "DaysAhead": 3,
  "CheckIntervalHours": 24
}
```

## Plany

- [ ] Kategorie i wykres kosztów
- [ ] Przypomnienia e-mail
- [ ] Testy jednostkowe i integracyjne
- [ ] Przeliczanie walut na PLN (API NBP)

## Licencja

Projekt jest udostępniony na licencji MIT, zobacz [LICENSE](LICENSE).
