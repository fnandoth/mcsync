# GitHub

English primary documentation. Spanish version: [README.es.md](README.es.md)

## Main responsibility

`GitHub` implements the remote control plane: read/write `state.json` with SHA-based atomic updates for lease, heartbeat, and snapshot publication.

Components:

- `GitHubClient`: HTTP client for GitHub Contents API.
- `GitHubStateStore`: `IStateStore` implementation with lease rules and conflict retries.

## Lease and state flow

```mermaid
sequenceDiagram
    participant O as SyncOrchestrator
    participant S as GitHubStateStore
    participant C as GitHubClient
    participant GH as GitHub API

    O->>S: TryAcquireLease
    S->>C: GetJson(state.json)
    C->>GH: GET /contents/{state}
    GH-->>C: state + sha
    S->>S: validate current lease
    S->>C: PutJson(updatedState, sha)
    C->>GH: PUT /contents/{state}
    GH-->>C: new sha
    C-->>S: ok
    S-->>O: AcquireLeaseResult(success)
```

## Publish Snapshot

```mermaid
flowchart TD
    A[PublishSnapshot] --> B[Read state + sha]
    B --> C{Does the lease match?}
    C -- No --> D[Abort with error]
    C -- Yes --> E[Update version/checksum/snapshotRef]
    E --> F[Status = Idle, Host = null]
    F --> G[PUT with current sha]
    G --> H{Conflict?}
    H -- Yes --> B
    H -- No --> I[Published status]
```
