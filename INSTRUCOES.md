# Teste Prático — Desenvolvedor .NET Senior

## Contexto

Você está construindo o backend de um sistema de gestão de pedidos para um e-commerce simples.

O foco é na **qualidade arquitetural**, não na quantidade de features.

> Código limpo, bem estruturado e testável vale mais do que muitos endpoints mal implementados.

---

## Domínio

Trabalhe com as seguintes entidades:

### `Order`

- `Id` (`Guid`)
- `CustomerId` (`Guid`)
- `Status` (`enum`)
  - `Pending`
  - `Confirmed`
  - `Cancelled`
- `CreatedAt` (`DateTime`)
- `Items` (lista de `OrderItem`)

### `OrderItem`

- `Id` (`Guid`)
- `OrderId` (`Guid`)
- `ProductName` (`string`)
- `Quantity` (`int`)
- `UnitPrice` (`decimal`)

---

## Stack Obrigatória

- **.NET 10**
  - Minimal API ou Controllers.
  - Justifique a escolha no `README`.

- **Clean Architecture**
  - `Domain`
  - `Application`
  - `Infrastructure`
  - `API`

- **CQRS com MediatR**
  - Commands e Queries separados.

- **Entity Framework Core**
  - SQLite.
  - Migrations aplicadas automaticamente na inicialização.

- **Autenticação JWT**
  - Endpoint de login.
  - Usuário fixo em memória é suficiente.

- **FluentValidation**
  - Pipeline Behavior de validação no MediatR.

- **xUnit**
  - Testes unitários para os Handlers.

- **Docker**
  - `Dockerfile`
  - `docker-compose.yml`

- **README**
  - Instruções para execução local.
  - Instruções para execução via Docker.

---

## Endpoints Esperados

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/auth/login` | Retorna um JWT. Usuário fixo: `dev@martech.com` / `Senha@123` |
| `POST` | `/api/orders` | Cria um novo pedido. Requer autenticação. |
| `GET` | `/api/orders` | Lista pedidos com paginação: `?page=1&pageSize=10`. Requer autenticação. |
| `GET` | `/api/orders/{id}` | Retorna um pedido pelo ID. Requer autenticação. |
| `PATCH` | `/api/orders/{id}/cancel` | Cancela um pedido. Requer autenticação. |

---

## Regras de Negócio

- Um pedido deve ter **pelo menos 1 item**.
- `UnitPrice` deve ser **maior que zero**.
- `Quantity` deve ser **maior que zero**.
- Apenas pedidos com status `Pending` podem ser cancelados.
- O `TotalAmount`, calculado pela soma de:

```text
UnitPrice * Quantity
```

deve ser calculado **no domínio**, e não na camada de aplicação.

---

## Desejável — Não Eliminatório

### Logging com Serilog

Implementar um Pipeline Behavior de logging que registre:

- Request das Commands/Queries.
- Response das Commands/Queries.
- Tempo de execução.

### Testes de Integração

Criar pelo menos um teste de integração utilizando:

```csharp
WebApplicationFactory
```

para validar pelo menos um endpoint.

### SonarQube

Configurar:

- SonarQube; ou
- `dotnet-sonarscanner`

no `docker-compose`.

### OpenTelemetry

Adicionar configuração básica de OpenTelemetry com exportação dos dados para o console.

---

## O que Não Fazer

- Não coloque lógica de negócio em **Controllers**.
- Não coloque lógica de negócio em **Infrastructure**.
- Não crie repositórios genéricos como:

```csharp
IRepository<T>
```

sem um motivo real.

> Caso utilize um repositório genérico, justifique a decisão.

- Não entregue o projeto sem pelo menos os **testes unitários dos Handlers**.