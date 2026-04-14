# Tunnel

English primary documentation. Spanish version: [README.es.md](README.es.md)

## Main responsibility

`Tunnel` manages the `playit-cli` process to expose the local Minecraft server through a public endpoint while hosting.

Component:

- `TunnelManager`: start, logging, state tracking, and stop logic for the tunnel process.

## Lifecycle flow

```mermaid
flowchart TD
    A[StartAsync] --> B{PlayitGGUrl configured?}
    B -- No --> C[Error]
    B -- Yes --> D[Start playit process]
    D --> E[Read stdout/stderr]
    E --> F[Return configured address]
    F --> G[Tunnel active]

    H[StopAsync] --> I{Process active?}
    I -- Yes --> J[Kill + WaitForExit]
    I -- No --> K[No-op]
```
