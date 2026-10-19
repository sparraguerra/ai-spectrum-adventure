# Recurso 02: Comando ambiguo

## Contenido visual

```text
Jugador: "Quiero usar la llave para abrir la puerta"

Interpretación: UseItem(key, door)
Motor: ¿la llave está en inventario?, ¿la puerta existe?, ¿la regla permite abrirla?
Resultado: evento válido o rechazo coherente
Narrador: explica el resultado, no lo inventa
```

## Mensaje oral

"El modelo puede proponer una intención razonable. No puede decidir que la puerta se abre: el motor comprueba el inventario, la localización y la regla configurada. Si cualquiera falla, no hay cambio de estado."

## Pregunta para la audiencia

¿Qué ocurre si el modelo responde que la puerta se abre pero no existe una llave? El diseño debe impedir que esa frase se convierta en estado.

## Referencias

- [Turno determinista](05-game-turn.md)
- [Flujo de acción normal](../runtime-flows.md)