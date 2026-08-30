# ECommerce — Sistema de Gestão de Pedidos

Backend API para um e-commerce simples, implementado em .NET 10 com Clean Architecture, CQRS + MediatR e Minimal API.

## Arquitetura

O projeto segue **Clean Architecture** com as seguintes camadas:

```
src/
  Domain/              — Entidades, enums, invariantes (zero dependências)
  Application/         — Commands, Queries, Handlers, DTOs, Validators
  Infrastructure/      — Persistência, JWT, implementações concretas
  Api/                 — DI, autenticação, endpoints (Minimal API)

tests/
  ECommerce.Tests/     — Testes unitários e integração
```

### Dependências entre camadas

```
Domain ← Application
Domain ← Infrastructure
Application + Infrastructure ← Api
```

**Nenhuma dependência invertida.**

## Stack Técnico

- **.NET 10**
- **Minimal API** — endpoints sem Controllers, reduzindo código cerimonial
- **CQRS + MediatR** — separação clara entre Commands e Queries
- **FluentValidation** — validação de entrada via Pipeline Behavior
- **Entity Framework Core 9.0** — persistência (SQLite)
- **OpenAPI nativo + Swagger UI** — contrato gerado pelo ASP.NET Core com interface interativa em Development
- **xUnit** — testes unitários
- **Central Package Management (CPM)** — versionamento centralizado

## Configuração Inicial

### Pré-requisitos

- .NET 10 SDK
- Visual Studio 2022 ou VS Code

### Restaurar e compilar

```bash
dotnet restore
dotnet build
```

### Executar testes

```bash
dotnet test
```

## Decisões Técnicas

### Por que Minimal API?

Minimal APIs foram escolhidas devido ao pequeno número de endpoints — cinco no escopo atual — e à simplicidade do serviço. Controllers seriam igualmente válidos, mas adicionariam estrutura sem benefício relevante neste cenário. As responsabilidades de negócio permanecem isoladas na camada Application por meio de CQRS/MediatR; os endpoints atuam somente como adaptadores entre HTTP e os casos de uso.

### Por que separar endpoints e contratos do `Program.cs`?

O `Program.cs` permanece como composition root da aplicação. Endpoints são organizados por recurso e seus contratos HTTP ficam separados dos Commands e DTOs da Application. Rotas de pedidos compartilham o prefixo `/api/orders` por meio de `MapGroup`, centralizando apenas configuração comum ao recurso. Essa organização evita o crescimento do arquivo de inicialização sem introduzir um framework próprio ou uma abstração genérica de endpoints e mantém explícita a fronteira entre API e Application.

### Por que manter o `Program.cs` mínimo?

O startup declara apenas a composição necessária para o escopo já implementado: Application, Infrastructure, tratamento global de erros, migrations, documentação da API e endpoints. Configurações de JWT e CORS não são antecipadas; serão adicionadas somente se seus respectivos requisitos exigirem. Isso mantém o composition root legível e evita dependências e configurações sem uso concreto.

### Por que utilizar o OpenAPI nativo?

O contrato continua sendo gerado pelo suporte oficial do ASP.NET Core por meio de `AddOpenApi` e `MapOpenApi`. Em Development, o documento fica disponível em `/openapi/v1.json` e o Swagger UI em `/swagger`. Apenas o pacote de interface do Swashbuckle é utilizado, apontando para o documento nativo; assim evitamos manter dois geradores OpenAPI concorrentes. Fora de Development, documento e interface não são publicados.

### Por que separar Request HTTP e Command?

Os requests da API representam contratos externos sujeitos à evolução do protocolo HTTP, enquanto os Commands representam intenções e casos de uso da Application. Mesmo quando possuem os mesmos campos, tipos distintos evitam expor MediatR como contrato público e permitem que API e Application evoluam sem acoplamento desnecessário.

### Por que utilizar um Response HTTP dedicado?

A API não expõe entidades do Domain nem retorna diretamente resultados internos da Application. Um response próprio mantém o contrato HTTP estável e permite representar `OrderStatus` como texto legível sem alterar a persistência, que continua usando o mapeamento inteiro padrão do EF Core.

### Por que utilizar Problem Details?

Erros HTTP utilizam o suporte nativo do ASP.NET Core a Problem Details, seguindo o formato padronizado `application/problem+json`. Isso oferece respostas interoperáveis sem criar envelopes genéricos como `ApiResponse<T>` ou `ErrorResponse<T>`. O tratamento global das exceções será responsável por traduzir cada categoria de falha para o status HTTP apropriado.

### Por que centralizar o tratamento de exceções?

A API utiliza o mecanismo nativo `IExceptionHandler` do ASP.NET Core. Exceções atravessam um único ponto de tradução para Problem Details, são registradas com o contexto da requisição e recebem um `traceId` para correlação. Falhas do FluentValidation produzem HTTP 400 com mensagens agrupadas pelo caminho da propriedade; falhas inesperadas não expõem detalhes internos e produzem HTTP 500. Os endpoints permanecem focados na adaptação HTTP, sem blocos `try/catch` repetidos.

### Por que CQRS?

Separação clara entre operações de leitura (Queries) e escrita (Commands), facilitando testes, escalabilidade e manutenção.

### Por que FluentValidation via Pipeline Behavior?

Validação centralizada e reutilizável. Todos os Commands/Queries passam pelo mesmo pipeline, garantindo consistência.

### Por que Central Package Management?

Simplifica manutenção de versões em projetos multi-camadas. Uma única fonte de verdade em `Directory.Packages.props`.

## Mudanças Realizadas (Etapa A — Esqueleto Funcional)

### ✅ Atualização para .NET 10

- `Directory.Build.props` configurado com `TargetFramework` net10.0
- Todos os projetos (.csproj) atualizados

### ✅ Central Package Management (CPM)

- Criado `Directory.Packages.props` com versionamento centralizado
- Dependências obrigatórias:
  - **MediatR** 12.4.1
  - **FluentValidation** 11.10.0
  - **Entity Framework Core** 9.0.0
  - **Microsoft.EntityFrameworkCore.Sqlite** 9.0.0
  - **Microsoft.EntityFrameworkCore.Design** 9.0.0

### ✅ Configuração de Program.cs

- Composition root limitado às camadas, erros globais, migrations e endpoints
- Sem Controllers ou configurações antecipadas de JWT e CORS
- OpenAPI registrado com o suporte nativo do ASP.NET Core e disponibilizado pelo Swagger UI
- MediatR e FluentValidation registrados pela extensão da Application

### ✅ Limpeza de estrutura

- Removidos `.gitkeep` de todas as camadas
- Estrutura de pastas mantida vazia mas pronta para implementação

### ✅ Compilação validada

```
dotnet build → SUCESSO
Warnings: 8 (vulnerabilidade transitiva SQLite, XML comments menores)
Erros: 0
```

## Próximos Passos (Etapa B — Implementação de Negócio)

1. **Implementar Domínio**
   - Entidades: `Order`, `OrderItem`
   - Enums: `OrderStatus`
   - Invariantes e comportamento

2. **Implementar Application**
   - Commands: `CreateOrderCommand`, `CancelOrderCommand`, `LoginCommand`
   - Queries: `GetOrderByIdQuery`, `GetOrdersQuery`
   - Handlers e Validators
   - DTOs

3. **Implementar Infrastructure**
   - `OrderRepository` com EF Core
   - `JwtTokenService`
   - DbContext e migrations

4. **Implementar Endpoints (Minimal API)**
   - `POST /auth/login`
   - `POST /api/orders`
   - `GET /api/orders?page=1&pageSize=10`
   - `GET /api/orders/{id}`
   - `PATCH /api/orders/{id}/cancel`

5. **Testes**
   - Testes unitários de Handlers
   - Testes de Domínio
   - Testes de integração com `WebApplicationFactory`

6. **Infraestrutura Final**
   - Migrations automáticas no startup
   - Docker + docker-compose
   - SonarQube
   - Serilog + OpenTelemetry

## Validação de Arquitetura

✅ Estrutura de Clean Architecture respeitada  
✅ Nenhuma dependência invertida  
✅ .NET 10 configurado  
✅ MediatR e FluentValidation prontos  
✅ Minimal API estruturada  
✅ CPM funcional  
✅ Build sem erros críticos  

## Notas

- Quando JWT for implementado, a signing key deverá vir de configuração segura, nunca hardcoded
- SQLite será embedded; sem container de banco separado
- Migrations serão aplicadas automaticamente no startup (Etapa B)
- Serilog e OpenTelemetry serão adicionados na Etapa B conforme necessário
