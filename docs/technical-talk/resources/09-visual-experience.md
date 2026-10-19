# Recurso 09: Pipeline visual

## Secuencia

```mermaid
sequenceDiagram
    participant UI as Cliente
    participant A as Aplicación
    participant Q as Trabajo en segundo plano
    participant B as Blob Storage
    participant V as IA visual
    UI->>A: solicita escena
    A-->>UI: estado factual disponible
    A->>Q: encola imagen opcional
    Q->>B: consulta caché
    alt sin caché
        Q->>V: solicita imagen con contexto permitido
        V-->>Q: imagen o fallo
        Q->>B: guarda bytes si hay éxito
    end
    UI->>A: consulta estado visual
    A-->>UI: imagen, cargando o fallback
```

## Mensaje oral

"El jugador nunca espera a una imagen para recibir el resultado del turno. Blob Storage almacena bytes, no verdad del juego. Si el pipeline falla, seguimos mostrando la localización y sus hechos."

## Referencia ampliada

- [Pipeline visual asíncrono](../runtime-flows.md)