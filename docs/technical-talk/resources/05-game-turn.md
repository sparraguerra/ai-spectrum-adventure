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
    I-->>E: intención estructurada
    E->>E: valida reglas
    E->>S: persiste eventos y estado
    S-->>N: hechos confirmados
    N-->>P: narrativa
```

## Mensaje oral

"Un turno no es texto que modifica una base de datos. Es una transición de estado: interpretación, validación, eventos y persistencia. La narración ocurre al final, a partir de hechos."

## Referencia ampliada

- [Flujo de acción normal](../runtime-flows.md)