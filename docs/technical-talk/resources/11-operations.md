# Recurso 11: Operación

## Topología

```mermaid
flowchart LR
    Dev[Desarrollo local] --> Aspire[.NET Aspire AppHost]
    Aspire --> Web[Aplicación web]
    Aspire --> Pg[(PostgreSQL)]
    Aspire --> Azurite[Azurite]
    Web --> Azure[Azure Container Apps]
    Azure --> AzurePg[(PostgreSQL Flexible Server)]
    Azure --> Storage[Blob Storage]
    Azure --> Insights[Application Insights]
    Azure --> Foundry[AI Foundry opcional]
```

## Mensaje oral

"Aspire reproduce localmente las dependencias que importan y pasa configuración a la aplicación. En Azure desplegamos una sola aplicación web con identidad administrada, almacenamiento y telemetría. La observabilidad registra resultados y duración, no prompts ni cuerpos de borrador."

## Referencias

- [Despliegue](../architecture.md)
- [Guion de demostración](../demo-script.md)