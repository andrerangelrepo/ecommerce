# Rules — Teste Prático Desenvolvedor .NET Senior

> Guideline operacional específica deste desafio.
> Use este arquivo no lugar do `rules.md` original durante a implementação.

---

## 1. Precedência

A ordem de autoridade é:

1. `Teste Prático — Desenvolvedor .NET Senior.md` — contrato soberano.
2. Este arquivo — regras técnicas adaptadas ao desafio.
3. `workflows.md` — referência auxiliar apenas quando compatível.
4. Código/convenções existentes — preservar quando compatíveis.

**Em qualquer conflito, o Teste Prático prevalece.**

Não alterar requisitos literais do desafio para adequá-los a outra guideline.

---

## 2. Princípio do projeto

Priorizar:

- clareza;
- arquitetura;
- testabilidade;
- baixo acoplamento;
- facilidade de explicar e modificar ao vivo.

> Não adicionar abstrações, bibliotecas ou padrões apenas para demonstrar conhecimento.

Implementar a menor solução completa que satisfaça o requisito.

---

## 3. Stack e decisões

Entrega final:

- .NET 10;
- **Minimal API**;
- Clean Architecture;
- CQRS + MediatR;
- FluentValidation via MediatR Pipeline Behavior;
- EF Core + SQLite;
- migrations automáticas no startup;
- JWT;
- xUnit;
- Dockerfile + docker-compose;
- README.

Desejáveis a implementar após o núcleo funcional:

- Serilog;
- LoggingBehavior;
- `WebApplicationFactory`;
- SonarQube/dotnet-sonarscanner;
- OpenTelemetry com console exporter.

### Simplificações deliberadas

Não utilizar sem requisito real:

- Dapper;
- `Csh.Shared`;
- API Gateway/gateways HTTP corporativos;
- Unit of Work próprio;
- generic repository;
- AutoMapper;
- Specification Pattern;
- Domain Events/Event Bus;
- projeto `CrossCutting` obrigatório;
- wrappers customizados de resposta apenas por padronização.

**EF Core será a única tecnologia de persistência definitiva**, inclusive para paginação.

---

## 4. Arquitetura

Estrutura esperada:

```text
src/
  Domain/
  Application/
  Infrastructure/
  Api/

tests/
  Domain.Tests/
  Application.Tests/
  Api.IntegrationTests/
```

Dependências:

```text
Domain <- Application
Domain <- Infrastructure
Application + Infrastructure <- Api
```

### Domain

- zero dependências das demais camadas;
- entidades, enums, invariantes e comportamento;
- não conhece HTTP, MediatR, EF Core, JWT ou logging.

### Application

- depende somente de Domain;
- Commands, Queries, Handlers, DTOs, Validators e mappings;
- orquestra casos de uso;
- não conhece HTTP ou EF Core.

### Infrastructure

- implementa interfaces das camadas internas;
- persistência e JWT concreto;
- **zero regra de negócio**.

### Api

- DI, autenticação, OpenAPI, exception handling e endpoints;
- converte HTTP em Commands/Queries;
- **zero regra de negócio**;
- endpoint não acessa repository/DbContext diretamente.

---

## 5. Desenvolvimento em duas etapas

### Etapa A — código funcional com persistência InMemory

Deve possuir:

- domínio completo;
- todos os casos de uso;
- endpoints;
- JWT;
- validações;
- testes unitários;
- integração HTTP possível;
- `InMemoryOrderRepository`.

Não antecipar:

- SQLite/migrations;
- Docker;
- SonarQube.

### Etapa B — infraestrutura definitiva

Trocar:

```text
InMemoryOrderRepository
        ↓
OrderRepository (EF Core + SQLite)
```

Adicionar migrations, Docker, SonarQube, Serilog/OpenTelemetry e documentação final.

**A troca de repository não deve exigir reescrever Domain, Handlers, Validators ou endpoints.**

---

## 6. Domínio

### Order

```text
Id          Guid
CustomerId  Guid
Status      OrderStatus
CreatedAt   DateTime
Items       IReadOnlyCollection<OrderItem>
TotalAmount decimal calculado
```

`OrderStatus`:

```text
Pending
Confirmed
Cancelled
```

### OrderItem

```text
Id          Guid
OrderId     Guid
ProductName string
Quantity    int
UnitPrice   decimal
```

### Convenções

- entidades são `class`;
- estado protegido por `private set`/coleções somente leitura;
- comportamento pertence às entidades;
- DTOs não devem virar modelo de domínio.

### Regras obrigatórias

1. Order possui pelo menos 1 item.
2. `UnitPrice > 0`.
3. `Quantity > 0`.
4. somente `Pending` pode ser cancelado.
5. `TotalAmount = Σ(UnitPrice * Quantity)`.
6. `TotalAmount` é calculado **exclusivamente no Domain**.

Regras de sanidade simples permitidas:

- `CustomerId != Guid.Empty`;
- `ProductName` não vazio.

Não inventar regras comerciais adicionais.

### Cancelamento

A transição de estado pertence ao Domain, por exemplo via `Order.Cancel()`.

É proibido alterar `Status` diretamente no endpoint ou Handler.

---

## 7. Validação e erros

### FluentValidation

Usar para input/Command:

- campos obrigatórios;
- coleção obrigatória;
- Guid válido;
- `Quantity > 0`;
- `UnitPrice > 0`;
- strings obrigatórias.

A execução deve ocorrer em `IPipelineBehavior<,>` do MediatR.

### Domain

O Domain **também protege suas invariantes**.

Nunca depender apenas do FluentValidation para impedir entidade inválida.

### Error handling HTTP

Centralizar via `IExceptionHandler`/ProblemDetails do ASP.NET Core ou equivalente simples.

Endpoints não devem conter `try/catch` repetitivo.

Mapeamento esperado:

| Caso | HTTP |
|---|---:|
| validação de entrada | 400 |
| não autenticado | 401 |
| não encontrado | 404 |
| conflito de estado (ex. cancelar não-Pending) | 409 |
| domínio inválido | 400 ou 422, consistente e documentado |
| erro inesperado | 500 |

### Responses

Não é obrigatório `ApiResponse<T>`.

Preferir:

- DTO direto em sucesso;
- `ProblemDetails`/`ValidationProblemDetails` em erro;
- `201 Created` + `Location` no POST de Order.

---

## 8. CQRS + MediatR

### Commands

```text
LoginCommand
CreateOrderCommand
CancelOrderCommand
```

### Queries

```text
GetOrderByIdQuery
GetOrdersQuery
```

### Handlers

Um Handler por Command/Query.

Handler pode:

- orquestrar caso de uso;
- chamar comportamento do Domain;
- usar interfaces de infraestrutura;
- mapear Domain -> DTO;
- propagar `CancellationToken`.

Handler não pode:

- acessar `HttpContext`;
- retornar `IResult`;
- usar DbContext/EF Core diretamente;
- executar SQL;
- recalcular `TotalAmount`;
- implementar regra que pertence ao Domain.

---

## 9. Organização e naming

Estrutura recomendada:

```text
Application/
  Features/
    Orders/
      Commands/
        CreateOrder/
        CancelOrder/
      Queries/
        GetOrderById/
        GetOrders/
      DTOs/
      Mappings/
    Auth/
      Commands/
        Login/
      DTOs/
  Behaviors/
```

Naming:

```text
{Action}{Entity}Command
Get{Entity}By{Criteria}Query
{Action}{Entity}Handler
{Action}{Entity}Validator
{Action}{Entity}RequestDto
{Action}{Entity}ResponseDto
{Entity}Dto
```

DTOs: preferir `record`, imutáveis e sem comportamento.

JSON: camelCase.

---

## 10. Async

Métodos async próprios devem terminar em `Async`:

```text
GetByIdAsync
GetPagedAsync
AddAsync
UpdateAsync
GenerateTokenAsync (se realmente async)
```

**Não renomear métodos impostos por frameworks**, como `IRequestHandler.Handle`.

Propagar `CancellationToken` em I/O async.

---

## 11. Repository

Criar somente:

```text
IOrderRepository
```

Não criar:

```text
IRepository<T>
```

Contrato deve suportar, no mínimo, operações equivalentes a:

```text
AddAsync
GetByIdAsync
GetPagedAsync
UpdateAsync
```

### Etapa A

`InMemoryOrderRepository`:

- compartilhado entre requests;
- thread-safe;
- não expõe coleção interna mutável;
- suporta paginação;
- determinístico para testes.

### Etapa B

`OrderRepository`:

- EF Core;
- SQLite;
- Fluent API;
- relacionamento Order/OrderItem;
- migrations.

Não introduzir Dapper.

---

## 12. Paginação

Contrato literal:

```http
GET /api/orders?page=1&pageSize=10
```

Parâmetros obrigatórios:

- `page`;
- `pageSize`.

**Nunca substituir `page` por `pageNumber`.**

Validar:

- `page >= 1`;
- `pageSize >= 1`;
- limite máximo simples pode ser adotado (ex.: 100).

Ordenação deve ser determinística.

Pode existir um `PagedResult<T>` local simples na Application.

Não criar dependência externa só para paginação.

---

## 13. Contrato HTTP — literal

Estas rotas são soberanas:

```http
POST  /auth/login
POST  /api/orders
GET   /api/orders?page=1&pageSize=10
GET   /api/orders/{id}
PATCH /api/orders/{id}/cancel
```

Regras:

- não adicionar `/v1`;
- não aplicar `/{capability}/{resource}/v{n}`;
- não criar aliases extras sem pedido explícito;
- `/auth/login` é anônimo;
- todo `/api/orders...` exige JWT.

Status esperados:

| Endpoint/caso | Status |
|---|---:|
| login válido | 200 |
| login inválido | 401 |
| create válido | 201 |
| list/get válido | 200 |
| paginação inválida | 400 |
| não autenticado | 401 |
| Order inexistente | 404 |
| cancelamento inválido por status | 409 |
| cancelamento válido | 200 ou 204, manter consistente |

---

## 14. Minimal API

Organizar por feature:

```text
Api/Endpoints/AuthEndpoints.cs
Api/Endpoints/OrderEndpoints.cs
```

Endpoint deve fazer apenas:

```text
bind request
→ construir Command/Query
→ mediator.Send(...)
→ retornar resultado HTTP
```

Proibido no endpoint:

- calcular total;
- verificar `Order.Status`;
- usar repository/DbContext;
- gerar JWT diretamente;
- regra de negócio;
- try/catch repetitivo.

Documentar no OpenAPI:

- summary;
- description;
- principais status codes;
- autenticação.

---

## 15. JWT

Credencial fixa do desafio:

```text
Email: dev@martech.com
Senha: Senha@123
```

Pode ser mantida em memória.

A **signing key JWT não pode ser hardcoded**; usar configuration/environment.

Criar abstração simples, por exemplo:

```text
ITokenService      (camada interna)
JwtTokenService    (Infrastructure)
```

O endpoint não gera token diretamente.

Claims mínimas recomendadas:

- `sub`;
- `email`;
- `jti`;
- `exp`.

Não criar refresh token, roles ou banco de usuários sem requisito.

---

## 16. Logging e observabilidade

### LoggingBehavior

Registrar:

- Command/Query;
- request seguro;
- response seguro;
- tempo de execução.

Nunca logar:

- senha;
- signing key;
- JWT completo.

Na Etapa B configurar Serilog.

### OpenTelemetry

Na Etapa B manter básico:

- ASP.NET Core instrumentation;
- HTTP instrumentation quando aplicável;
- console exporter.

Não adicionar stack externa de observabilidade sem necessidade.

---

## 17. Testes

### Obrigatório

Todos os Handlers possuem testes unitários em xUnit.

### Domain

Cobrir:

- Order válida;
- total com 1 item;
- total com vários itens;
- Order sem itens;
- UnitPrice zero e negativo;
- Quantity zero e negativa;
- cancelar Pending;
- rejeitar cancelamento de Confirmed;
- rejeitar cancelamento de Cancelled.

### Handlers

Happy path + falha relevante para:

- LoginHandler;
- CreateOrderHandler;
- GetOrderByIdHandler;
- GetOrdersHandler;
- CancelOrderHandler.

### Integração

Usar `WebApplicationFactory`.

Testes devem:

- ser independentes de ordem;
- não depender da internet;
- não depender de banco externo;
- usar InMemory na Etapa A;
- poder usar SQLite temporário na Etapa B.

### Métrica de qualidade

Priorizar:

- 100% dos Handlers testados;
- 100% das regras explícitas testadas;
- cenários positivos e negativos críticos.

Não criar testes triviais apenas para inflar coverage.

---

## 18. Code quality

Aplicar:

- SOLID quando agrega valor;
- KISS;
- DRY sem abstração prematura;
- nullable reference types;
- nomes claros;
- métodos pequenos;
- zero warnings novos relevantes.

### Constantes

Não criar projeto/classe global de constantes para cada literal.

Constantes compartilhadas ficam próximas da feature/camada que as possui.

### XML comments

Não são obrigatórios em todo membro público.

Usar quando acrescentarem informação que o código não expressa sozinho.

---

## 19. README vivo

Atualizar quando mudar algo observável:

- arquitetura;
- execução;
- endpoints;
- configuração;
- persistência;
- Docker;
- testes;
- decisão técnica relevante.

README final deve explicar:

- objetivo;
- Clean Architecture;
- escolha de Minimal API;
- CQRS/MediatR;
- endpoints;
- JWT e credencial de teste;
- paginação;
- execução local;
- testes;
- EF Core + SQLite;
- migrations;
- Docker;
- SonarQube;
- Serilog/OpenTelemetry;
- principais trade-offs.

Não documentar como pronto algo ainda não implementado.

---

## 20. Etapa B — infraestrutura

### EF Core + SQLite

- migrations;
- aplicação automática das migrations no startup;
- relacionamento Order/OrderItem;
- persistência sobre restart.

### Docker

Dockerfile:

- multi-stage;
- SDK somente no build;
- runtime na imagem final;
- sem segredos baked.

Docker Compose:

- API;
- SonarQube quando configurado;
- SQLite permanece embedded, sem container de banco separado;
- volume para arquivo SQLite se necessário.

### SonarQube

Objetivos:

- 0 Blocker;
- 0 Critical;
- revisar hotspots;
- não silenciar issues somente para deixar gate verde.

---

## 21. Proibições

Não fazer:

- lógica de negócio em Api;
- lógica de negócio em Infrastructure;
- `IRepository<T>` sem motivo real;
- Dapper neste desafio;
- dependência de biblioteca corporativa privada;
- `TotalAmount` fora do Domain;
- alteração direta de `Order.Status` fora do Domain;
- alterar rotas do enunciado;
- usar `pageNumber` no lugar de `page`;
- hardcode da JWT signing key;
- entregar sem unit tests dos Handlers;
- antecipar infraestrutura futura sem necessidade;
- refatorar código fora do escopo apenas por preferência do agente.

---

## 22. Checklist por task

Antes:

```text
[ ] Li o trecho relevante do Teste Prático
[ ] Li este arquivo
[ ] Inspecionei o código existente
[ ] Entendi dependências da task
[ ] Não vou antecipar task futura sem necessidade
```

Depois:

```text
[ ] dotnet build passa
[ ] dotnet test passa
[ ] comportamento novo tem testes
[ ] testes existentes continuam verdes
[ ] regra de negócio permaneceu fora de Api/Infrastructure
[ ] CancellationToken foi propagado quando aplicável
[ ] README foi atualizado se necessário
[ ] cada CA possui evidência objetiva
```

Formato de fechamento recomendado:

```text
CA-X.1: PASS — evidência: <teste/arquivo/comportamento>
CA-X.2: PASS — evidência: <teste/arquivo/comportamento>

Build: PASS
Tests: PASS
Pendências: nenhuma | <lista>
```

---

## 23. Definition of Done global

A entrega só está pronta quando:

1. compila e todos os testes passam;
2. todos os Handlers estão testados;
3. todas as regras obrigatórias estão protegidas no Domain;
4. API e Infrastructure não possuem regra de negócio;
5. rotas são exatamente as do Teste Prático;
6. autenticação protege Orders;
7. paginação usa `page`/`pageSize`;
8. persistência final usa EF Core + SQLite;
9. migrations são automáticas;
10. Docker funciona;
11. Sonar/observabilidade desejáveis estão configurados conforme plano;
12. README representa exatamente o estado do projeto;
13. todas as decisões podem ser explicadas de forma simples na entrevista.

---

## 24. Princípio final

> O objetivo não é demonstrar o maior número de padrões. É entregar todo o desafio com fronteiras claras, regras protegidas, testes confiáveis e decisões técnicas fáceis de defender.
