# MCSync

MCSync is a desktop app (C# .NET 8 + WinForms) for **rotating the host of a Minecraft Java world** among friends using a **single-writer + lease** model.

## What It Solves

Eliminates the manual process of "send me the world as a ZIP" every time the host changes.  
MCSync automatically coordinates:

1. who can host at any given moment,
2. when to download the latest consistent version,
3. when to upload the new snapshot after finishing.

## Functional Architecture (Summary)

```mermaid
flowchart TD
    UI[Dashboard / Tray] --> ORCH[SyncOrchestrator]
    ORCH --> STATE[IStateStore]
    ORCH --> WORLD[WorldManager]
    ORCH --> SERVER[ServerManager]
    ORCH --> TUNNEL[TunnelManager]
    ORCH --> SNAP[ISnapshotStorageProvider]

    STATE --> GHSTATE[GitHub state.json]
    SNAP --> GHSNAP[GitHub snapshots]
    SERVER --> JAVA[java + server.jar]
    TUNNEL --> PLAYIT[playit-cli]
```

## Prerequisites

1. Windows.
2. `.NET 8 SDK` (if running from source code).
3. `Java` installed and accessible.
4. `server.jar` available locally (downloaded from the official [Minecraft](https://www.minecraft.net/en-us/download/server) page).
5. `playit` installed and accessible in PATH (downloaded from the official [playit.gg](https://playit.gg/download/windows) page — must be the `.msi` installer).
6. Private GitHub repository for `state.json` and snapshots.
7. GitHub token with read/write permissions to the repo.

## Getting Started

1. Clone the repository.
2. Build:

```powershell
dotnet build --nologo
```

3. Run:

```powershell
dotnet run --project .\MCSync.csproj
```

4. Open **SETTINGS** and fill in at minimum:
   - GitHub owner / repo / branch / token
   - path to `server.jar`
   - `playit.gg` URL
   - minimum and maximum Java memory
5. Save the configuration.

## Daily Use

### Start Hosting

1. Press **START HOST**.
2. The app validates the remote lease.
3. If there is a newer remote snapshot, it downloads it.
4. Prepares the server folder and starts `server.jar`.
5. Starts the tunnel and publishes the endpoint.

### Stop Hosting

1. Press **STOP HOST AND SYNC**.
2. Marks the remote state as `Transferring`.
3. Stops the server and tunnel.
4. Compresses the world, calculates the checksum, and uploads the snapshot.
5. Publishes the new version and releases the lease.

### Full Cycle Flow

```mermaid
sequenceDiagram
    participant U as User
    participant UI as Dashboard/Tray
    participant O as SyncOrchestrator
    participant S as GitHubStateStore
    participant W as WorldManager
    participant M as ServerManager
    participant T as TunnelManager
    participant P as SnapshotProvider

    U->>UI: Start host
    UI->>O: StartHostingAsync
    O->>S: TryAcquireLease
    S-->>O: Lease OK
    O->>W: SyncDownIfRequired
    O->>M: StartAsync
    O->>T: StartAsync
    O->>S: UpdateTunnelAddress + Heartbeat
    O-->>UI: Hosting

    U->>UI: Stop host
    UI->>O: StopHostingAsync
    O->>S: MarkTransferring
    O->>M: StopGracefullyAsync
    O->>T: StopAsync
    O->>W: CreateSnapshotAsync
    O->>P: UploadSnapshotAsync
    O->>S: PublishSnapshot
    O-->>UI: Idle
```

## Project Status

The app is in **phase 1 (functional demo)**: end-to-end operable flow, UI for daily use, and local logging.

## Module Documentation

- `src/Core/README.md`: orchestration, states, and consistency.
- `src/GitHub/README.md`: control plane and lease semantics.
- `src/Minecraft/README.md`: local server and snapshot lifecycle.
- `src/Storage/README.md`: snapshot abstraction and provider.
- `src/Tunnel/README.md`: `playit-cli` lifecycle.