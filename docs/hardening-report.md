# TASK 13 — Relatório de Hardening Final

Consolidação da auditoria de hardening arquitetural (SUBTASKs 13.1–13.75): cada item abaixo foi verificado empiricamente — build real, teste real, ou requisição HTTP real contra a aplicação em execução — não apenas por leitura de código. O detalhamento de cada verificação está no histórico da conversa; este documento reúne o resultado final.

```
TASK 13 — Hardening Final

Build:
✅ Debug     — dotnet build            → 0 Aviso(s), 0 Erro(s)
✅ Release   — dotnet build -c Release → 0 Aviso(s), 0 Erro(s)
✅ Publish   — dotnet publish src/ECommerce.API/ECommerce.API.csproj -c Release
               → executado fora do Docker, respondeu 200/201/200/200 em health/login/create/list

Tests:
✅ Unit         — ECommerce.Application.Tests: 44 aprovados
✅ Integration  — ECommerce.IntegrationTests: 13 aprovados
✅ 0 failures   — confirmado em duas execuções consecutivas de `dotnet test` (Debug e Release)

Architecture:
✅ Domain independente        — ECommerce.Domain.csproj: 0 PackageReference, 0 ProjectReference
✅ Application sem Infrastructure — ECommerce.Application.csproj referencia só Domain + MediatR + FluentValidation
✅ API sem regra de negócio   — Program.cs é só composition root (51 linhas); endpoints só adaptam HTTP
✅ Infrastructure sem regra de negócio — OrderRepository só faz CRUD; Cancel()/AddItem() vivem no Domain
✅ CQRS preservado            — Commands e Queries em pastas e sufixos distintos, cada um com Handler próprio

Persistence:
✅ SQLite                — embarcado, sem container de banco separado
✅ migrations             — aplicadas automaticamente no startup (confirmado em ambiente limpo, Docker e publish)
✅ paginação no banco     — SQL real capturado: "LIMIT @__p_1 OFFSET @__p_0" antes do ToListAsync

Security:
✅ JWT                    — assinatura, issuer, audience e expiração validados, ClockSkew = zero
✅ endpoints protegidos   — /api/orders* exige Bearer; sem token → 401 (confirmado por request real)
✅ zero secret real       — só o usuário fixo exigido pelo enunciado; chave JWT é placeholder documentado

Docker:
✅ build      — docker compose build --no-cache concluído com sucesso
✅ startup    — container atinge (healthy) em ~24s
✅ persistence — volume nomeado sobrevive a "down" (sem -v); dados somem só com "down -v"

Warnings:
✅ 0 — build Debug e Release, ambos 0 Aviso(s)

Pendências:
Nenhuma pendência obrigatória. Restam só os 3 itens "Desejáveis — não eliminatórios"
já documentados em docs/pendencias.md (Serilog+LoggingBehavior, SonarQube, OpenTelemetry) —
deliberadamente fora do escopo, não gaps.
```

## Evidência por seção

### Build

| Comando | Resultado |
|---|---|
| `dotnet build` | Compilação com êxito — 0 Aviso(s), 0 Erro(s) |
| `dotnet build -c Release` | Compilação com êxito — 0 Aviso(s), 0 Erro(s) |
| `dotnet publish src/ECommerce.API/ECommerce.API.csproj -c Release` | Publish gerado em `bin/Release/net10.0/publish/`; executado diretamente (`ASPNETCORE_ENVIRONMENT=Production`, fora do Docker) — migrations rodaram do zero, `GET /health` → 200, login → token, `POST /api/orders` → 201, `GET /api/orders` → 200, `GET /swagger` → 404 (confirma que a UI de docs não vaza em Production) |

### Tests

Duas execuções consecutivas de `dotnet test`, resultado idêntico em ambas:

```
Aprovado! – Com falha: 0, Aprovado: 44, Ignorado: 0 — ECommerce.Application.Tests
Aprovado! – Com falha: 0, Aprovado: 13, Ignorado: 0 — ECommerce.IntegrationTests
```

Repetido também em `-c Release`, mesmo resultado (57/57).

### Architecture

- `ECommerce.Domain.csproj` não tem nenhum `<PackageReference>` nem `<ProjectReference>` — zero dependência externa.
- `ECommerce.Application.csproj` referencia só `ECommerce.Domain` + MediatR/FluentValidation — nenhuma referência a Infrastructure ou API.
- `Program.cs` tem 51 linhas: registro de serviços → build → middlewares → migrations → map endpoints → run. Nenhuma regra de pedido, SQL, criação de entidade ou geração manual de JWT.
- `OrderRepository` só chama `SaveChangesAsync`/consultas EF Core; toda regra de negócio (`Cancel()`, cálculo de `TotalAmount`/`TotalPrice`, invariantes) vive nas entidades do Domain.
- Todo `IRequest<>` termina em `Command`/`Query`, todo `IRequestHandler<>` no `...Handler` correspondente — sem mistura de convenção.

### Persistence

- SQLite embarcado, um único arquivo, sem container de banco separado no `docker-compose.yml`.
- Migrations aplicadas via `ApplyMigrationsAsync()` no startup — testado em três contextos distintos nesta rodada de hardening: Docker com volume novo, publish fora do Docker, e `dotnet run` local.
- Paginação confirmada no banco via SQL real capturado do EF Core: `Skip`/`Take` traduzem para `LIMIT`/`OFFSET` numa subquery, antes de qualquer `JOIN` com `OrderItems` — não há materialização de todos os pedidos em memória.

### Security

- JWT valida `ValidateIssuerSigningKey`, `ValidateIssuer`, `ValidateAudience`, `RequireExpirationTime`, `ValidateLifetime`, com `ClockSkew = TimeSpan.Zero`.
- `/api/orders*` exige `Authorization: Bearer <token>` — request real sem token retorna `401`.
- Busca por `password`/`secret`/`key`/`token`/`connection string` no repositório inteiro: o único valor fixo é o usuário exigido pelo próprio enunciado (`dev@martech.com`/`Senha@123`); a chave JWT é um placeholder de desenvolvimento explicitamente rotulado como tal, nunca hardcoded no `Dockerfile`.

### Docker

- `docker compose down -v && docker compose build --no-cache && docker compose up -d` executado do zero, simulando um avaliador clonando o repositório — container atingiu `(healthy)`.
- Fluxo funcional completo (login → create → list → get → cancel → get) executado contra o container recém-criado, todos os status HTTP conforme esperado.
- Persistência de volume já validada na TASK 11 e revalidada nesta rodada: dados sobrevivem a `down` sem `-v`.

### Warnings

`dotnet build` e `dotnet build -c Release`: **0 Aviso(s)** em ambos. Os avisos `NU1903` (vulnerabilidade transitiva do SQLite) foram eliminados fixando `SQLitePCLRaw.lib.e_sqlite3` na SUBTASK 13.20.

### Pendências

A única pendência genuína encontrada durante o hardening — o README não documentava `docker build` como comando isolado, só via `docker compose up --build` — foi fechada nesta mesma rodada (ver `docs/pendencias.md`, seção 2).

Os únicos itens não implementados no projeto inteiro são os 3 "Desejáveis — não eliminatórios" do enunciado (Serilog + `LoggingBehavior`, SonarQube, OpenTelemetry), documentados em detalhe em [`docs/pendencias.md`](pendencias.md) como decisão consciente de escopo, não como gap descoberto.
