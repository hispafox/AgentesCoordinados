# Plan: asignados-multiples

## Objetivo

Permitir que una tarea tenga varios responsables simultáneos, reemplazando el campo escalar `UsuarioId` por una colección de identificadores de usuario, y exponiendo endpoints dedicados para añadir y quitar asignados de forma individual.

---

## Endpoints y DTOs afectados

### Endpoints modificados

| Método | Ruta | Cambio |
|--------|------|--------|
| `POST` | `/tareas` | El DTO acepta `AsignadosIds` (lista) en lugar de `UsuarioId` (escalar). |
| `PUT` | `/tareas/{id}` | El DTO acepta `AsignadosIds` (lista) en lugar de `UsuarioId` (escalar). |

### Endpoints eliminados

| Método | Ruta | Motivo |
|--------|------|--------|
| `PUT` | `/tareas/{id}/usuario` | Sustituido por los dos endpoints nuevos de colección. |
| `DELETE` | `/tareas/{id}/usuario` | Sustituido por el endpoint de quitar asignado individual. |

### Endpoints nuevos

| Método | Ruta | Descripción | Respuestas |
|--------|------|-------------|------------|
| `POST` | `/tareas/{id}/asignados` | Añade un usuario a la lista de asignados. Body: `AgregarAsignadoDto`. | `204 No Content`, `400 Bad Request` (usuario ya asignado), `404 Not Found` (tarea o usuario inexistente). |
| `DELETE` | `/tareas/{id}/asignados/{usuarioId}` | Quita un usuario de la lista de asignados. Sin body. | `204 No Content`, `404 Not Found` (tarea inexistente o usuario no estaba asignado). |

### DTOs nuevos y modificados (al final de `Program.cs`)

```csharp
// Reemplaza CrearTareaDto
public sealed record CrearTareaDto(
    string Titulo,
    Guid? CategoriaId = null,
    IReadOnlyList<Guid>? AsignadosIds = null);

// Reemplaza ActualizarTareaDto
public sealed record ActualizarTareaDto(
    string Titulo,
    bool Completada,
    Guid? CategoriaId = null,
    IReadOnlyList<Guid>? AsignadosIds = null);

// Nuevo
public sealed record AgregarAsignadoDto(Guid UsuarioId);
```

`AsignarUsuarioDto` puede eliminarse al quedar sin uso.

---

## Cambios en el modelo

### `Tarea` (`Tarea.cs`)

- Eliminar `public Guid? UsuarioId { get; set; }`.
- Añadir `public List<Guid> AsignadosIds { get; set; } = [];`.  
  Se usa `List<Guid>` (no `ImmutableList`) porque los métodos del almacén mutan la colección in situ, igual que hacen `Titulo` y `Completada`.

### `AlmacenTareas` (`Tarea.cs`)

| Miembro | Acción | Detalle |
|---------|--------|---------|
| `Actualizar(…, Guid? usuarioId)` | Modificar firma | Cambiar el parámetro `Guid? usuarioId` por `IReadOnlyList<Guid> asignadosIds`. Dentro del método, validar que todos los ids existen (`ExisteUsuario`) y asignar `tarea.AsignadosIds = new List<Guid>(asignadosIds)`. |
| `AsignarUsuario(Guid tareaId, Guid? usuarioId)` | Eliminar | Ya no tiene sentido con la colección. |
| `AgregarAsignado(Guid tareaId, Guid usuarioId)` | Nuevo | Verifica que la tarea existe, que el usuario existe y que no está ya en la lista; si todo es correcto, añade el id y devuelve `AgregarAsignadoResultado` (enum o tipo resultado para diferenciar los tres fallos). |
| `QuitarAsignado(Guid tareaId, Guid usuarioId)` | Nuevo | Verifica que la tarea existe y que el usuario estaba en la lista; lo elimina y devuelve `QuitarAsignadoResultado`. |
| `BorrarUsuario(Guid id)` | Modificar | Reemplazar `tarea.UsuarioId = null` por `tarea.AsignadosIds.Remove(id)` para mantener la coherencia referencial al borrar un usuario. |
| `Agregar(Tarea tarea)` | Sin cambio de firma | La tarea ya llega construida con la colección inicializada. |

Para los nuevos métodos conviene usar un pequeño enum de resultado (o un tipo suma) en lugar de `bool`, de modo que el endpoint pueda devolver `404` vs `400` sin ambigüedad:

```csharp
public enum AgregarAsignadoResultado { Ok, TareaNoEncontrada, UsuarioNoEncontrado, YaAsignado }
public enum QuitarAsignadoResultado { Ok, TareaNoEncontrada, AsignadoNoEncontrado }
```

---

## Pasos de implementación

1. **Modelo `Tarea`** — Eliminar `UsuarioId`, añadir `AsignadosIds`.
2. **Enums de resultado** — Declarar `AgregarAsignadoResultado` y `QuitarAsignadoResultado` en `Tarea.cs`.
3. **`AlmacenTareas.BorrarUsuario`** — Cambiar la limpieza de `UsuarioId` por `AsignadosIds.Remove(id)`.
4. **`AlmacenTareas.Actualizar`** — Cambiar parámetro `Guid? usuarioId` por `IReadOnlyList<Guid> asignadosIds`; validar existencia de cada id dentro del método; asignar la lista.
5. **`AlmacenTareas.AsignarUsuario`** — Eliminar el método.
6. **`AlmacenTareas.AgregarAsignado`** — Implementar con validaciones y retorno de `AgregarAsignadoResultado`.
7. **`AlmacenTareas.QuitarAsignado`** — Implementar con validaciones y retorno de `QuitarAsignadoResultado`.
8. **DTOs en `Program.cs`** — Sustituir `CrearTareaDto`, `ActualizarTareaDto`; añadir `AgregarAsignadoDto`; eliminar `AsignarUsuarioDto`.
9. **Endpoint `POST /tareas`** — Validar lista `AsignadosIds`: para cada id comprobar `ExisteUsuario`; construir `Tarea` con `AsignadosIds` inicializada.
10. **Endpoint `PUT /tareas/{id}`** — Misma validación de lista; llamar a `Actualizar` con la nueva firma.
11. **Eliminar endpoints** `PUT /tareas/{id}/usuario` y `DELETE /tareas/{id}/usuario`.
12. **Endpoint `POST /tareas/{id}/asignados`** — Llamar a `AgregarAsignado`; mapear enum a código HTTP.
13. **Endpoint `DELETE /tareas/{id}/asignados/{usuarioId}`** — Llamar a `QuitarAsignado`; mapear enum a código HTTP.
14. **`ListaTareas.Api.http`** — Añadir ejemplos para los cuatro casos (crear con lista, actualizar con lista, añadir asignado, quitar asignado).
15. **Compilar** — `dotnet build ListaTareas.slnx` sin errores ni advertencias.

---

## Criterios de aceptación

1. `GET /tareas/{id}` devuelve el campo `asignadosIds` como array JSON (vacío `[]` si no hay asignados).
2. `POST /tareas` con `"asignadosIds": ["<guid-valido>"]` devuelve `201` y el array tiene un elemento.
3. `POST /tareas` con `"asignadosIds": ["<guid-inexistente>"]` devuelve `400`.
4. `POST /tareas` sin `asignadosIds` (omitido o `null`) devuelve `201` con `asignadosIds: []`.
5. `POST /tareas/{id}/asignados` con un usuario válido no asignado aún devuelve `204`.
6. `POST /tareas/{id}/asignados` con un usuario ya asignado devuelve `400`.
7. `POST /tareas/{id}/asignados` con tarea inexistente devuelve `404`.
8. `POST /tareas/{id}/asignados` con usuario inexistente devuelve `404`.
9. `DELETE /tareas/{id}/asignados/{usuarioId}` quita el usuario y devuelve `204`.
10. `DELETE /tareas/{id}/asignados/{usuarioId}` cuando el usuario no estaba asignado devuelve `404`.
11. Borrar un usuario (`DELETE /usuarios/{id}`) lo elimina del array `asignadosIds` de todas las tareas que lo contenían.
12. `PUT /tareas/{id}` con `"asignadosIds": []` deja la tarea sin asignados (`204`).
13. `PUT /tareas/{id}` con ids duplicados en la lista devuelve `400`.
14. El proyecto compila sin errores con `dotnet build ListaTareas.slnx`.
15. Los endpoints `PUT /tareas/{id}/usuario` y `DELETE /tareas/{id}/usuario` ya no existen (devuelven `404` de enrutamiento).

---

## Riesgos

| Riesgo | Probabilidad | Mitigacion |
|--------|--------------|------------|
| Concurrencia: `List<Guid>` no es thread-safe y `ConcurrentDictionary` no protege las mutaciones internas de la lista. | Media | Usar `lock` sobre la instancia de `Tarea` dentro de `AgregarAsignado` y `QuitarAsignado`, o bien reemplazar la lista por `ConcurrentBag<Guid>` / `ImmutableList` con intercambio atómico via `Interlocked.CompareExchange`. Dado que es un almacén en memoria de uso educativo, un simple `lock (tarea)` es suficiente. |
| Ids duplicados en el body de `PUT /tareas/{id}` (lista con el mismo guid repetido). | Baja | Deduplicar con `Distinct()` antes de persistir, o devolver `400` indicando duplicados. El plan opta por `400` para hacerlo explícito (criterio 13). |
| Rotura de contratos HTTP existentes: clientes que usaban `usuarioId` en `POST`/`PUT` o los endpoints `/usuario`. | Alta (si hay clientes) | Los DTOs eliminan `UsuarioId`; los endpoints `/usuario` desaparecen. Documentar el breaking change en el PR. Al ser un almacén en memoria sin persistencia externa, el impacto es acotado. |
| `AsignadosIds` no inicializado en rutas de código que construyen `Tarea` directamente. | Baja | El inicializador `= []` en la declaración de la propiedad garantiza que nunca sea `null`. |
