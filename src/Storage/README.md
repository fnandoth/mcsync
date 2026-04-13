# Storage

## Funcion central

`Storage` abstrae el backend donde se guardan y recuperan snapshots del mundo.

- `ISnapshotStorageProvider`: contrato agnostico para upload/download.
- `GitHubSnapshotStorageProvider`: implementacion actual usando GitHub Contents API.

## Flujo de subida

```mermaid
sequenceDiagram
    participant O as SyncOrchestrator
    participant P as GitHubSnapshotStorageProvider
    participant C as GitHubClient
    participant GH as GitHub API

    O->>P: UploadSnapshotAsync(artifact, worldVersion)
    P->>P: construir snapshotRef (world-vXXXXXX.zip)
    P->>C: PutBytes(snapshotRef, bytes)
    C->>GH: PUT /contents/{snapshotRef}
    GH-->>C: ok
    C-->>P: ok
    P-->>O: SnapshotUploadResult
```

## Flujo de descarga

```mermaid
flowchart TD
    A[DownloadSnapshotAsync] --> B[GetBytes snapshotRef]
    B --> C[Guardar ZIP en temp local]
    C --> D[Retornar ruta local]
```

## Motivo del diseno

1. **Puerto de almacenamiento**: desacopla `Core` del proveedor concreto.
2. **Ruta versionada** por `world-vNNNNNN.zip`: trazabilidad simple de snapshots.
3. **Evolucion futura**: habilita migrar a Drive/Dropbox/B2 sin rediseñar orquestacion.
