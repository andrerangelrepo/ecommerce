# ECommerce — Sistema de Gestão de Pedidos

Backend API para um e-commerce simples, implementado em .NET 10 com Clean Architecture, CQRS + MediatR e Minimal API.

## Sumário

- [Arquitetura](#arquitetura)
- [Stack Técnico](#stack-técnico)
- [Como Rodar — Passo a Passo](#como-rodar--passo-a-passo)
- [Autenticação](#autenticação)
- [Endpoints](#endpoints)
- [Migrations](#migrations)
- [Testes](#testes)
- [Decisões Técnicas](#decisões-técnicas)
- [Status do Projeto e Trade-offs Conscientes](#status-do-projeto-e-trade-offs-conscientes)
- [Notas de Segurança](#notas-de-segurança)
- [Relatório de Hardening](docs/hardening-report.md)

## Arquitetura

O projeto segue **Clean Architecture** com as seguintes camadas:

```
src/
  Domain/              — Entidades, enums, invariantes (zero dependências)
  Application/         — Commands, Queries, Handlers, DTOs, Validators
  Infrastructure/      — Persistência, JWT, implementações concretas
  Api/                 — DI, autenticação, endpoints (Minimal API)

tests/
  ECommerce.Application.Tests/ — Testes unitários (Domain, Handlers, Validators) — rápidos e isolados
  ECommerce.IntegrationTests/  — Testes de integração via WebApplicationFactory (JWT + MediatR + EF Core + SQLite)
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
- **JWT Bearer** — autenticação stateless, usuário fixo em memória
- **OpenAPI nativo + Swagger UI** — contrato gerado pelo ASP.NET Core com interface interativa em Development
- **xUnit + Moq + FluentAssertions** — testes unitários e de integração (50 testes)
- **Docker + Docker Compose** — build multi-stage, persistência via volume nomeado
- **Central Package Management (CPM)** — versionamento centralizado

## Como Rodar — Passo a Passo

Duas formas de subir a aplicação: **local** (`dotnet run`) ou **Docker** (`docker compose`). Escolha uma.

### 1. Pré-requisitos

- **Local:** .NET 10 SDK
- **Docker:** Docker Desktop (ou engine + compose plugin) — não precisa do SDK instalado

### 2. Preparar o ambiente

```bash
git clone <url-do-repositório>
cd ecommerce
dotnet restore
```

### 3a. Rodar localmente

```bash
dotnet run --project src/ECommerce.API
```

A API sobe em `http://localhost:5000` (perfil `http` de `launchSettings.json`). O SQLite (`orders.db`) é criado no diretório do projeto (`src/ECommerce.API/orders.db`) e as migrations aplicam automaticamente no startup — nenhum passo manual.

### 3b. Rodar com Docker

```bash
docker compose up --build
```

A API sobe em `http://localhost:8080`. Mesmo comportamento de migrations automáticas; o SQLite fica num volume Docker nomeado (`order-data`), não no filesystem do host. Detalhes de persistência, variáveis de ambiente e o ciclo `down`/`down -v` estão na subseção [Docker em detalhe](#docker-em-detalhe) abaixo.

### 4. Abrir o Swagger

Com a aplicação em execução (local ou Docker), acesse:

```
http://localhost:5000/swagger   (local)
http://localhost:8080/swagger   (Docker)
```

O Swagger UI só fica disponível em `Development` (que é o ambiente padrão tanto local quanto no `docker-compose.yml`). É a forma mais rápida de explorar os endpoints sem precisar de Postman — cada rota já vem com o schema de request/response, e o botão **Authorize** aceita o JWT direto (veja o passo 5).

Se preferir Postman/Insomnia em vez do Swagger, importe o contrato OpenAPI em `http://localhost:5000/openapi/v1.json` (ou `:8080` no Docker) — a maioria dos clientes HTTP importa OpenAPI/Swagger nativamente.

### 5. Autenticar

Credencial fixa (não há cadastro de usuário neste desafio):

```json
{
  "email": "dev@martech.com",
  "password": "Senha@123"
}
```

**Via Swagger:** rode `POST /auth/login` com o corpo acima, copie o `accessToken` da resposta, clique em **Authorize** (canto superior direito) e cole `Bearer <token>`.

**Via curl:**

```bash
TOKEN=$(curl -s -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"dev@martech.com","password":"Senha@123"}' \
  | sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p')
```

Detalhes de como o JWT é validado (issuer, audience, expiração, assinatura) estão em [Autenticação](#autenticação).

### 6. Rodando os Cenários

Um fluxo completo mínimo, pra confirmar que tudo está funcionando:

```bash
# criar um pedido
curl -s -X POST http://localhost:5000/api/orders \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"customerId":"11111111-1111-1111-1111-111111111111","items":[{"productName":"Teclado","quantity":1,"unitPrice":350}]}'

# listar pedidos
curl -s "http://localhost:5000/api/orders?page=1&pageSize=10" -H "Authorization: Bearer $TOKEN"

# buscar por id (troque {id} pelo retornado no POST)
curl -s http://localhost:5000/api/orders/{id} -H "Authorization: Bearer $TOKEN"

# cancelar
curl -s -X PATCH http://localhost:5000/api/orders/{id}/cancel -H "Authorization: Bearer $TOKEN"
```

Para o catálogo completo — todos os endpoints, todos os status HTTP possíveis (`200`/`201`/`400`/`401`/`404`/`409`), com `curl` e corpo de resposta esperado para cada cenário — veja **[docs/manual-testing-guide.md](docs/manual-testing-guide.md)**.

### 7. Ver os dados no banco (opcional)

O banco é SQLite; qualquer cliente SQLite serve, incluindo o [DB Browser for SQLite](https://sqlitebrowser.org/) (gratuito, GUI).

**Local:** abra `src/ECommerce.API/orders.db` diretamente.

**Docker:** o arquivo vive dentro do volume nomeado `order-data`, não no host. Duas formas de acessar:

```bash
# copiar o arquivo do container para o host
docker cp ecommerce-api-1:/app/data/orders.db ./orders.db

# ou inspecionar direto dentro do container
docker compose exec api sh
# dentro do container, o arquivo está em /app/data/orders.db
```

Schema (tabelas `Orders` e `OrderItems`, com `Status` gravado como inteiro do enum):

```sql
SELECT * FROM Orders ORDER BY CreatedAt DESC;

SELECT o.Id, o.Status, i.ProductName, i.Quantity, i.UnitPrice
FROM Orders o JOIN OrderItems i ON i.OrderId = o.Id;
```

### 8. Rodar os testes automatizados

```bash
dotnet test
```

Roda os dois projetos de teste juntos (50 testes). Para rodar isoladamente:

```bash
dotnet test tests/ECommerce.Application.Tests  # 42 testes — rápidos, isolados (mocks, sem I/O real)
dotnet test tests/ECommerce.IntegrationTests   # 8 testes — WebApplicationFactory, SQLite real
```

Detalhes de cada modalidade de teste em [Testes](#testes).

### Docker em detalhe

```bash
docker build -t ecommerce-api .   # builda só a imagem, sem subir nada (útil pra inspecionar/CI)
docker compose up --build         # builda (se preciso) e sobe, aplicando migrations automaticamente
docker compose down               # para o container, MANTÉM o volume (dados persistem)
docker compose down -v            # para e APAGA o volume (reset completo — útil pra testar a primeira inicialização de novo)
```

`docker-compose.yml` sobrescreve `Jwt:Key` via variável de ambiente (`Jwt__Key`) com um valor de desenvolvimento — nunca use esse valor em produção. Fora do Docker, `dotnet run` usa o placeholder de `appsettings.json`. O container roda como usuário não-root (`app`, uid 1654) e expõe um health check em `GET /health` (`docker compose ps` mostra `(healthy)`).

## Autenticação

- **Credencial fixa** (usuário em memória, sem cadastro): `dev@martech.com` / `Senha@123` — comparação de email é case-insensitive, senha é case-sensitive.
- `POST /auth/login` é o único endpoint anônimo; todos os `/api/orders*` exigem `Authorization: Bearer <token>`.
- O JWT carrega `sub`/`email` (identidade), `jti` (id único do token) e `exp` (expiração — configurável via `Jwt:ExpirationMinutes`, padrão 60 min).
- Validação no servidor cobre assinatura, issuer, audience e expiração, sem tolerância de relógio (`ClockSkew = TimeSpan.Zero`) — um token expirado há 1 segundo já é rejeitado. Detalhes e o porquê de cada escolha em [Decisões Técnicas](#decisões-técnicas).
- A chave de assinatura (`Jwt:Key`) nunca é hardcoded no Dockerfile/imagem — vem de configuração (`appsettings.json` localmente, variável de ambiente no Docker). Ver [Notas de Segurança](#notas-de-segurança).

## Endpoints

| Método | Rota | Autenticação | Descrição |
|---|---|:---:|---|
| `POST` | `/auth/login` | ❌ | Autentica e retorna um JWT |
| `POST` | `/api/orders` | ✅ | Cria um pedido |
| `GET` | `/api/orders?page=&pageSize=` | ✅ | Lista pedidos paginados |
| `GET` | `/api/orders/{id}` | ✅ | Busca um pedido por id |
| `PATCH` | `/api/orders/{id}/cancel` | ✅ | Cancela um pedido (só `Pending`) |
| `GET` | `/health` | ❌ | Health check operacional (sem lógica de negócio) |

Cenários completos de cada endpoint (todo status HTTP possível) em [docs/manual-testing-guide.md](docs/manual-testing-guide.md).

## Migrations

- Ferramenta: **EF Core Migrations**, arquivos em `src/ECommerce.Infrastructure/Persistence/Migrations/`.
- Aplicação: **automática no startup**, via `app.Services.ApplyMigrationsAsync()` em `Program.cs`, que chama `Database.MigrateAsync()` — não existe passo manual (`dotnet ef database update`) nem em execução local nem no Docker.
- Migration atual: `InitialCreate` (tabelas `Orders` e `OrderItems`, FK com `ON DELETE CASCADE`, `TotalAmount` explicitamente ignorado do mapeamento porque é calculado no domínio, não persistido).
- Para criar uma nova migration (se o schema mudar):

```bash
dotnet ef migrations add NomeDaMigration \
  --project src/ECommerce.Infrastructure \
  --startup-project src/ECommerce.API
```

Requer a ferramenta `dotnet-ef` instalada (`dotnet tool install --global dotnet-ef`) — só necessária em desenvolvimento; a imagem Docker de runtime não tem o SDK nem essa ferramenta (por design, ver [Decisões Técnicas](#decisões-técnicas)).

## Testes

**61 testes no total**, divididos em dois projetos com propósitos diferentes:

| Projeto | Testes | O que cobre | Velocidade |
|---|---:|---|---|
| `ECommerce.Application.Tests` | 48 | Domínio (invariantes de `Order`/`OrderItem`), os 4 Handlers (com `Mock<IOrderRepository>`), Validators (`FluentValidation`), `ValidationBehavior`, `LoggingBehavior` | Rápido — sem I/O real, sem HTTP |
| `ECommerce.IntegrationTests` | 13 | Fluxos HTTP completos via `WebApplicationFactory` (JWT + MediatR + EF Core + SQLite real), incluindo login, os 404 dos dois endpoints e o mapeamento EF Core (`Order`↔`OrderItem`) | Mais lento — sobe a aplicação real |

Nenhum teste depende de banco local pré-existente ou de outro processo já rodando — `dotnet test` sozinho é suficiente, cada teste de integração usa um SQLite isolado descartável.

## Decisões Técnicas

### Por que Minimal API?

Minimal APIs foram escolhidas devido ao pequeno número de endpoints — cinco no escopo atual — e à simplicidade do serviço. Controllers seriam igualmente válidos, mas adicionariam estrutura sem benefício relevante neste cenário. As responsabilidades de negócio permanecem isoladas na camada Application por meio de CQRS/MediatR; os endpoints atuam somente como adaptadores entre HTTP e os casos de uso.

### Por que separar endpoints e contratos do `Program.cs`?

O `Program.cs` permanece como composition root da aplicação. Endpoints são organizados por recurso e seus contratos HTTP ficam separados dos Commands e DTOs da Application. Rotas de pedidos compartilham o prefixo `/api/orders` por meio de `MapGroup`, centralizando apenas configuração comum ao recurso. Essa organização evita o crescimento do arquivo de inicialização sem introduzir um framework próprio ou uma abstração genérica de endpoints e mantém explícita a fronteira entre API e Application.

### Por que manter o `Program.cs` mínimo?

O startup declara apenas a composição necessária para o escopo já implementado: Application, Infrastructure, tratamento global de erros, migrations, documentação da API e endpoints. Isso mantém o composition root legível e evita dependências e configurações sem uso concreto.

### Por que utilizar o OpenAPI nativo?

O contrato continua sendo gerado pelo suporte oficial do ASP.NET Core por meio de `AddOpenApi` e `MapOpenApi`. Em Development, o documento fica disponível em `/openapi/v1.json` e o Swagger UI em `/swagger`. Apenas o pacote de interface do Swashbuckle é utilizado, apontando para o documento nativo; assim evitamos manter dois geradores OpenAPI concorrentes. Fora de Development, documento e interface não são publicados.

### Por que separar Request HTTP e Command?

Os requests da API representam contratos externos sujeitos à evolução do protocolo HTTP, enquanto os Commands representam intenções e casos de uso da Application. Mesmo quando possuem os mesmos campos, tipos distintos evitam expor MediatR como contrato público e permitem que API e Application evoluam sem acoplamento desnecessário.

### Por que utilizar um Response HTTP dedicado?

A API não expõe entidades do Domain nem retorna diretamente resultados internos da Application. Um response próprio mantém o contrato HTTP estável e permite representar `OrderStatus` como texto legível sem alterar a persistência, que continua usando o mapeamento inteiro padrão do EF Core.

### Por que utilizar Problem Details?

Erros HTTP utilizam o suporte nativo do ASP.NET Core a Problem Details, seguindo o formato padronizado `application/problem+json`. Isso oferece respostas interoperáveis sem criar envelopes genéricos como `ApiResponse<T>` ou `ErrorResponse<T>`.

### Por que centralizar o tratamento de exceções?

A API utiliza o mecanismo nativo `IExceptionHandler` do ASP.NET Core. Exceções atravessam um único ponto de tradução para Problem Details, são registradas com o contexto da requisição e recebem um `traceId` para correlação. Falhas do FluentValidation e falhas de binding do Minimal API (`BadHttpRequestException`) produzem HTTP 400; regra de negócio violada (`OrderCannotBeCancelledException`) produz 409; falhas inesperadas não expõem detalhes internos e produzem HTTP 500. Os endpoints permanecem focados na adaptação HTTP, sem blocos `try/catch` repetidos.

### Por que CQRS?

Separação clara entre operações de leitura (Queries) e escrita (Commands), facilitando testes, escalabilidade e manutenção.

### Por que FluentValidation via Pipeline Behavior?

Validação centralizada e reutilizável. Todos os Commands/Queries passam pelo mesmo pipeline, garantindo consistência.

### Por que Central Package Management?

Simplifica manutenção de versões em projetos multi-camadas. Uma única fonte de verdade em `Directory.Packages.props`.

### Por que não criar `PagedResult<T>`?

`GetOrdersResult` é um record concreto (`Items`, `Page`, `PageSize`, `TotalCount`, `TotalPages`), não uma abstração genérica de paginação. Hoje existe apenas um caso de listagem paginada no projeto; generalizar para `PagedResult<T>` antes de existir um segundo ou terceiro consumidor seria design especulativo. Se outra listagem paginada surgir, essa decisão é reavaliada então, com exemplos concretos guiando a forma da abstração.

### Por que não criar `IUnitOfWork`?

`IOrderRepository` continua a única abstração de persistência — `AddAsync`/`UpdateAsync` chamam `SaveChangesAsync` diretamente, sem um `IUnitOfWork`/`ITransactionManager` por cima. Cada caso de uso implementado (criar, cancelar) altera um único aggregate dentro do mesmo `DbContext` por requisição; um Unit of Work explícito só se justificaria coordenando múltiplos repositórios numa mesma transação, cenário que ainda não existe aqui.

### Por que um teste de integração HTTP real para confirmar a persistência do cancelamento?

Os testes de Handler (Moq) provam que `UpdateAsync` foi chamado, mas não provam que a alteração sobrevive além de uma única requisição — é só a mesma instância de `DbContext`/mock em memória. Para confirmar de fato que `POST → GET → PATCH cancel → GET` reflete `Cancelled` numa leitura *separada*, foi adicionado `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`), hospedando a API real em processo contra um arquivo SQLite isolado por execução de teste. `Program.cs` ganhou `public partial class Program;` no final — necessário porque top-level statements geram uma classe `Program` `internal` por padrão, e o `WebApplicationFactory<Program>` do projeto de testes precisa enxergá-la.

### Por que não tratar concorrência no cancelamento?

`OrderRepository.UpdateAsync` não usa `rowversion`/concurrency token, nem trata `DbUpdateConcurrencyException`, nem há lock distribuído ou retry. O teste não exige concorrência (múltiplos clientes cancelando o mesmo pedido simultaneamente), e adicionar esse controle agora seria complexidade sem requisito por trás. Se concorrência real for exigida depois, a mudança fica isolada em `OrderConfiguration` (coluna de token) e no `catch` de `UpdateAsync`, sem afetar Handler ou Domain.

### Por que Queries retornam `null` em vez de lançar exceção para registro inexistente?

`GetOrderByIdQueryHandler` retorna `GetOrderByIdResult?` e devolve `null` quando o pedido não existe, sem `OrderNotFoundException` nem qualquer outro tipo de exceção. Não encontrar um registro é um resultado normal de uma consulta, não uma falha excepcional. A tradução `null → 404 Not Found` é responsabilidade do endpoint HTTP, mantendo a Application indiferente a códigos de status, do mesmo jeito que já é indiferente a JWT.

### Por que o 404 de pedido inexistente usa `ProblemDetails` em vez de `Results.NotFound()`?

`Results.NotFound()` produz um `404` com corpo vazio (`Content-Length: 0`), sem `Content-Type`, diferente do formato `application/problem+json` retornado por todos os outros erros (400, 409, 500) via `GlobalExceptionHandler`. Como esse `404` não passa por exceção — é um `null` de Query/Command traduzido diretamente no endpoint —, ele nunca alcança o `GlobalExceptionHandler`. `OrderNotFoundProblem.Result(httpContext)` (`src/ECommerce.API/ExceptionHandling/OrderNotFoundProblem.cs`) monta o mesmo formato `ProblemDetails` (`type`, `title`, `status`, `traceId`) usado nos demais erros, mantendo o contrato de erro da API consistente independentemente de a resposta ter passado por uma exceção ou não. É um helper específico para este caso conhecido — não um envelope genérico de resposta.

### Por que `OrderCannotBeCancelledException` vira 409 e não 400?

`400 Bad Request` indicaria problema no formato ou nos dados da requisição em si. Não é o caso aqui: o request é válido, o pedido existe, mas o estado atual dele (`Cancelled`/`Confirmed`) é incompatível com a operação pedida — request válido + recurso existente + estado incompatível é exatamente a definição de conflito. `409 Conflict` comunica isso de forma mais expressiva do que um `400` genérico.

### Por que validar Issuer, Audience e assinatura no JWT?

O `TokenValidationParameters` habilita explicitamente `ValidateIssuer`, `ValidateAudience`, `ValidateIssuerSigningKey` e `ValidateLifetime`. Confiar apenas na assinatura não seria suficiente: sem validar issuer/audience, um token assinado pela própria aplicação mas emitido com outro propósito ainda seria aceito.

### Por que ClockSkew = TimeSpan.Zero?

Por padrão, o `JwtBearerHandler` aplica uma tolerância de 5 minutos na validação de expiração, aceitando tokens já expirados dentro dessa janela. Para um teste técnico, essa tolerância reduz a previsibilidade dos testes de expiração. Zerar o `ClockSkew` faz o token expirar exatamente em `ExpirationMinutes`, sem margem adicional.

### Por que a ordem UseAuthentication → UseAuthorization?

`UseAuthentication` identifica quem é o chamador, populando `HttpContext.User` a partir do token JWT; `UseAuthorization` decide se esse chamador pode acessar o recurso. A segunda depende do resultado da primeira, então a ordem inversa faria toda decisão de autorização cair sempre no caminho de "não autenticado".

### Por que Bearer no OpenAPI via Document/Operation Transformer em vez de trocar de biblioteca?

O contrato OpenAPI continua gerado pelo suporte nativo do ASP.NET Core (`AddOpenApi`), sem introduzir Swashbuckle.AspNetCore.SwaggerGen só para ganhar suporte a esquemas de segurança. `BearerSecuritySchemeTransformer` registra o esquema `Bearer`; `BearerSecurityRequirementOperationTransformer` adiciona o requisito de segurança só às operações cujo endpoint tem `IAuthorizeData` nos metadados — ou seja, só `/api/orders`, não `/auth/login`. Isso mantém a documentação sincronizada automaticamente com `.RequireAuthorization()`.

### Por que validar `JwtOptions` no startup?

`Jwt:Key`, `Jwt:Issuer` e `Jwt:Audience` binding para `string` sempre resultam em um valor não nulo (`string.Empty` quando ausentes), então checar só se a seção existe não pega o caso de configuração em branco — a aplicação subiria com uma chave de assinatura vazia. `JwtOptions` usa `DataAnnotations` (`[Required(AllowEmptyStrings = false)]`, `[Range(1, int.MaxValue)]` em `ExpirationMinutes`) e `Program.cs` valida o objeto com `Validator.TryValidateObject` antes de qualquer registro de serviço que dependa desses valores.

### Por que login não passa pelo MediatR/Application?

`LoginEndpoint` valida a credencial fixa e emite o token chamando `ITokenService` diretamente, sem `LoginCommand`. Autenticação é uma preocupação do boundary HTTP, não uma regra de negócio do domínio de pedidos: não há entidade, invariante ou persistência envolvida. Como consequência, a camada Application nunca importa nada relacionado a JWT.

### Por que Serilog só no projeto API?

`Serilog.AspNetCore`/`Serilog.Sinks.Console`/`Serilog.Settings.Configuration` são referenciados só em `ECommerce.API`, configurado em `Program.cs` via `builder.Host.UseSerilog(...)`, substituindo o provider de log padrão do ASP.NET Core para toda a aplicação (Domain/Application/Infrastructure continuam só usando `ILogger<T>` do BCL, sem saber que Serilog existe por baixo). `Program.cs` só chama `ReadFrom.Configuration(context.Configuration)` e `ReadFrom.Services(services)` — nenhum sink/enricher/nível é hardcoded em código; tudo isso (`MinimumLevel`, `WriteTo`, `Enrich`) vem da seção `Serilog` do `appsettings.json`. Isso evita um bug real que apareceu durante a implementação: declarar `WriteTo.Console()` tanto em código quanto no JSON registra **dois** sinks de console, duplicando cada linha de log.

### Por que `Microsoft`/`Microsoft.AspNetCore`/`Microsoft.EntityFrameworkCore` como `Warning` no `appsettings.json` base?

Esses namespaces de framework são extremamente verbosos em `Information`/`Debug` (cada `DbCommand` do EF Core, cada início/fim de request do Kestrel). Suprimi-los para `Warning` no `appsettings.json` base mantém o log de produção legível — só aparece o que é nosso ou é um problema real. `appsettings.Development.json` sobrescreve `Default` para `Debug` e `Microsoft.AspNetCore` de volta para `Information`, então localmente ainda dá pra ver o ciclo de vida de cada request; `Microsoft.EntityFrameworkCore` continua suprimido mesmo em desenvolvimento (não sobrescrito), já que o SQL gerado é verboso demais para uso corriqueiro — quando esse nível de detalhe for necessário, dá pra sobrescrever via variável de ambiente (`Serilog__MinimumLevel__Override__Microsoft.EntityFrameworkCore=Debug`) sem tocar em código. `Warning` é um piso, não uma mudez: qualquer evento em `Warning`/`Error`/`Fatal` desses namespaces continua aparecendo — só `Information`/`Debug` é suprimido.

### Por que `app.UseSerilogRequestLogging()`?

Sem esse middleware, cada request gera várias linhas do ASP.NET Core (`Request starting`, `Executing endpoint`, `Executed endpoint`, `Request finished`) — genérico e caro de ler. `UseSerilogRequestLogging()` substitui isso por uma única linha estruturada por request (`HTTP POST /api/orders responded 201 in 54 ms`, com `RequestMethod`/`RequestPath`/`StatusCode`/`Elapsed` como propriedades pesquisáveis, não só texto). Em produção (`Microsoft.AspNetCore` suprimido para `Warning`), essa é a única linha por request que sobra — confirmado rodando a API em `Production` e vendo só essa linha no console, sem nenhum ruído do framework por baixo.

### Por que `UseSerilogRequestLogging()` vem antes de `UseExceptionHandler()`?

Middleware do ASP.NET Core forma camadas aninhadas: quem é registrado primeiro "embrulha" tudo que vem depois. Colocar `UseSerilogRequestLogging()` **antes** de `UseExceptionHandler()` faz o logging ser a camada mais externa, então ele só escreve a linha de conclusão depois que o exception handler (mais interno) já decidiu o status code final. Registrar na ordem inversa (como uma versão inicial desta implementação fez) parece inofensivo, mas quebra em qualquer request que dispare uma exceção tratada: reproduzi isso rodando a API e forçando um `409` real (cancelar o mesmo pedido duas vezes) — com `UseExceptionHandler()` por fora, a resposta HTTP era `409` de verdade, mas a linha de log dizia `responded 500`, porque o middleware de logging via a exceção (antes dela ser traduzida) e não o resultado final. Com `UseSerilogRequestLogging()` por fora, a mesma requisição loga `responded 409` corretamente — confirmado também para o caso `400` de payload/paginação inválida.

### Por que `LoggingBehavior`?

`LoggingBehavior<TRequest, TResponse>` (`Application/Behaviors`) registra início, duração e resultado de todo Command/Query que passa pelo MediatR — `Handling CreateOrderCommand` → `Handled CreateOrderCommand in 42 ms`. Usa `ILogger<T>` do `Microsoft.Extensions.Logging.Abstractions` (só a abstração, sem pacote do Serilog na Application), então a camada continua sem saber que Serilog existe — quem faz a ponte é a API, no bootstrap. Registrado como Open Behavior via `config.AddOpenBehavior(...)` dentro de `AddMediatR(...)` (não mais um `services.AddTransient(typeof(IPipelineBehavior<,>), ...)` solto), na ordem `LoggingBehavior` → `ValidationBehavior`, para que até requests inválidas sejam medidas e logadas como tentativa de processamento.

Nunca loga o request/response inteiro (`{@Request}`/`{@Response}`) — só o nome do tipo (`RequestName`) e a duração, como propriedades estruturadas (não interpolação de string). Isso é, por construção, suficiente para nunca vazar senha/token: `Login` nem passa pelo MediatR (vai direto pra `ITokenService`, sem `LoginCommand`), então `LoggingBehavior` nunca chega a ver `LoginRequest`; para os demais Commands/Queries, como nunca serializamos o objeto inteiro, uma eventual property sensível num futuro DTO não vazaria por acidente.

Falhas são classificadas por tipo de exceção, não por status HTTP — o Behavior não conhece `400`/`409`/`500`/`ProblemDetails`, só `ValidationException`/`OrderCannotBeCancelledException` (`Warning`, sem stack trace — são resultados esperados, não erro operacional) vs. qualquer outra exceção (`Error`, com stack trace completo). A mesma exceção também é logada pelo `GlobalExceptionHandler` (que trata o lado HTTP) — a duplicação é intencional (dois contextos diferentes: Application vs. HTTP), mas os dois usam a mesma classificação de nível, então uma exceção de validação nunca aparece como `Error` num lugar e `Warning` noutro. Em todos os casos a exceção é relançada (`throw;`) — o Behavior nunca engole nem substitui, o tratamento HTTP continua 100% no `GlobalExceptionHandler`.

## Status do Projeto e Trade-offs Conscientes

Todos os itens da **Stack Obrigatória** do desafio estão implementados: domínio, CQRS/MediatR, EF Core + SQLite com migrations automáticas, JWT, FluentValidation via pipeline, testes unitários dos 4 Handlers, Docker + Docker Compose, e este README.

Os itens listados como **"Desejável — Não Eliminatório"** no enunciado receberam este tratamento:

| Item | Status |
|---|---|
| Testes de integração com `WebApplicationFactory` | ✅ Implementado — 13 testes, cobrindo `POST`/`GET`/`PATCH` e login |
| Logging com Serilog + `LoggingBehavior` | ❌ Não implementado — decisão consciente de priorizar o restante do escopo obrigatório. Os pacotes não usados foram removidos do projeto (não ficam como peso morto); reinstalar é trivial se este item for retomado |
| SonarQube / `dotnet-sonarscanner` | ❌ Não implementado |
| OpenTelemetry | ❌ Não implementado |

Essas três ausências são rastreadas com detalhe (incluindo o porquê e o que falta exatamente) em [`docs/pendencias.md`](docs/pendencias.md) — mantido como registro honesto do que foi deliberadamente deixado de fora, não como lista de bugs.

Uma auditoria de hardening arquitetural dedicada (build, testes, camadas, persistência, segurança, Docker) foi conduzida ao final da implementação — resultado consolidado em [`docs/hardening-report.md`](docs/hardening-report.md).

## Notas de Segurança

- A chave JWT em `appsettings.json` é só um placeholder de desenvolvimento, não um segredo real.
- Em produção, `Jwt:Key` deve vir de variável de ambiente (`Jwt__Key`) ou de um secret manager — nunca versionada. O `docker-compose.yml` já demonstra esse padrão com um valor de desenvolvimento explicitamente marcado como tal.
- O container Docker roda como usuário não-root e não expõe o arquivo SQLite como estático — só o EF Core acessa o banco.
- SQLite é embarcado (sem container de banco separado); persiste via volume Docker nomeado, não bind mount do código-fonte.
