# Recurso 07: Exploración dinámica

## Secuencia

```mermaid
sequenceDiagram
    participant G as Juego
    participant W as World use case
    participant R as Reglas
    participant D as Base de datos
    participant A as IA opcional
    G->>W: explorar dirección desconocida
    W->>R: derivar candidato con semilla y GenerationKey
    R-->>W: candidato válido o rechazo
    W->>D: persistir frontera de forma atómica
    D-->>W: mundo ganador
    W->>A: enriquecer descripción opcional
    W-->>G: lugar persistido y narración
```

## Mensaje oral

"Primero existe un lugar válido y persistido; después puede existir una descripción enriquecida. La `GenerationKey` hace idempotente la exploración concurrente: dos solicitudes de la misma frontera reutilizan el mismo resultado."

## Referencia ampliada

- [Mundo dinámico](../runtime-flows.md)