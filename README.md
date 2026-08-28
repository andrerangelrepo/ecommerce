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
- **JWT** — autenticação stateless
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

Minimal API reduz código cerimonial em comparação com Controllers tradicionais. Cada endpoint é um adapter HTTP → MediatR, mantendo a lógica de negócio isolada na camada Application.

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

- Removido `AddControllers()` e `MapControllers()`
- Adicionado `AddMediatR()` com auto-registro de handlers
- Implementado `ValidationBehavior<TRequest, TResponse>` para FluentValidation
- Mantido Swagger/OpenAPI com documentação JWT
- Mantido JWT e CORS conforme configuração

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

- A signing key JWT não é hardcoded; usar `appsettings.json` ou variáveis de ambiente
- SQLite será embedded; sem container de banco separado
- Migrations serão aplicadas automaticamente no startup (Etapa B)
- Serilog e OpenTelemetry serão adicionados na Etapa B conforme necessário
