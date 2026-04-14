# Storage

English primary documentation. Spanish version: [README.es.md](README.es.md)

## Main responsibility

`Storage` abstracts where world snapshots are uploaded and downloaded.

- `ISnapshotStorageProvider`: provider-agnostic upload/download contract.
- `GitHubSnapshotStorageProvider`: current implementation using GitHub Contents API.

## Upload flow

```mermaid
sequenceDiagram
    participant O as SyncOrchestrator
    participant P as GitHubSnapshotStorageProvider
    participant C as GitHubClient
    participant GH as GitHub API

    O->>P: UploadSnapshotAsync(artifact, worldVersion)
    P->>P: build snapshotRef (world-vXXXXXX.zip)
    P->>C: PutBytes(snapshotRef, bytes)
    C->>GH: PUT /contents/{snapshotRef}
    GH-->>C: ok
    C-->>P: ok
    P-->>O: SnapshotUploadResult
```
## Download flow

```mermaid
flowchart TD
    A[DownloadSnapshotAsync] --> B[GetBytes snapshotRef]
    B --> C[Save ZIP in local temp]
    C --> D[Return local path]
```