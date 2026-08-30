# Guia de Testes Manuais — API de Pedidos

Referenciado a partir do [README](../README.md#rodando-os-cenários). Checklist de cenários de teste para todos os endpoints, um grupo por endpoint, organizado por response HTTP possível. Cada cenário traz o `curl` para reproduzir e o corpo de resposta esperado — útil tanto para reproduzir manualmente (Swagger/Postman/`curl`) quanto como referência do contrato de cada endpoint.

Marcar `[x]` quando o cenário foi validado (manualmente ou por teste automatizado) e o comportamento bate com o esperado. Cenários com gap conhecido ficam `[ ]` com uma nota explicando o desvio — não marcar até corrigir.

Base URL usada nos exemplos: `http://localhost:5000` rodando localmente (`dotnet run`), ou `http://localhost:8080` via Docker (`docker compose up`) — troque a porta conforme como você subiu a aplicação.

## Setup — obter um token

A maioria dos endpoints exige `Authorization: Bearer <token>`. Para os exemplos abaixo, obtenha um token e exporte como variável de ambiente:

```bash
TOKEN=$(curl -s -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"dev@martech.com","password":"Senha@123"}' \
  | sed -n 's/.*"accessToken":"\([^"]*\)".*/\1/p')
```

Onde os exemplos precisarem de um pedido já existente, use o `id` retornado por um `POST /api/orders` anterior e exporte como `ORDER_ID`.

---

## 1. `POST /auth/login`

Endpoint anônimo — não exige token.

### 200 OK

- [x] Credenciais válidas

```bash
curl -i -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"dev@martech.com","password":"Senha@123"}'
```

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-08-30T06:00:00Z"
}
```

- [x] Email com capitalização diferente (comparação `OrdinalIgnoreCase`)

```bash
curl -i -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"DEV@MARTECH.COM","password":"Senha@123"}'
```

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-08-30T06:00:00Z"
}
```

### 401 Unauthorized

- [ ] Email inexistente (não verificado em runtime nesta rodada, mas mesmo caminho de código do próximo item)

```bash
curl -i -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"outro@x.com","password":"Senha@123"}'
```

```
(corpo vazio)
```

- [x] Senha incorreta

```bash
curl -i -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"dev@martech.com","password":"senha-errada"}'
```

```
(corpo vazio)
```

- [ ] Senha com capitalização diferente (comparação `Ordinal`, case-sensitive — não verificado em runtime)

```bash
curl -i -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"dev@martech.com","password":"senha@123"}'
```

```
(corpo vazio)
```

- [x] Campo `email` ausente

```bash
curl -i -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"password":"Senha@123"}'
```

```
(corpo vazio)
```

- [x] Corpo vazio `{}`

```bash
curl -i -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d '{}'
```

```
(corpo vazio)
```

### 400 Bad Request

- [ ] Corpo ausente / JSON malformado (não verificado nesta rodada)

```bash
curl -i -X POST http://localhost:5000/auth/login \
  -H "Content-Type: application/json" \
  -d 'não é json'
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400
}
```

---

## 2. `POST /api/orders`

### 201 Created

- [x] Um item

```bash
curl -i -X POST http://localhost:5000/api/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "11111111-1111-1111-1111-111111111111",
    "items": [
      { "productName": "Teclado Mecânico", "quantity": 1, "unitPrice": 350.00 }
    ]
  }'
```

```json
{
  "id": "8ba70864-df13-4695-b4e6-e568ebab084a",
  "customerId": "11111111-1111-1111-1111-111111111111",
  "status": "Pending",
  "createdAt": "2026-08-30T04:26:33.4610506Z",
  "totalAmount": 350.00
}
```

`Location: /api/orders/8ba70864-df13-4695-b4e6-e568ebab084a` no header.

- [x] Múltiplos itens (`totalAmount` = soma de `unitPrice * quantity`)

```bash
curl -i -X POST http://localhost:5000/api/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "22222222-2222-2222-2222-222222222222",
    "items": [
      { "productName": "Monitor 27\"", "quantity": 2, "unitPrice": 899.50 },
      { "productName": "Cabo HDMI", "quantity": 3, "unitPrice": 25.00 }
    ]
  }'
```

```json
{
  "id": "8ba70864-df13-4695-b4e6-e568ebab084a",
  "customerId": "22222222-2222-2222-2222-222222222222",
  "status": "Pending",
  "createdAt": "2026-08-30T04:25:03.820803Z",
  "totalAmount": 1874.00
}
```

### 400 Bad Request

- [x] `customerId` vazio (`Guid.Empty`)

```bash
curl -i -X POST http://localhost:5000/api/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "00000000-0000-0000-0000-000000000000",
    "items": [{ "productName": "Mouse", "quantity": 1, "unitPrice": 100 }]
  }'
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation error",
  "status": 400,
  "errors": {
    "CustomerId": ["CustomerId is required."]
  },
  "traceId": "00-...-00"
}
```

- [x] `items` ausente/nulo

```bash
curl -i -X POST http://localhost:5000/api/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "customerId": "11111111-1111-1111-1111-111111111111" }'
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation error",
  "status": 400,
  "errors": {
    "Items": ["At least one item is required."]
  },
  "traceId": "00-...-00"
}
```

- [x] `items` vazio

```bash
curl -i -X POST http://localhost:5000/api/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "customerId": "11111111-1111-1111-1111-111111111111", "items": [] }'
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation error",
  "status": 400,
  "errors": {
    "Items": ["At least one item is required."]
  },
  "traceId": "00-...-00"
}
```

- [x] `productName` vazio num item

```bash
curl -i -X POST http://localhost:5000/api/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "11111111-1111-1111-1111-111111111111",
    "items": [{ "productName": "", "quantity": 1, "unitPrice": 100 }]
  }'
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation error",
  "status": 400,
  "errors": {
    "Items[0].ProductName": ["ProductName is required."]
  },
  "traceId": "00-...-00"
}
```

- [x] `quantity` = 0 ou negativa

```bash
curl -i -X POST http://localhost:5000/api/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "11111111-1111-1111-1111-111111111111",
    "items": [{ "productName": "Mouse", "quantity": 0, "unitPrice": 100 }]
  }'
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation error",
  "status": 400,
  "errors": {
    "Items[0].Quantity": ["Quantity must be greater than zero."]
  },
  "traceId": "00-...-00"
}
```

- [x] `unitPrice` = 0 ou negativo

```bash
curl -i -X POST http://localhost:5000/api/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "11111111-1111-1111-1111-111111111111",
    "items": [{ "productName": "Mouse", "quantity": 1, "unitPrice": 0 }]
  }'
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation error",
  "status": 400,
  "errors": {
    "Items[0].UnitPrice": ["UnitPrice must be greater than zero."]
  },
  "traceId": "00-...-00"
}
```

- [ ] Múltiplos campos inválidos ao mesmo tempo (erros agrupados por propriedade — não verificado nesta rodada)

```bash
curl -i -X POST http://localhost:5000/api/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "00000000-0000-0000-0000-000000000000",
    "items": [{ "productName": "", "quantity": 0, "unitPrice": 0 }]
  }'
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation error",
  "status": 400,
  "errors": {
    "CustomerId": ["CustomerId is required."],
    "Items[0].ProductName": ["ProductName is required."],
    "Items[0].Quantity": ["Quantity must be greater than zero."],
    "Items[0].UnitPrice": ["UnitPrice must be greater than zero."]
  },
  "traceId": "00-...-00"
}
```

- [ ] `Content-Type` ausente ou JSON malformado (não verificado nesta rodada)

### 401 Unauthorized

- [x] Sem header `Authorization`

```bash
curl -i -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId":"11111111-1111-1111-1111-111111111111","items":[{"productName":"Mouse","quantity":1,"unitPrice":100}]}'
```

```
(corpo vazio)
WWW-Authenticate: Bearer
```

- [x] Token com string arbitrária

```bash
curl -i -X POST http://localhost:5000/api/orders \
  -H "Authorization: Bearer token-invalido" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"11111111-1111-1111-1111-111111111111","items":[{"productName":"Mouse","quantity":1,"unitPrice":100}]}'
```

```
(corpo vazio)
WWW-Authenticate: Bearer error="invalid_token"
```

- [x] Token bem-formado, assinado com chave errada

```bash
curl -i -X POST http://localhost:5000/api/orders \
  -H "Authorization: Bearer <jwt-assinado-com-outra-chave>" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"11111111-1111-1111-1111-111111111111","items":[{"productName":"Mouse","quantity":1,"unitPrice":100}]}'
```

```
(corpo vazio)
WWW-Authenticate: Bearer error="invalid_token", error_description="The signature key was not found"
```

- [x] Token estruturalmente válido, mas expirado

```bash
curl -i -X POST http://localhost:5000/api/orders \
  -H "Authorization: Bearer <jwt-expirado>" \
  -H "Content-Type: application/json" \
  -d '{"customerId":"11111111-1111-1111-1111-111111111111","items":[{"productName":"Mouse","quantity":1,"unitPrice":100}]}'
```

```
(corpo vazio)
WWW-Authenticate: Bearer error="invalid_token", error_description="The token expired at '2020-01-01 00:00:00'"
```

---

## 3. `GET /api/orders?page=&pageSize=`

### 200 OK

- [x] Sem query string (defaults `page=1`, `pageSize=10`)

```bash
curl -i http://localhost:5000/api/orders \
  -H "Authorization: Bearer $TOKEN"
```

```json
{
  "items": [
    {
      "id": "8ba70864-df13-4695-b4e6-e568ebab084a",
      "customerId": "22222222-2222-2222-2222-222222222222",
      "status": "Pending",
      "createdAt": "2026-08-30T04:25:03.820803Z",
      "totalAmount": 1874.00
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1,
  "totalPages": 1
}
```

- [x] `page`/`pageSize` explícitos, com 43 pedidos no banco (`page=2&pageSize=10`)

```bash
curl -i "http://localhost:5000/api/orders?page=2&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"
```

```json
{
  "items": [ /* 10 pedidos */ ],
  "page": 2,
  "pageSize": 10,
  "totalCount": 43,
  "totalPages": 5
}
```

- [x] Página além do total (`page=6&pageSize=10`, mesmos 43 pedidos) — não é `404`

```bash
curl -i "http://localhost:5000/api/orders?page=6&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"
```

```json
{
  "items": [],
  "page": 6,
  "pageSize": 10,
  "totalCount": 43,
  "totalPages": 5
}
```

- [x] `pageSize` grande, sem limite superior configurado (decisão documentada)

```bash
curl -i "http://localhost:5000/api/orders?page=1&pageSize=999999" \
  -H "Authorization: Bearer $TOKEN"
```

```json
{
  "items": [ /* todos os pedidos existentes */ ],
  "page": 1,
  "pageSize": 999999,
  "totalCount": 43,
  "totalPages": 1
}
```

### 400 Bad Request

- [x] `page=0`

```bash
curl -i "http://localhost:5000/api/orders?page=0&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation error",
  "status": 400,
  "errors": {
    "Page": ["Page must be greater than zero."]
  },
  "traceId": "00-...-00"
}
```

- [x] `pageSize` negativo

```bash
curl -i "http://localhost:5000/api/orders?page=1&pageSize=-5" \
  -H "Authorization: Bearer $TOKEN"
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation error",
  "status": 400,
  "errors": {
    "PageSize": ["PageSize must be greater than zero."]
  },
  "traceId": "00-...-00"
}
```

- [x] `page` negativo (agora coberto por [`GetOrdersQueryValidatorTests.cs`](../tests/ECommerce.Application.Tests/Features/Orders/Queries/GetOrders/GetOrdersQueryValidatorTests.cs), não verificado via HTTP nesta rodada)

```bash
curl -i "http://localhost:5000/api/orders?page=-1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation error",
  "status": 400,
  "errors": {
    "Page": ["Page must be greater than zero."]
  },
  "traceId": "00-...-00"
}
```

- [x] `pageSize=0` (agora coberto por [`GetOrdersQueryValidatorTests.cs`](../tests/ECommerce.Application.Tests/Features/Orders/Queries/GetOrders/GetOrdersQueryValidatorTests.cs), não verificado via HTTP nesta rodada)

```bash
curl -i "http://localhost:5000/api/orders?page=1&pageSize=0" \
  -H "Authorization: Bearer $TOKEN"
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation error",
  "status": 400,
  "errors": {
    "PageSize": ["PageSize must be greater than zero."]
  },
  "traceId": "00-...-00"
}
```

### 401 Unauthorized

- [x] Sem header `Authorization`

```bash
curl -i http://localhost:5000/api/orders
```

```
(corpo vazio)
WWW-Authenticate: Bearer
```

### 400 Bad Request — binding malformado (corrigido)

- [x] `page` não numérico

```bash
curl -i "http://localhost:5000/api/orders?page=abc&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Invalid request",
  "status": 400,
  "detail": "Failed to bind parameter \"int page\" from \"abc\".",
  "traceId": "00-...-00"
}
```

Causa original: `BadHttpRequestException` do binding do Minimal API não era tratada pelo `GlobalExceptionHandler` (caía no branch genérico `500`). Corrigido com um case dedicado no `switch`. Teste de regressão: [`GetOrdersIntegrationTests.cs`](../tests/ECommerce.IntegrationTests/GetOrdersIntegrationTests.cs).

- [x] `pageSize` não numérico

```bash
curl -i "http://localhost:5000/api/orders?page=1&pageSize=abc" \
  -H "Authorization: Bearer $TOKEN"
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Invalid request",
  "status": 400,
  "detail": "Failed to bind parameter \"int pageSize\" from \"abc\".",
  "traceId": "00-...-00"
}
```

---

## 4. `GET /api/orders/{id}`

### 200 OK

- [x] Pedido existente

```bash
curl -i http://localhost:5000/api/orders/$ORDER_ID \
  -H "Authorization: Bearer $TOKEN"
```

```json
{
  "id": "8ba70864-df13-4695-b4e6-e568ebab084a",
  "customerId": "22222222-2222-2222-2222-222222222222",
  "status": "Pending",
  "createdAt": "2026-08-30T04:25:03.820803",
  "totalAmount": 1874.00,
  "items": [
    {
      "id": "b8959a38-c138-4bd1-a0c1-7429a7f3a071",
      "productName": "Cabo HDMI",
      "quantity": 3,
      "unitPrice": 25.00,
      "totalPrice": 75.00
    },
    {
      "id": "e6564172-1f93-4ca8-89e3-d6f9e1aee517",
      "productName": "Monitor 27\"",
      "quantity": 2,
      "unitPrice": 899.50,
      "totalPrice": 1799.00
    }
  ]
}
```

### 404 Not Found

- [x] Pedido inexistente (GUID bem-formado, mas não cadastrado)

```bash
curl -i http://localhost:5000/api/orders/99999999-9999-9999-9999-999999999999 \
  -H "Authorization: Bearer $TOKEN"
```

```
(corpo vazio)
```

- [x] `id` na rota não é um GUID válido

```bash
curl -i http://localhost:5000/api/orders/nao-e-um-guid \
  -H "Authorization: Bearer $TOKEN"
```

```
(corpo vazio)
```

### 401 Unauthorized

- [x] Sem header `Authorization`

```bash
curl -i http://localhost:5000/api/orders/$ORDER_ID
```

```
(corpo vazio)
WWW-Authenticate: Bearer
```

---

## 5. `PATCH /api/orders/{id}/cancel`

### 200 OK

- [x] Pedido `Pending` cancelado com sucesso

```bash
curl -i -X PATCH http://localhost:5000/api/orders/$ORDER_ID/cancel \
  -H "Authorization: Bearer $TOKEN"
```

```json
{
  "id": "40656777-05db-4fbb-b51a-6133e6defa43",
  "status": "Cancelled"
}
```

### 404 Not Found

- [x] Pedido inexistente

```bash
curl -i -X PATCH http://localhost:5000/api/orders/99999999-9999-9999-9999-999999999999/cancel \
  -H "Authorization: Bearer $TOKEN"
```

```
(corpo vazio)
```

- [x] `id` na rota não é um GUID válido

```bash
curl -i -X PATCH http://localhost:5000/api/orders/nao-e-um-guid/cancel \
  -H "Authorization: Bearer $TOKEN"
```

```
(corpo vazio)
```

### 409 Conflict

- [x] Cancelar um pedido que já está `Cancelled` (endpoint não é idempotente)

```bash
# 1ª chamada: 200 OK, pedido vira Cancelled
curl -s -X PATCH http://localhost:5000/api/orders/$ORDER_ID/cancel -H "Authorization: Bearer $TOKEN" > /dev/null

# 2ª chamada: mesma requisição, resultado diferente
curl -i -X PATCH http://localhost:5000/api/orders/$ORDER_ID/cancel \
  -H "Authorization: Bearer $TOKEN"
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Order cannot be cancelled",
  "status": 409,
  "detail": "Order with status 'Cancelled' cannot be cancelled.",
  "traceId": "00-...-00"
}
```

### 401 Unauthorized

- [x] Sem header `Authorization`

```bash
curl -i -X PATCH http://localhost:5000/api/orders/$ORDER_ID/cancel
```

```
(corpo vazio)
WWW-Authenticate: Bearer
```

---

## Cobertura por teste automatizado

Os cenários marcados `[x]` acima foram validados manualmente (runtime) ao longo do desenvolvimento. Os seguintes já têm também teste automatizado no código:

| Cenário | Teste |
|---|---|
| `GET /api/orders/{id}` — encontrado, não encontrado, `CancellationToken` propagado | [`GetOrderByIdQueryHandlerTests.cs`](../tests/ECommerce.Application.Tests/Features/Orders/Queries/GetOrderById/GetOrderByIdQueryHandlerTests.cs) |
| `GET /api/orders` — página com registros, página vazia, `TotalCount=0` | [`GetOrdersQueryHandlerTests.cs`](../tests/ECommerce.Application.Tests/Features/Orders/Queries/GetOrders/GetOrdersQueryHandlerTests.cs) |
| `PATCH .../cancel` — `Pending`, inexistente, `Cancelled`, `Confirmed` | [`CancelOrderCommandHandlerTests.cs`](../tests/ECommerce.Application.Tests/Features/Orders/Commands/CancelOrder/CancelOrderCommandHandlerTests.cs) |
| `POST /api/orders` — validação de entrada (todas as regras) | [`CreateOrderCommandValidatorTests.cs`](../tests/ECommerce.Application.Tests/Features/Orders/Commands/CreateOrder/CreateOrderCommandValidatorTests.cs) |
| `POST /api/orders` — persistência via `AddAsync`, mapeamento `Order` → `CreateOrderResult`, `CancellationToken` propagado | [`CreateOrderCommandHandlerTests.cs`](../tests/ECommerce.Application.Tests/Features/Orders/Commands/CreateOrder/CreateOrderCommandHandlerTests.cs) |
| `GET /api/orders` — validação de `page`/`pageSize` (`<= 0`) | [`GetOrdersQueryValidatorTests.cs`](../tests/ECommerce.Application.Tests/Features/Orders/Queries/GetOrders/GetOrdersQueryValidatorTests.cs) |
| Fluxo completo `POST → GET → PATCH cancel → GET`, e cancelamento repetido (`409`) | [`CancelOrderIntegrationTests.cs`](../tests/ECommerce.IntegrationTests/CancelOrderIntegrationTests.cs) |
| `POST /api/orders` fim a fim — sem token (`401`), payload inválido via pipeline real (`400`), criação + releitura provando persistência real com `TotalAmount`/`TotalPrice` calculados pelo domínio | [`CreateOrderIntegrationTests.cs`](../tests/ECommerce.IntegrationTests/CreateOrderIntegrationTests.cs) |
| Invariantes do Domain: sem itens, `Quantity`/`UnitPrice` zero ou negativo, `TotalAmount`, status inicial `Pending`, `Order.Cancel()` (`Pending`/`Cancelled`/`Confirmed`) — CT-DOMAIN-01 a 10 | [`OrderTests.cs`](../tests/ECommerce.Application.Tests/Domain/Entities/OrderTests.cs) |

Todos os quatro Handlers (`CreateOrder`, `GetOrderById`, `GetOrders`, `CancelOrder`) e os validators relevantes (`CreateOrderCommand`, `GetOrdersQuery`) têm teste unitário dedicado — gap fechado na TASK 10.

### Catálogo formal de casos de teste dos Handlers (TASK 10)

| Handler | Casos |
|---|---|
| `CreateOrderCommandHandler` | CT-CREATE-01 (criação válida), CT-CREATE-02 (`TotalAmount` calculado pelo domínio), CT-CREATE-03 (entidade capturada e inspecionada), CT-CREATE-04 (`CancellationToken`) |
| `GetOrderByIdQueryHandler` | CT-GET-ID-01 (encontrado), CT-GET-ID-02 (inexistente, sem exceção), CT-GET-ID-03 (`CancellationToken`) |
| `GetOrdersQueryHandler` | CT-LIST-01 (página com resultados), CT-LIST-02 (nenhum pedido), CT-LIST-03 (última página parcial), CT-LIST-04 (página além do total), CT-LIST-05 (`CancellationToken`) |
| `CancelOrderCommandHandler` | CT-CANCEL-01 (`Pending`→`Cancelled`), CT-CANCEL-02 (inexistente), CT-CANCEL-03 (`Cancelled`→exceção), CT-CANCEL-04 (`Confirmed`→exceção), CT-CANCEL-05 (`CancellationToken` em `GetByIdForUpdateAsync` e `UpdateAsync`) |

Inventário de `IRequestHandler<,>` no código confirma exatamente esses 4 Handlers — nenhum outro existe (login não passa pelo MediatR, decisão documentada no README).
