# ListaTareas.Api — guía para Claude Code

API de **lista de tareas** en .NET 10 (Minimal API, almacén en memoria).

## Arquitectura

- Proyecto único: `src/ListaTareas.Api` (target `net10.0`).
- `Tarea.cs`: el modelo `Tarea` y el almacén `AlmacenTareas` (en memoria).
- `Program.cs`: endpoints CRUD agrupados en `/tareas`.
- DTOs de entrada como `record` al final de `Program.cs`.

## Convenciones

- C# moderno: file-scoped namespaces, `record` para DTOs, `nullable` activado.
- Cada endpoint nuevo va en el grupo `/tareas` (`MapGroup`).
- El acceso a datos pasa por `AlmacenTareas`.
- Cada endpoint nuevo se documenta con un ejemplo en `src/ListaTareas.Api/ListaTareas.Api.http`.

## Compilar y ejecutar

```bash
dotnet build ListaTareas.slnx
cd src/ListaTareas.Api && dotnet run
```

API en `http://localhost:5080`. Spec OpenAPI (Development) en `/openapi/v1.json`.

## Equipo de agentes coordinados

Toda ampliación entra por el comando **`/orquestar <funcionalidad>`**, que ejecuta el
**ciclo completo en GitHub** delegando en los subagentes definidos en `.claude/agents/`:

1. Crea un **issue** de seguimiento (`gh issue create`).
2. Crea la rama `feature/<slug>` desde `main`.
3. `analista` — produce el plan en `docs/plan-<slug>.md` (no toca código).
4. `desarrollador` — implementa siguiendo el plan y compila.
5. `verificador` — compila, prueba y emite veredicto APROBADO / REVISAR (no toca código);
   bucle de corrección hasta 3 iteraciones.
6. Tras APROBADO: **commit** (`closes #<issue>`), **push** y **Pull Request** (`gh pr create`).

El orquestador gestiona git/GitHub con `gh`/`git`; los subagentes solo tocan código.

📐 Documentación completa con diagramas: [`docs/ARQUITECTURA-AGENTES.md`](docs/ARQUITECTURA-AGENTES.md).

> Nota: en Claude Code los subagentes no pueden invocar a otros subagentes, por eso el
> orquestador es un **comando** (`.claude/commands/orquestar.md`) que corre en el hilo
> principal, en vez de un subagente. Los originales de Copilot siguen en `.github/agents/`.

> Nota: en Claude Code los subagentes no pueden invocar a otros subagentes, por eso el
> orquestador es un **comando** (`.claude/commands/orquestar.md`) que corre en el hilo
> principal, en vez de un subagente. Los originales de Copilot siguen en `.github/agents/`.
