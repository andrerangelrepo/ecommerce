# Pendências — Aderência às INSTRUÇÕES

Checklist do que falta para aderência completa ao [`INSTRUCOES.md`](../INSTRUCOES.md) (o "Teste Prático", contrato soberano). Levantado por revisão da implementação atual; ver histórico de conversa para o detalhamento de cada gap.

Marcar `[x]` conforme cada item for implementado.

---

## 1. Docker (Stack Obrigatória)

- [ ] `Dockerfile`
  - [ ] multi-stage build
  - [ ] SDK do .NET presente somente na etapa de build
  - [ ] imagem final contém apenas o runtime
  - [ ] nenhum segredo (ex.: `Jwt:Key`) hardcoded na imagem
- [ ] `docker-compose.yml`
  - [ ] serviço da API
  - [ ] SQLite permanece embarcado (sem container de banco separado)
  - [ ] volume para o arquivo SQLite, se necessário para persistir entre restarts do container
  - [ ] `Jwt:Key` (e demais segredos) injetados via variável de ambiente, não hardcoded no compose

## 2. README — instruções de execução via Docker (Stack Obrigatória)

- [ ] Seção "Executar com Docker" no README
  - [ ] comando de build da imagem
  - [ ] comando de subida via `docker-compose`
  - [ ] variáveis de ambiente necessárias (ex.: `Jwt__Key`) e como fornecê-las

## 3. Testes unitários de todos os Handlers (Stack Obrigatória / "O que Não Fazer")

- [x] `CreateOrderCommandHandlerTests` ([tests/.../CreateOrder/CreateOrderCommandHandlerTests.cs](../tests/ECommerce.Application.Tests/Features/Orders/Commands/CreateOrder/CreateOrderCommandHandlerTests.cs))
  - [x] happy path: pedido é persistido via `IOrderRepository.AddAsync`
  - [x] mapeamento `Order` → `CreateOrderResult` correto (`Id`, `CustomerId`, `Status`, `CreatedAt`, `TotalAmount`)
  - [x] `CancellationToken` recebido é propagado para `AddAsync`
- [x] `GetOrdersQueryValidatorTests` — achado durante a TASK 10, fora do escopo original desta lista, mas mesmo tipo de gap (validator sem teste dedicado) ([tests/.../GetOrders/GetOrdersQueryValidatorTests.cs](../tests/ECommerce.Application.Tests/Features/Orders/Queries/GetOrders/GetOrdersQueryValidatorTests.cs))

## 4. Desejáveis — não eliminatórios

- [ ] Logging com Serilog
  - [ ] `UseSerilog()` configurado em `Program.cs` (pacotes já referenciados no `.csproj`, mas não usados)
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

- [ ] `GET /api/orders?page=abc` (valor não numérico) retorna `500` em vez de `400`. Causa: `BadHttpRequestException` do binding do Minimal API não é tratada pelo `GlobalExceptionHandler`. Detalhado em [`docs/test-scenarios.md`](test-scenarios.md), seção 8 (gap LIST-06/LIST-07).
