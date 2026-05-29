# Lista de Tareas — API (.NET 10)

API mínima de lista de tareas (CRUD) sobre ASP.NET Core, en memoria.

## Arrancar

```bash
cd src/ListaTareas.Api
dotnet run
```

Queda en `http://localhost:5080`. Prueba los endpoints desde `ListaTareas.Api.http`.

## Endpoints

| Método | Ruta | Qué hace |
|--------|------|----------|
| GET | `/tareas` | Lista todas las tareas |
| GET | `/tareas/{id}` | Una tarea |
| POST | `/tareas` | Crea una tarea `{ "titulo": "..." }` |
| PUT | `/tareas/{id}` | Actualiza título y estado |
| POST | `/tareas/{id}/completar` | Marca como completada |
| DELETE | `/tareas/{id}` | Borra |

En Development, la spec OpenAPI está en `/openapi/v1.json`.

## Estructura

```
ListaTareas/
├── ListaTareas.slnx
├── README.md
└── src/ListaTareas.Api/
    ├── ListaTareas.Api.csproj
    ├── Program.cs        endpoints CRUD
    ├── Tarea.cs          modelo + almacén en memoria
    └── ListaTareas.Api.http
```
