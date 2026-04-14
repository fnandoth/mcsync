# Tunnel

## Funcion central

`Tunnel` controla el proceso de `playit-cli` para exponer el servidor local por una direccion publica durante el hosting.

Componente:

- `TunnelManager`: arranque, logging, estado y parada del proceso del tunel.

## Flujo de ciclo de vida

```mermaid
flowchart TD
    A[StartAsync] --> B{PlayitGGUrl configurada?}
    B -- No --> C[Error]
    B -- Si --> D[Iniciar proceso playit]
    D --> E[Escuchar stdout/stderr]
    E --> F[Retornar direccion configurada]
    F --> G[Tunel activo]

    H[StopAsync] --> I{Proceso activo?}
    I -- Si --> J[Kill + WaitForExit]
    I -- No --> K[No-op]
```

## Motivo del diseño

1. **Encapsulacion de proceso externo**: evita mezclar detalles de `playit` en `Core`.
2. **Interfaz minima** (start/stop): reduce superficie de error en una dependencia externa.
3. **Logging centralizado**: facilita diagnostico de conectividad y arranque.
