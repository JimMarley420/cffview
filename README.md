# CFF View - Horaires Transports Suisses

Application WPF (.NET 10) affichant les prochains départs de transports publics suisses en temps réel.

![.NET](https://img.shields.io/badge/.NET-10.0-blue)
![License](https://img.shields.io/badge/License-MIT-green)
![Platform](https://img.shields.io/badge/Platform-Windows-red)

## Fonctionnalités

- **Recherche d'arrêts** - Autocomplete avec API transport.opendata.ch
- **Prochains départs** - Affichage temps réel (3 suivants)
- **Favoris** - Sauvegarde locale automatique
- **Mode offline** - Fallback GTFS si pas de connexion
- **Design Fluent** - Palette SBB moderne

## Captures d'écran

```
┌─────────────────────────────────┐
│  CFF View            ● Connecté  │
├─────────────────────────────────┤
│  🔍 Rechercher un arrêt...       │
│                                 │
│  ┌───────────────────────────┐  │
│  │ Lausanne                 │  │
│  │ Lausanne, Riponne       │  │
│  │ Lausanne, Bel-Air       │  │
│  └───────────────────────────┘  │
│                                 │
│  ┌───────────────────────────┐  │
│  │ 🟠 16:52  EC    Genève │  │
│  │     +2 min  Platform 8 │  │
│  ├───────────────────────────┤  │
│  │ 🟠 16:55  R     Bex  │  │
│  │     +2 min             │  │
│  └───────────────────────────┘  │
└─────────────────────────────────┘
```

## Installation

### Prérequis

- Windows 10/11
- .NET 10.0

### Depuis les sources

```bash
# Clone
git clone https://github.com/votre-user/cffview.git
cd cffview

# Restore & Build
cd cffview
dotnet restore
dotnet build

# Run
dotnet run --project cffview
```

### Executable

Télécharger `CFFView.exe` depuis [Releases](../../releases) et lancer directement.

## Configuration

Le fichier `appsettings.json` configure les URLs:

```json
{
  "Api": {
    "BaseUrl": "https://transport.opendata.ch/v1"
  },
  "Gtfs": {
    "DownloadUrl": "https://data.opentransportdata.swiss/..."
  }
}
```

## Architecture

```
cffview/
├── Models/           # Stop, Departure, Favorite, DTOs
├── ViewModels/       # MainViewModel (MVVM CommunityToolkit)
├── Services/         # TransportApi, Gtfs, Database
├── Converters/       # XAML converters
└── MainWindow.xaml  # UI WPF
```

### Services

| Service | Description |
|---------|------------|
| `TransportApiService` | Appels API opendata.ch |
| `GtfsService` | Parsing GTFS statique |
| `DatabaseService` | Stockage favoris JSON |

## Technologies

- **.NET 10** + WPF
- **CommunityToolkit.Mvvm** - MVVM pattern
- **CsvHelper** - Parsing GTFS
- **Serilog** - Logging

## API Utilisées

- [transport.opendata.ch](https://transport.opendata.ch) - API temps réel
- [opentransportdata.swiss](https://opentransportdata.swiss) - GTFS statique

## Contribution

1. Fork le projet
2. Créer une branche (`git checkout -b feature/nom`)
3. Commit (`git commit -m 'Ajouter feature'`)
4. Push (`git push origin feature/nom`)
5. Ouvrir une Pull Request

## License

MIT - Voir [LICENSE](LICENSE)

## Avertissement

Les données horaires sont fournies par les opérateurs de transports publics suisses via opentransportdata.swiss. L'application n'est pas affiliée aux CFF/SBB.