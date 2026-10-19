# Recurso 05: Turno determinista

## Secuencia

```mermaid
sequenceDiagram
    participant P as Jugador
    participant I as Intérprete IA
    participant E as Motor
    participant S as Estado
    participant N as Narrador IA
    P->>I: comando libre
    Note right of I: vocabulario de la aventura activa
    I-->>E: intención estructurada
    E->>E: valida reglas
    E->>S: persiste eventos y estado
    S-->>N: hechos confirmados
    N-->>P: narrativa
```

## Mensaje oral

"Un turno no es texto que modifica una base de datos. El intérprete resuelve las expresiones del jugador con el vocabulario de la aventura y produce una intención; después llegan validación, eventos y persistencia. La narración ocurre al final, a partir de hechos."

## Referencia ampliada

- [Flujo de acción normal](../runtime-flows.md)