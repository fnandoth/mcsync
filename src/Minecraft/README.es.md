# Minecraft

## Funcion central

`Minecraft` gestiona el **data plane local del mundo** y el ciclo de vida del proceso Java:

- `ServerManager`: inicia, monitorea y detiene `server.jar`.
- `WorldManager`: prepara carpeta, extrae snapshots remotos y genera snapshots locales.

## Flujo local de arranque y cierre

```mermaid
flowchart TD
    A[PrepareServerFolder] --> B[Copiar server.jar si cambia]
    B --> C[Escribir eula=true]
    C --> D[StartAsync]
    D --> E[Esperar log de readiness]
    E --> F[Hosting]

    G[StopGracefullyAsync] --> H[save-off]
    H --> I[save-all flush]
    I --> J[stop]
    J --> K{Salio en timeout?}
    K -- No --> L[KillAsync]
    K -- Si --> M[CreateSnapshotAsync]
    L --> M
    M --> N[ZIP + SHA-256]
```

## Flujo de snapshot remoto -> local

```mermaid
flowchart LR
    A[Download ZIP] --> B[ExtractSnapshotAsync]
    B --> C[Limpiar carpeta server]
    C --> D[Extraer ZIP]
    D --> E[Restaurar server.jar]
    E --> F[Actualizar LocalWorldState]
```

## Motivo del diseno

1. **Separar proceso Java del orchestration**: simplifica fallos y manejo de lifecycle.
2. **Snapshot completo ZIP**: estrategia robusta para MVP, facil de auditar y recuperar.
3. **Checksum SHA-256**: verifica integridad y sincronizacion local/remota.
4. **Comandos de cierre ordenado** (`save-all flush`): minimiza perdida de progreso.
