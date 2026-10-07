---
name: simulate-traffic
description: Sends a parametrized test transaction to the Ledger API and checks the resulting balance.
---

# Simulate Ledger Traffic

When the user asks to "simulate traffic" or "test the ledger", use this skill.

## Required Parameters
You need three parameters from the user to run this skill:
1. `accountId` (string)
2. `amount` (number)
3. `type` (Deposit, Withdrawal, TransferIn, TransferOut)

*If the user does not provide these parameters, ask for them before proceeding or generate random ones if they say "use random data".*

## Execution Steps

### Step 1: Execute Transaction
Use your terminal tool to execute the appropriate curl command:

**Deposit:**
```bash
curl -s -X POST http://localhost:5287/accounts/{accountId}/deposit \
  -H "Content-Type: application/json" \
  -d '{"amount": {amount}, "idempotencyKey": "{idempotencyKey}"}'
```

**Withdrawal:**
```bash
curl -s -X POST http://localhost:5287/accounts/{accountId}/withdraw \
  -H "Content-Type: application/json" \
  -d '{"amount": {amount}, "idempotencyKey": "{idempotencyKey}"}'
```

**Transfer:**
```bash
curl -s -X POST http://localhost:5287/transfers \
  -H "Content-Type: application/json" \
  -d '{"sourceAccountId": "{sourceAccountId}", "destinationAccountId": "{destinationAccountId}", "amount": {amount}, "idempotencyKey": "{idempotencyKey}"}'
```

### Step 2: Fetch Final Balance
Query the ledger to verify the balance:

```bash
curl -s http://localhost:5287/accounts/{accountId}/balance
```

Display the final JSON result to the user so they can verify the balance!
