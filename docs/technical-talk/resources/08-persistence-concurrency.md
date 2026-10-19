# Recurso 08: Modelo y concurrencia

## Modelo mínimo

```mermaid
erDiagram
    ADVENTURE_DRAFT ||--o{ ADVENTURE_VERSION : publica
    ADVENTURE_DRAFT ||--o{ PLAYTEST_SESSION : crea
    ADVENTURE_VERSION ||--o{ GAME : origina
    WORLD ||--o{ WORLD_EVENT : registra
    GAME ||--o{ GAME_EVENT : registra
```

## Mensaje oral

"Los borradores son mutables y usan revisión optimista. Las versiones publicadas son snapshots inmutables. Un juego guarda su versión de origen, de modo que publicar una aventura nueva no reescribe partidas activas."

## Punto de atención

Explica que las inserciones únicas y las transacciones resuelven la carrera de exploración sin depender de memoria local de una réplica.

## Referencia ampliada

- [Modelo de datos](../architecture.md)