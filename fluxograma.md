# Fluxograma das Funções e Interações

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

> **Este fluxograma representa as principais funções e interações entre as camadas do sistema Oracle Version Control.**
