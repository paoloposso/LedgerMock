---
name: LedgerMock Guidelines
description: Core architectural patterns and coding standards for the Nubank Ledger Mock project.
---

# LedgerMock Architectural Patterns

This file (`AGENTS.md`) serves as the core rulebook for this project. Any AI agents interacting with this codebase must adhere to the following patterns.

## 1. Functional C# & Immutability
- **No Mutable State:** Never use classes with mutable properties for domain models. Always use `record` types.
- **Pure Functions:** Business logic must be implemented as `static` methods that take inputs and return outputs without causing side effects. Do not inject databases or external services into domain logic.
- **Null Safety:** Avoid returning `null`. Use functional concepts (like `Result<T>` or simple exceptions if appropriate for the mock) instead of null checks.

## 2. Event Sourcing
- **Append-Only:** We do not perform SQL-style `UPDATE` or `DELETE` operations on balances. 
- **Event Reducers:** State is calculated by dynamically folding (reducing/aggregating) an immutable log of historical events using `IEnumerable.Aggregate`.

## 3. Separation of Concerns
- **Domain:** Contains absolutely zero infrastructure code (no AWS SDK, no HTTP contexts).
- **Infrastructure:** `DynamoDbStore` is strictly for interacting with AWS.
- **Event Bus:** Asynchronous pub/sub is managed strictly via `System.Threading.Channels`. The HTTP API must never wait for background side-effects (like notifications).

## 4. File Segregation
- Keep classes and records segregated into their own files under their respective namespaces (e.g., `LedgerMock.Domain`). Do not dump everything into a single file.
