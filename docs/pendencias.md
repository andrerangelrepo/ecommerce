# Pendências — Aderência às INSTRUÇÕES

Checklist do que falta para aderência completa ao [`INSTRUCOES.md`](../INSTRUCOES.md) (o "Teste Prático", contrato soberano). Levantado por revisão da implementação atual; ver histórico de conversa para o detalhamento de cada gap.

Marcar `[x]` conforme cada item for implementado.

---

## 1. Docker (Stack Obrigatória)

- [x] `Dockerfile` ([Dockerfile](../Dockerfile))
  - [x] multi-stage build — testado com `docker build`, cache de `restore` confirmado
  - [x] SDK do .NET presente somente na etapa de build
  - [x] imagem final contém apenas o runtime — confirmado por inspeção do container (`dotnet --list-sdks` vazio, sem `/src`)
  - [x] nenhum segredo (ex.: `Jwt:Key`) hardcoded na imagem — a chave vem só via `Jwt__Key` no `docker-compose.yml`, nunca do Dockerfile/imagem
- [x] `docker-compose.yml` ([docker-compose.yml](../docker-compose.yml))
  - [x] serviço da API
  - [x] SQLite permanece embarcado (sem container de banco separado)
  - [x] volume para o arquivo SQLite — testado com container removido (`down` sem `-v`) e recriado, dados persistiram
  - [x] `Jwt:Key` (e demais segredos) injetados via variável de ambiente, não hardcoded no compose — `Jwt__Key: "development-only-key-change-in-production"`, testado com um token assinado com a chave do `appsettings.json` sendo rejeitado (`401`), confirmando que o override é o valor efetivo

## 2. README — instruções de execução via Docker (Stack Obrigatória)

- [x] Seção "Executar com Docker" no README ([README.md](../README.md))
  - [x] comando de build da imagem isolado (`docker build -t ecommerce-api .`), além do `docker compose up --build`
  - [x] comando de subida via `docker compose`, incluindo a diferença entre `down` e `down -v`
  - [x] variáveis de ambiente necessárias (`Jwt__Key`) e como fornecê-las

## 3. Testes unitários de todos os Handlers (Stack Obrigatória / "O que Não Fazer")

- [x] `CreateOrderCommandHandlerTests` ([tests/.../CreateOrder/CreateOrderCommandHandlerTests.cs](../tests/ECommerce.Application.Tests/Features/Orders/Commands/CreateOrder/CreateOrderCommandHandlerTests.cs))
  - [x] happy path: pedido é persistido via `IOrderRepository.AddAsync`
  - [x] mapeamento `Order` → `CreateOrderResult` correto (`Id`, `CustomerId`, `Status`, `CreatedAt`, `TotalAmount`)
  - [x] `CancellationToken` recebido é propagado para `AddAsync`
- [x] `GetOrdersQueryValidatorTests` — achado durante a TASK 10, fora do escopo original desta lista, mas mesmo tipo de gap (validator sem teste dedicado) ([tests/.../GetOrders/GetOrdersQueryValidatorTests.cs](../tests/ECommerce.Application.Tests/Features/Orders/Queries/GetOrders/GetOrdersQueryValidatorTests.cs))

## 4. Desejáveis — não eliminatórios

- [ ] Logging com Serilog
  - [ ] `UseSerilog()` configurado em `Program.cs` — os pacotes (`Serilog`, `Serilog.Sinks.Console`) foram **removidos** de `Directory.Packages.props`/`ECommerce.Infrastructure.csproj` na TASK 13 (SUBTASK 13.21, hardening) por estarem referenciados sem nenhum uso real; reinstalar (`dotnet add package Serilog`) é trivial se/quando este item for implementado
  - [ ] `LoggingBehavior` no pipeline do MediatR, registrando:
    - [ ] Command/Query recebido
    - [ ] Response
    - [ ] Tempo de execução
  - [ ] Garantir que nunca loga senha, signing key ou JWT completo
- [ ] SonarQube ou `dotnet-sonarscanner`
  - [ ] configurado no `docker-compose.yml` (depende do item 1)
  - [ ] meta: 0 Blocker, 0 Critical
- [ ] OpenTelemetry
  - [ ] instrumentação ASP.NET Core
  - [ ] instrumentação HTTP, quando aplicável
  - [ ] exportação para console

---

## Outros achados (fora do escopo direto das INSTRUÇÕES, mas relevantes)

- [x] `GET /api/orders?page=abc` (valor não numérico) retornava `500` em vez de `400`. Corrigido: `GlobalExceptionHandler` agora trata `BadHttpRequestException` (lançada pelo binding do Minimal API) e mapeia para `400`, com `title: "Invalid request"` e `detail` explicando o parâmetro que falhou. Teste de regressão em [`GetOrdersIntegrationTests.cs`](../tests/ECommerce.IntegrationTests/GetOrdersIntegrationTests.cs) — necessário como integração porque o erro ocorre no binding, antes do MediatR/`ValidationBehavior` rodarem, então nenhum teste de Handler/Validator conseguiria pegar essa regressão.
