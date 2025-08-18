# Fluxograma das Fun��es e Intera��es

```mermaid
graph TD
    subgraph UI (Views)
        LoginPage -->|Binding| LoginViewModel
        MainPage -->|Binding| MainViewModel
        ObjectDetailPage -->|Binding| ObjectDetailViewModel
    end

    subgraph ViewModels
        LoginViewModel -->|Usa| OracleService
        LoginViewModel -->|Usa| SettingsService
        MainViewModel -->|Usa| OracleService
        ObjectDetailViewModel -->|Usa| OracleService
    end

    subgraph Services
        OracleService -->|Acessa| OracleDB
        SettingsService -->|Acessa| Arquivo INI
    end

    MainViewModel -->|Manipula| VersionedObject
    ObjectDetailViewModel -->|Manipula| VersionedObject

    OracleDB((Oracle Database))
    Arquivo INI((settings.ini))
```

---

> **Este fluxograma representa as principais fun��es e intera��es entre as camadas do sistema Oracle Version Control.**
