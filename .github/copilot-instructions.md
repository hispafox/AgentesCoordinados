# Instrucciones del repositorio para GitHub Copilot

API de **lista de tareas** en .NET 10 (Minimal API, almacén en memoria). Esta carpeta `.github/agents/` define un equipo de agentes de Copilot que se coordinan para ampliar la API.

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

## Cómo ampliamos funcionalidades

Toda ampliación entra por el agente **orquestador**, que se encarga del **ciclo completo en GitHub** y delega la parte de código en los especialistas:

1. Crea un **issue** de seguimiento.
2. Crea la rama `feature/<slug>` desde `main`.
3. **analista** → plan en `docs/plan-<slug>.md` (no toca código).
4. **desarrollador** → implementa según el plan y compila.
5. **verificador** → compila, prueba y emite veredicto APROBADO / REVISAR; bucle de corrección hasta 3 iteraciones.
6. Tras APROBADO: **commit** (`closes #<issue>`), **push** y **Pull Request**.

El orquestador gestiona git y GitHub con las herramientas del **MCP de GitHub** si están disponibles, o con el **CLI `gh`** como alternativa; el commit y el push van con `git`. Los subagentes solo tocan su parcela: analista escribe el plan, desarrollador edita código, verificador revisa.

📖 Manual completo con ejemplo reproducible: [`../MANUAL.md`](../MANUAL.md).
