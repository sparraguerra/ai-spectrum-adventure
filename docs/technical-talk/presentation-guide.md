# Guion de presentación técnica

## Mensaje rector

**Construimos una aventura conversacional donde la IA amplía la expresividad, mientras un núcleo determinista garantiza coherencia, trazabilidad y control.**

Esta propuesta evita dos extremos: un juego de comandos rígido que no entiende lenguaje natural y una experiencia puramente generativa que puede inventar reglas, objetos o consecuencias.

## Estructura sugerida: 30-40 minutos

| Diapositiva | Tiempo | Contenido | Recurso |
| --- | ---: | --- | --- |
| 1. Título y promesa | 2 min | Aventura conversacional retro con IA responsable. | [Ficha: título y promesa](resources/01-title-and-promise.md) |
| 2. El problema | 3 min | ¿Cómo aceptar lenguaje libre sin perder reglas, puzles y estado fiable? | [Ficha: comando ambiguo](resources/02-ambiguous-command.md) |
| 3. Principio de diseño | 3 min | "La IA crea posibilidades; el motor preserva la realidad". | [Ficha: autoridad del estado](resources/03-state-authority.md) |
| 4. Arquitectura modular | 4 min | Proyectos, dirección de dependencias y por qué monolito modular. | [Ficha: capas modulares](resources/04-modular-architecture.md) |
| 5. Turno de juego | 4 min | Intento -> reglas -> eventos -> estado -> narrativa. | [Ficha: turno determinista](resources/05-game-turn.md) |
| 6. IA acotada | 4 min | Intérprete, narrador, NPC, visual y propuestas de autoría. | [Ficha: responsabilidades IA](resources/06-bounded-ai.md) |
| 7. Mundo dinámico | 4 min | Semilla, `GenerationKey`, restricciones y persistencia antes del enriquecimiento. | [Ficha: exploración dinámica](resources/07-dynamic-world.md) |
| 8. Persistencia y concurrencia | 3 min | `Game`, `World`, borradores, versiones inmutables y transacciones. | [Ficha: modelo y concurrencia](resources/08-persistence-concurrency.md) |
| 9. Experiencia visual | 3 min | Generación asíncrona, caché y degradación elegante. | [Ficha: pipeline visual](resources/09-visual-experience.md) |
| 10. Editor y publicación | 3 min | Autoría humana, validación y versiones. | [Ficha: autoría y publicación](resources/10-authoring-publication.md) |
| 11. Operación | 3 min | Aspire local, Azure Container Apps, observabilidad e identidad. | [Ficha: operación](resources/11-operations.md) |
| 12. Demostración y cierre | 4 min | Ejecutar un turno, explorar, autoría o fallo controlado. | [Ficha: demo y cierre](resources/12-demo-close.md) |

## Narrativa para las diapositivas clave

### El problema

Un LLM puede convertir "quiero usar la llave para abrir la puerta" en una intención razonable, pero no debe decidir que la puerta se abre si la llave no está en el inventario. Si lo hiciera, el juego perdería consistencia y las pruebas dejarían de demostrar el comportamiento.

### La inversión de control

La API de IA no es el centro de la arquitectura. El motor es el centro: valida la intención y produce eventos. La IA trabaja sobre una proyección factual posterior. Esta inversión permite cambiar el proveedor, manejar caídas y probar reglas sin llamadas remotas.

### Monolito modular

No hay microservicios por proyecto. Hay límites de código y contratos explícitos dentro de una aplicación desplegable. El coste operativo se mantiene bajo y la separación permite extraer una pieza cuando existan métricas que lo justifiquen.

### Mundo procedimental

"Procedimental" no significa aleatorio ni generado por un modelo. Una frontera se deriva de una semilla, una versión de generador, la posición y una dirección. La estructura candidata debe superar restricciones y quedar persistida de forma atómica antes de narrarse.

### Resiliencia

La demostración más convincente no es sólo cuando la IA responde bien. Es enseñar que, si la respuesta es inválida o la imagen falla, el jugador sigue avanzando con un resultado coherente y la telemetría conserva el diagnóstico.

## Preguntas previsibles

| Pregunta | Respuesta breve |
| --- | --- |
| ¿Por qué no dejar que el LLM dirija todo el juego? | Porque no ofrece garantías de consistencia, reproducibilidad ni control de costes. Lo usamos para lenguaje y presentación, donde aporta más valor. |
| ¿Por qué no microservicios? | La carga y el equipo no justifican complejidad distribuida. Los puertos y proyectos actuales permiten extraer un worker de imágenes o agentes después. |
| ¿Cómo evitáis alucinaciones? | Contextos mínimos, contratos estructurados, validación y la regla de que las salidas no pueden mutar estado autoritativo. |
| ¿Qué ocurre con dos réplicas explorando la misma zona? | La clave de generación única y las transacciones hacen la operación idempotente; se reutiliza el resultado persistido ganador. |
| ¿Cómo probáis IA? | Se prueban contratos, contexto, límites y fallbacks con dobles de agente; no se requieren llamadas a modelos en CI. |
| ¿Cómo protegéis secretos? | Configuración externa, user-secrets en local e identidad administrada/secretos de Container Apps en Azure. |

## Material visual a preparar para PowerPoint

- Una captura de la pantalla de aventura antes y después de un comando.
- Una captura del editor de aventuras con una propuesta pendiente, una validación y una versión publicada.
- Exportaciones de los cuatro diagramas principales: capas, autoridad del estado, exploración y despliegue.
- Una captura de Aspire Dashboard mostrando `web`, PostgreSQL y Azurite durante el desarrollo local.
- Una consulta o captura de Application Insights con duración y resultado de una acción, sin exponer prompts ni contenido sensible.

## Cierre sugerido

El proyecto no trata de delegar el juego en un modelo. Trata de diseñar una arquitectura donde la IA tiene una función poderosa, observable y reemplazable, y donde las reglas que hacen que un juego sea un juego continúan siendo código determinista.