# Guion de demostración

## Preparación previa

Ejecuta desde la raíz del repositorio:

```powershell
dotnet run --project src/AI.SpectrumAdventure.AppHost
```

Abre el endpoint web y el dashboard de Aspire mostrados en la salida. PostgreSQL y Azurite deben aparecer sanos antes de iniciar la demostración. Para mostrar IA real, configura previamente `AzureAI:Endpoint`, `AzureAI:ChatDeployment` y las credenciales como user-secrets de `AI.SpectrumAdventure.Web`; la demostración funcional sigue siendo válida si se activa el fallback determinista.

No ejecutes `scripts/reset-local-state.ps1` durante la sesión salvo que quieras borrar de forma deliberada el estado local persistido y confirmes el texto `RESET`.

## Recorrido principal: 6 minutos

1. **Selecciona una aventura e inicia una partida.**
   - Mensaje: la partida se crea desde una definición/versionado de aventura y guarda su estado en PostgreSQL.

2. **Ejecuta `look`.**
   - Mensaje: la descripción procede de una escena factual; la narrativa es presentación, no el estado del mundo.

3. **Ejecuta un movimiento disponible, por ejemplo `go north`.**
   - Mensaje: muestra el orden: intención, reglas, eventos, estado y sólo después narración.

4. **Ejecuta una acción imposible, por ejemplo `go south` desde Forest Entrance o usa un objeto no poseído.**
   - Mensaje: la ubicación tiene salidas definidas por la aventura; una dirección no declarada no crea una ruta nueva. No hay mutación y el motor responde de forma determinista.

5. **Habla con un NPC.**
   - Mensaje: el agente recibe personalidad y una frontera de conocimiento, no un acceso global al mundo.

6. **Provoca o espera un cambio de escena.**
   - Mensaje: la respuesta textual llega sin esperar la imagen. Cuando ésta termina, se actualiza de forma progresiva.

## Extensión: exploración dinámica

Usa una partida o escenario preparado que sitúe al jugador en una localización sin salidas declaradas y cuya política permita expandirse. En las ubicaciones con salidas creadas por el autor, una dirección no declarada debe rechazarse en vez de generar contenido.

1. Explica que se calcula una clave con semilla, versión, origen, dirección y ordinal.
2. Muestra que se valida y persiste una nueva estructura antes de pedir enriquecimiento.
3. Repite la acción o reinicia la pantalla: el mundo recuperado es el persistido, no una nueva invención del modelo.

No prometas una dirección concreta sin comprobar primero la aventura y la localización preparadas: la expansión requiere tanto una frontera sin salidas autoritativas como una política que la permita.

## Extensión: editor de aventuras

1. Abre `/authoring/adventure`.
2. Crea o abre un borrador con identificador único.
3. Introduce un error de referencia o salida para mostrar la validación bloqueante.
4. Corrige el borrador y solicita una propuesta de IA opcional.
5. Enseña que la propuesta queda pendiente y que aceptar aplica validación de nuevo.
6. Publica para crear una versión inmutable y abre el historial.

Mensaje: la IA ayuda al autor, pero el autor y el validador determinista conservan el control.

## Plan de contingencia

| Riesgo en directo | Alternativa |
| --- | --- |
| Azure AI no responde | Explica el fallback de narración y continúa con el motor determinista. |
| La imagen tarda o falla | Continúa con la narrativa y señala que el worker marca el asset como fallido sin detener la partida. |
| La base de datos local no arranca | Muestra los diagramas y una grabación/captura preparada; no sustituyas PostgreSQL por estado en memoria. |
| Un borrador ya existe | Usa un identificador nuevo con sufijo de fecha/hora. |
| No hay red | Haz la parte de juego, reglas, editor y Aspire local; reserva la narración generativa para la explicación. |

## Checklist de cierre

- La diapositiva de arquitectura y la demo transmiten el mismo límite de autoridad.
- Los secretos no aparecen en terminal, configuración, capturas ni diapositivas.
- Se explica que los recursos generativos son opcionales para el camino crítico.
- La última acción de la demo muestra una respuesta concreta, no una pantalla de carga.