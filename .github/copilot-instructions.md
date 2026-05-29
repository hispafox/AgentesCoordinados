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

Toda ampliación entra por el agente **orquestador**, que delega en **analista → desarrollador → verificador**.
