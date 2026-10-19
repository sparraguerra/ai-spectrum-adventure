# Recurso 07: Exploración dinámica

## Secuencia

```mermaid
sequenceDiagram
    participant G as Juego
    participant W as World use case
    participant R as Reglas
    participant D as Base de datos
    participant A as IA opcional
    G->>W: solicitar una dirección sin salida declarada
    W->>W: comprobar que no hay salidas autoritativas
    alt Ya existen salidas
        W-->>G: rechazo determinista
    else Frontera expandible
    W->>R: derivar candidato con semilla y GenerationKey
    R-->>W: candidato válido o rechazo
    W->>D: persistir frontera de forma atómica
    D-->>W: mundo ganador
    W->>A: enriquecer descripción opcional
    W-->>G: lugar persistido y narración
    end
```

## Mensaje oral

"Las salidas escritas por el autor son autoridad: no generamos rutas adicionales desde ellas. Sólo en una frontera expandible existe primero un lugar válido y persistido; después puede existir una descripción enriquecida. La `GenerationKey` hace idempotente esa exploración concurrente: dos solicitudes de la misma frontera reutilizan el mismo resultado."

## Referencia ampliada

- [Mundo dinámico](../runtime-flows.md)