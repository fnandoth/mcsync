# Minecraft

English primary documentation. Spanish version: [README.es.md](README.es.md)

## Main responsibility

`Minecraft` handles the local world data plane and Java process lifecycle:

- `ServerManager`: starts, monitors, and stops `server.jar`.
- `WorldManager`: prepares folders, extracts remote snapshots, and creates local snapshots.

## Local start/stop flow

```mermaid
flowchart TD
    A[PrepareServerFolder] --> B[Copy server.jar if needed]
    B --> C[Write eula=true]
    C --> D[StartAsync]
    D --> E[Wait for readiness log]
    E --> F[Hosting]

    G[StopGracefullyAsync] --> H[save-off]
    H --> I[save-all flush]
    I --> J[stop]
    J --> K{Exited in timeout?}
    K -- No --> L[KillAsync]
    K -- Yes --> M[CreateSnapshotAsync]
    L --> M
    M --> N[ZIP + SHA-256]
```
## Remote-to-local snapshot flow

```mermaid
flowchart LR
    A[Download ZIP] --> B[ExtractSnapshotAsync]
    B --> C[Clean server folder]
    C --> D[Extract ZIP]
    D --> E[Restore server.jar]
    E --> F[Update LocalWorldState]
```
