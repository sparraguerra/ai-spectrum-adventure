# Documentación para la charla técnica

Este directorio reúne el material técnico de apoyo para presentar AI Spectrum Adventure. Está pensado para que la presentación PowerPoint se guarde aquí mismo y enlace, cuando convenga, a los diagramas y explicaciones fuente.

## Punto de partida

AI Spectrum Adventure es una aventura conversacional inspirada en los juegos de 8 bits. Su idea arquitectónica central es:

> La IA crea posibilidades expresivas; el motor del juego preserva la realidad.

El estado del juego, la topología del mundo, los puzles, el inventario, la publicación de aventuras y las decisiones de autoría son deterministas. Los modelos de IA interpretan lenguaje natural y enriquecen la experiencia, pero no poseen ni modifican el estado autoritativo.

## Material disponible

| Documento | Uso durante la charla |
| --- | --- |
| [architecture.md](architecture.md) | Arquitectura, módulos, despliegue, seguridad y observabilidad. |
| [runtime-flows.md](runtime-flows.md) | Secuencias de juego, exploración procedimental, imágenes y autoría. |
| [presentation-guide.md](presentation-guide.md) | Propuesta de estructura para las diapositivas y mensajes clave. |
| [demo-script.md](demo-script.md) | Guion reproducible para una demostración en directo. |
| [../adventures/adventure-authoring-guide.md](../adventures/adventure-authoring-guide.md) | Manual funcional de autoría para profundizar durante preguntas. |

## Lectura recomendada

1. Empieza por [presentation-guide.md](presentation-guide.md) para construir las diapositivas.
2. Copia los bloques Mermaid desde [architecture.md](architecture.md) y [runtime-flows.md](runtime-flows.md) a la herramienta de diagramación o expórtalos como SVG/PNG.
3. Ensaya el recorrido de [demo-script.md](demo-script.md) con Aspire levantado localmente.
4. Conserva estos documentos como fuente viva: deben actualizarse cuando cambien límites, flujos o infraestructura.

## Alcance verificado

La documentación describe la implementación actual: aplicación Blazor Server en .NET 10, monolito modular, PostgreSQL, Blob Storage, Azure AI/OpenAI mediante Microsoft Agent Framework, OpenTelemetry y Aspire para el entorno local. Los detalles proceden del código de composición, los casos de uso y la infraestructura Bicep; no son una arquitectura aspiracional.