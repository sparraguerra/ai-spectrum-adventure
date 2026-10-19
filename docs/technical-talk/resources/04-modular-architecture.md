# Recurso 04: Capas modulares

## Diagrama

```mermaid
flowchart TB
    Web[Web: Blazor y endpoints] --> Application[Application: casos de uso]
    Application --> Domain[Domain: reglas y entidades]
    Infrastructure[Infrastructure: EF Core y almacenamiento] --> Application
    Agents[Agents: adaptadores de IA] --> Application
    AppHost[AppHost: Aspire local] --> Web
```

## Mensaje oral

"No hemos convertido cada carpeta en un microservicio. Son límites de código dentro de un despliegue. El dominio no depende de EF Core, Azure ni del proveedor de modelos; por eso podemos probar reglas y sustituir adaptadores sin mover la lógica central."

## Referencia ampliada

- [Arquitectura modular](../architecture.md)