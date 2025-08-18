# Oracle Version Control

Oracle Version Control � um sistema de controle de vers�o para objetos Oracle, desenvolvido em .NET MAUI (.NET 9), com interface moderna e integra��o a banco de dados Oracle. O sistema permite gerenciar check-in/check-out de objetos, visualizar status, baixar DDLs e manter hist�rico de altera��es.

## Funcionalidades
- Login seguro com armazenamento de configura��es
- Listagem, busca e pagina��o de objetos versionados
- Check-out e check-in de objetos Oracle
- Download do DDL do objeto selecionado
- Visualiza��o de detalhes do objeto
- Ordena��o e filtragem por m�ltiplos campos
- Interface responsiva e moderna

## Estrutura do Projeto
- **Views**: Telas (LoginPage, MainPage, ObjectDetailPage)
- **ViewModels**: L�gica de apresenta��o (LoginViewModel, MainViewModel, ObjectDetailViewModel)
- **Models**: Representa��o dos dados (VersionedObject)
- **Services**: Servi�os de acesso a dados e configura��es (OracleService, SettingsService)
- **Resources**: �cones, splash, etc.

## Fluxo de Uso
1. **Login**: Usu�rio informa credenciais e configura��es Oracle.
2. **MainPage**: Ap�s login, exibe lista de objetos versionados, com busca, ordena��o e pagina��o.
3. **Check-out/Check-in**: Usu�rio pode reservar (check-out) ou liberar (check-in) objetos, sempre com coment�rio obrigat�rio.
4. **Detalhes**: Ao selecionar um objeto, � poss�vel ver detalhes e baixar o DDL.
5. **Logout**: Encerra a sess�o e retorna ao login.

## Diagrama de Fluxo e Intera��o de Camadas

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

## Principais Classes e Responsabilidades
- **LoginViewModel**: Gerencia autentica��o, carrega/salva configura��es.
- **MainViewModel**: Gerencia listagem, busca, pagina��o, check-in/out e download de objetos.
- **ObjectDetailViewModel**: Exibe detalhes do objeto selecionado.
- **OracleService**: Conex�o, queries e procedimentos Oracle (check-in/out, busca, DDL).
- **SettingsService**: Persist�ncia de configura��es locais.
- **VersionedObject**: Modelo de dados dos objetos versionados.

## Requisitos
- .NET 9
- .NET MAUI
- Oracle.ManagedDataAccess
- UraniumUI (UI)

## Observa��es
- O projeto n�o altera objetos Oracle sem check-out pr�vio.
- Coment�rios s�o obrigat�rios para check-in/out.
- Configura��es s�o salvas em arquivo local (settings.ini).

---

> **Este README e fluxograma foram gerados automaticamente a partir do c�digo-fonte.**
