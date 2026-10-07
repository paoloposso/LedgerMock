# LedgerMock

An Event Sourced, immutable ledger service built with .NET 10, Amazon DynamoDB, and asynchronous channel-based event streaming.

---

## Getting Started

### 1. Start Infrastructure (DynamoDB Local & UI)
```bash
docker compose up -d
```
* **DynamoDB Local Port:** `8000`
* **DynamoDB Admin UI:** [http://localhost:8001](http://localhost:8001)

### 2. Run the Application
```bash
dotnet run
```
The API listens on `http://localhost:5287`.

### 3. Run Tests
```bash
dotnet test
```

---

## API & cURL Examples

### 1. Deposit Funds
Credits a single account with funds (amount represented in cents / minor currency units).

```bash
curl -X POST http://localhost:5287/accounts/acc-001/deposit \
  -H "Content-Type: application/json" \
  -d '{
    "amountInCents": 20000,
    "idempotencyKey": "dep-001"
  }'
```

---

### 2. Withdraw Funds
Debits a single account.

```bash
curl -X POST http://localhost:5287/accounts/acc-001/withdraw \
  -H "Content-Type: application/json" \
  -d '{
    "amountInCents": 5000,
    "idempotencyKey": "wd-001"
  }'
```

---

### 3. Atomic Double-Entry Transfer
Moves money between two accounts atomically. Debits `sourceAccountId`, credits `destinationAccountId`, and locks the `idempotencyKey` in a single ACID transaction (`TransactWriteItems`).

```bash
curl -X POST http://localhost:5287/transfers \
  -H "Content-Type: application/json" \
  -d '{
    "sourceAccountId": "acc-001",
    "destinationAccountId": "acc-002",
    "amountInCents": 6000,
    "idempotencyKey": "txf-001"
  }'
```

---

### 4. Check Account Balance
Calculates an account's current balance on the fly by reducing/aggregating all historical events (`IEnumerable.Aggregate`).

```bash
# Check Source Account (acc-001) - Expected: 9000 cents ($90.00)
curl -X GET http://localhost:5287/accounts/acc-001/balance
```

```bash
# Check Destination Account (acc-002) - Expected: 6000 cents ($60.00)
curl -X GET http://localhost:5287/accounts/acc-002/balance
```

---

### 5. Idempotency Protection Test
Replaying any request with the same `idempotencyKey` returns a successful idempotent response without duplicating ledger operations or altering balances:

```bash
curl -X POST http://localhost:5287/transfers \
  -H "Content-Type: application/json" \
  -d '{
    "sourceAccountId": "acc-001",
    "destinationAccountId": "acc-002",
    "amountInCents": 6000,
    "idempotencyKey": "txf-001"
  }'
```
Response:
```json
{
  "message": "Transfer already processed (Idempotent response)."
}
```
