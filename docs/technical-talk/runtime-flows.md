# Flujos de ejecución

## 1. Acción normal del jugador

La UI no entrega el texto directamente a un modelo narrativo. Primero lo interpreta como una intención estructurada y el motor comprueba esa intención contra el estado actual.

```mermaid
sequenceDiagram
    actor Player as Jugador
    participant UI as Blazor UI
    participant Intent as IIntentInterpreter
    participant Orchestrator as AdventureOrchestrator
    participant Rules as RulesEngine
    participant Game as Game
    participant Narrator as INarratorAgent
    participant Repo as IGameRepository

    Player->>UI: "ve al norte"
    UI->>Intent: InterpretAsync(texto)
    Intent-->>UI: ParsedIntent(Go, North)
    UI->>Orchestrator: ProcessActionWithNarrationAsync
    Orchestrator->>Rules: Validate(intent, game)
    alt La acción es inválida
        Rules-->>Orchestrator: razón de rechazo
        Orchestrator-->>UI: ActionResult con feedback determinista
    else La acción es válida
        Rules-->>Orchestrator: eventos propuestos
        Orchestrator->>Game: ApplyRange(eventos)
        Orchestrator->>Narrator: NarrateAsync(contexto factual)
        alt IA responde con contrato válido
            Narrator-->>Orchestrator: NarrationResult
        else IA falla o la salida es inválida
            Narrator-->>Orchestrator: fallback determinista
        end
        Orchestrator-->>UI: ActionResult con narrativa
        UI->>Repo: Guardar partida
    end
```

La cualidad esencial del orden es que, cuando el narrador recibe contexto, los cambios ya fueron validados y aplicados. Puede describirlos, pero no decidirlos ni revertirlos.

## 2. Exploración de una frontera desconocida

La exploración procedimental no pide a un modelo que invente un lugar y lo haga verdadero. Genera estructura de forma determinista, valida restricciones, persiste y sólo entonces permite el enriquecimiento narrativo.

```mermaid
sequenceDiagram
    actor Player as Jugador
    participant UI as Blazor UI
    participant UseCase as ExploreUnknownDirectionUseCase
    participant Gen as Generadores deterministas
    participant Validator as WorldConstraintValidator
    participant Repo as IWorldRepository
    participant World as World y Game
    participant Enrich as WorldEnrichmentService

    Player->>UI: "ve al este"
    UI->>UseCase: ExecuteAsync(game, East)
    UseCase->>Repo: Cargar World persistido
    UseCase->>Gen: Derivar GenerationKey(seed, versión, origen, dirección, ordinal)
    Gen-->>UseCase: región, localización, conexión y metadatos
    UseCase->>Validator: Validate(world, candidato)
    alt Restricción incumplida
        Validator-->>UseCase: error
        UseCase-->>UI: No se modifica el mundo
    else Candidato válido
        UseCase->>World: Aplicar WorldExpandedEvent y movimiento
        UseCase->>Repo: PersistExpansionAsync con clave única
        Repo-->>UseCase: expansión ganadora y persistida
        UseCase->>Repo: Guardar World y Game en transacción
        opt Enriquecimiento opcional
            UseCase->>Enrich: EnrichAsync(proyección de sólo lectura)
            Enrich-->>UseCase: metadatos de presentación o fallo aislado
        end
        UseCase-->>UI: Descripción factual de destino
    end
```

La `GenerationKey` hace idempotente la expansión. Ante competencia, la restricción única de base de datos sustituye un lock local que no funcionaría con varias réplicas.

## 3. Canalización visual asíncrona

La imagen no está en el camino crítico de una acción. Sólo se solicita después de detectar un cambio de escena y se procesa en segundo plano con caché por `SceneStateKey`.

```mermaid
sequenceDiagram
    participant UI as Blazor UI
    participant Director as Visual Art Director
    participant Queue as ImageGenerationQueue
    participant Worker as ImageGenerationWorker
    participant Image as IImageGenerator
    participant Retro as IRetroImageProcessor
    participant Blob as Blob Storage
    participant Assets as VisualAssetRepository

    UI->>Director: Crear especificación desde escena validada
    Director-->>UI: VisualSceneSpec
    UI->>Queue: Encolar solicitud
    UI-->>UI: Mostrar narrativa inmediatamente
    Queue->>Worker: Consumir solicitud
    Worker->>Assets: Buscar por SceneStateKey
    alt Recurso listo en caché
        Worker-->>UI: Notificar URL existente
    else Sin caché
        Worker->>Image: Generar imagen
        Worker->>Retro: Aplicar procesamiento retro
        Worker->>Blob: Subir bytes
        Worker->>Assets: Guardar estado Ready y URL
        Worker-->>UI: Notificar actualización de escena
    else Error
        Worker->>Assets: Guardar estado Failed
        Worker-->>UI: Notificar sin interrumpir juego
    end
```

El trabajador registra la excepción y marca el recurso como fallido. No propaga el error al turno del jugador.

## 4. Conversación con NPC

El NPC recibe un subconjunto controlado del mundo: personalidad, objetivos, relaciones, memoria limitada y una lista de conocimiento permitido. Incluso después de la respuesta, el orquestador filtra las claves reveladas frente a la lista del dominio.

```mermaid
flowchart LR
    U[Texto del jugador] --> I[Intent: TalkTo]
    I --> Presence{NPC presente?}
    Presence -->|No| Reject[Respuesta determinista]
    Presence -->|Sí| Context[Contexto acotado\npersonalidad + KnowledgeBoundary]
    Context --> Agent[Agente NPC]
    Agent --> Response[NpcResponse]
    Response --> Filter[Filtrar claves reveladas\ncontra whitelist del dominio]
    Filter --> Memory[Actualizar memoria\ny conocimiento permitido]
    Memory --> Result[ActionResult]
```

## 5. Ciclo de autoría

La autoría tiene el mismo principio de autoridad que el juego: una propuesta de IA es un artefacto revisable, no una escritura de datos.

```mermaid
sequenceDiagram
    actor Author as Autor
    participant Editor as Editor Blazor
    participant Draft as DraftUseCases
    participant Validator as AdventureAuthoringValidator
    participant Agent as AuthoringProposalAgent
    participant Db as PostgreSQL

    Author->>Editor: Editar definición JSON
    Editor->>Validator: Validar estructura y referencias
    Validator-->>Editor: Problemas o resultado
    Editor->>Draft: Guardar con revisión esperada
    Draft->>Db: Actualizar borrador con concurrencia optimista

    Author->>Editor: Solicitar propuesta opcional
    Editor->>Agent: Contexto de borrador acotado
    Agent-->>Editor: AuthoringProposal pendiente
    Author->>Editor: Revisar, editar y aceptar
    Editor->>Validator: Validar parche aceptado
    Validator->>Db: Aplicar cambio sólo si es válido

    Author->>Editor: Publicar
    Editor->>Validator: Validación completa
    Validator->>Db: Crear AdventureVersion inmutable
```

Una partida se vincula con la versión publicada de la que nació. Restaurar una versión crea un borrador nuevo; nunca modifica el snapshot histórico.