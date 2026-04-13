# GitHub

## Funcion central

`GitHub` implementa el **control plane remoto**: lectura/escritura de `state.json` y actualizaciones atomicas basadas en SHA para coordinar lease, heartbeat y publicacion de version.

Componentes:

- `GitHubClient`: cliente HTTP para GitHub Contents API.
- `GitHubStateStore`: implementacion de `IStateStore` con logica de lease y reintentos por conflicto.

## Flujo de lease y estado

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
    S->>S: validar lease actual
    S->>C: PutJson(updatedState, sha)
    C->>GH: PUT /contents/{state}
    GH-->>C: nuevo sha
    C-->>S: ok
    S-->>O: AcquireLeaseResult(success)
```

## Publicacion de snapshot

```mermaid
flowchart TD
    A[PublishSnapshot] --> B[Read state + sha]
    B --> C{Lease coincide?}
    C -- No --> D[Abortar con error]
    C -- Si --> E[Actualizar version/checksum/snapshotRef]
    E --> F[Status = Idle, Host = null]
    F --> G[PUT con sha actual]
    G --> H{Conflicto?}
    H -- Si --> B
    H -- No --> I[Estado publicado]
```

## Motivo del diseno

1. **Control de concurrencia optimista** por SHA: evita sobrescritura ciega.
2. **Reintentos acotados**: absorbe carreras entre clientes sin bloquear indefinidamente.
3. **Lease como frontera de seguridad**: ningun cliente publica o heartbeat si no posee lease vigente.
4. **GitHub como backend MVP**: reduce friccion de despliegue para validar el flujo.
