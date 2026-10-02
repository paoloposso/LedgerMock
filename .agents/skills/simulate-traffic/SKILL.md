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

### Step 1: Execute Transfer
Use your terminal tool to execute the following curl command, replacing the variables with the parameters:

```bash
curl -s -X POST http://localhost:5000/transfer \
-H "Content-Type: application/json" \
-d '{"AccountId":"{accountId}", "Amount":{amount}, "Type":"{type}"}'
```

### Step 2: Fetch Final Balance
Use your terminal tool to query the ledger to ensure the Event Sourcing calculation worked:

```bash
curl -s http://localhost:5000/account/{accountId}/balance
```

Display the final JSON result to the user so they can verify the balance!
