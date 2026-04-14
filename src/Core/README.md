# Core

English primary documentation. Spanish version: [README.es.md](README.es.md)

## Main responsibility

`Core` contains MCSync orchestration logic: it handles config, shared/local state, lease rules, and the full host lifecycle.

Main components:

- `SyncOrchestrator`: main use case (`start -> host -> stop -> publish`).
- `AppState` / `HostLeaseInfo`: shared remote state contract.
- `IStateStore`: control-plane port.
- `UserConfig` + `ConfigStore`: persisted configuration and validation.
- `LocalWorldStateStore`: applied local version/checksum.
- `AppLogger`: audit trail and UI events.

## Main flow

```mermaid
flowchart TD
    A[StartHostingAsync] --> B[Load + Validate Config]
    B --> C[TryAcquireLease]
    C --> D[SyncDownIfRequired]
    D --> E[PrepareServerFolder]
    E --> F[Start server.jar]
    F --> G[Start tunnel]
    G --> H[Update tunnel address]
    H --> I[Status: Hosting]

    J[StopHostingAsync] --> K[MarkTransferring]
    K --> L[Stop server + tunnel]
    L --> M[Create snapshot]
    M --> N[Upload snapshot]
    N --> O[PublishSnapshot + Release lease]
    O --> P[Status: Idle]
```
