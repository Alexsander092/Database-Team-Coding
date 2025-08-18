# Database Team Coding

Oracle Version Control é um sistema de controle de versão para objetos Oracle, desenvolvido em .NET MAUI (.NET 9), com interface moderna e integração a banco de dados Oracle. O sistema permite gerenciar check-in/check-out de objetos, visualizar status, baixar DDLs e manter histórico de alterações.

## Funcionalidades
- Login seguro com armazenamento de configurações
- Listagem, busca e paginação de objetos versionados
- Check-out e check-in de objetos Oracle
- Download do DDL do objeto selecionado
- Visualização de detalhes do objeto
- Ordenação e filtragem por múltiplos campos
- Interface responsiva e moderna

## Estrutura do Projeto
- **Views**: Telas (LoginPage, MainPage, ObjectDetailPage)
- **ViewModels**: Lógica de apresentação (LoginViewModel, MainViewModel, ObjectDetailViewModel)
- **Models**: Representação dos dados (VersionedObject)
- **Services**: Serviços de acesso a dados e configurações (OracleService, SettingsService)
- **Resources**: Ícones, splash, etc.

## Fluxo de Uso
1. **Login**: Usuário informa credenciais e configurações Oracle.
2. **MainPage**: Após login, exibe lista de objetos versionados, com busca, ordenação e paginação.
3. **Check-out/Check-in**: Usuário pode reservar (check-out) ou liberar (check-in) objetos, sempre com comentário obrigatório.
4. **Detalhes**: Ao selecionar um objeto, é possível ver detalhes e baixar o DDL.
5. **Logout**: Encerra a sessão e retorna ao login.

## Architecture Overview

flowchart TD
    subgraph UI["UI (Views)"]
        A[LoginPage] -->|Binding| B[LoginViewModel]
        C[MainPage] -->|Binding| D[MainViewModel]
        E[ObjectDetailPage] -->|Binding| F[ObjectDetailViewModel]
    end

    subgraph ViewModels["ViewModels"]
        B -->|Uses| G[OracleService]
        B -->|Uses| H[SettingsService]
        D -->|Uses| G
        F -->|Uses| G
    end

    subgraph Services["Services"]
        G -->|Connects to| I[[Oracle Database]]
        H -->|Reads/Writes| J[[settings.ini]]
    end

    D -->|Manages| K[VersionedObject]
    F -->|Manages| K

    classDef box fill:#f9f9f9,stroke:#333,stroke-width:2px;
    classDef db fill:#e6f3ff,stroke:#0066cc,stroke-width:2px;
    class I,J db;
    class UI,ViewModels,Services box;

## Principais Classes e Responsabilidades
- **LoginViewModel**: Gerencia autenticação, carrega/salva configurações.
- **MainViewModel**: Gerencia listagem, busca, paginação, check-in/out e download de objetos.
- **ObjectDetailViewModel**: Exibe detalhes do objeto selecionado.
- **OracleService**: Conexão, queries e procedimentos Oracle (check-in/out, busca, DDL).
- **SettingsService**: Persistência de configurações locais.
- **VersionedObject**: Modelo de dados dos objetos versionados.

## Requisitos
- .NET 9
- .NET MAUI
- Oracle.ManagedDataAccess
- UraniumUI (UI)

## Observações
- O projeto não altera objetos Oracle sem check-out prévio.
- Comentários são obrigatórios para check-in/out.
- Configurações são salvas em arquivo local (settings.ini).

---

> **Este README e fluxograma foram gerados automaticamente a partir do código-fonte.**
