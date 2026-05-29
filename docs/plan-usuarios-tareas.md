# Plan: usuarios asignados a tareas

## Objetivo
Añadir un CRUD de usuarios (`/usuarios`) y permitir asignar/desasignar un único usuario por tarea, siguiendo el mismo patrón Minimal API ya establecido para categorías, sin romper ningún endpoint existente.

## Endpoints y DTOs afectados

### Nuevos endpoints en `/usuarios`
| Método | Ruta | Body | Códigos HTTP |
|--------|------|------|--------------|
| `GET`    | `/usuarios`        | —                     | `200 OK` |
| `GET`    | `/usuarios/{id}`   | —                     | `200 OK`, `404 Not Found` |
| `POST`   | `/usuarios`        | `CrearUsuarioDto`     | `201 Created`, `400 Bad Request` (nombre vacío), `409 Conflict` (email duplicado) |
| `PUT`    | `/usuarios/{id}`   | `ActualizarUsuarioDto`| `204 No Content`, `400 Bad Request`, `404 Not Found`, `409 Conflict` |
| `DELETE` | `/usuarios/{id}`   | —                     | `204 No Content`, `404 Not Found` |

### Nuevos sub-endpoints en `/tareas/{id}/usuario`
| Método | Ruta | Body | Códigos HTTP |
|--------|------|------|--------------|
| `PUT`    | `/tareas/{id}/usuario` | `AsignarUsuarioDto(Guid UsuarioId)` | `204 No Content`, `404 Not Found` (tarea o usuario) |
| `DELETE` | `/tareas/{id}/usuario` | — | `204 No Content`, `404 Not Found` (tarea) |

### Endpoints existentes en `/tareas` a extender
- `POST /tareas` y `PUT /tareas/{id}`:
  - Extender los DTOs con `Guid? UsuarioId = null` (opcional, backward-compatible).
  - Validación: si `UsuarioId` se envía, debe existir en el almacén; si no, `400 Bad Request`.
- `GET /tareas` y `GET /tareas/{id}`:
  - El campo `UsuarioId` se devuelve en la entidad `Tarea` (nullable, sin romper clientes que no lo consumen).

## Cambios en el modelo

### `Tarea.cs`
1. En la clase `Tarea`: añadir `public Guid? UsuarioId { get; set; }` (nullable, inicializa a `null`).
2. Nueva clase `Usuario` (en `Tarea.cs`, junto a `Categoria`):
   ```csharp
   public sealed class Usuario
   {
       public Guid Id { get; init; } = Guid.NewGuid();
       public required string Nombre { get; set; }
       public required string Email { get; set; }
       public DateTimeOffset CreadoEn { get; init; } = DateTimeOffset.UtcNow;
   }
   ```
3. En `AlmacenTareas`:
   - Añadir `private readonly ConcurrentDictionary<Guid, Usuario> _usuarios = new();`
   - Nuevos métodos: `ListarUsuarios`, `ObtenerUsuario`, `AgregarUsuario`, `ActualizarUsuario`, `BorrarUsuario`, `ExisteUsuario`, `EmailDuplicado`.
   - Extender `Actualizar` para aceptar `Guid? usuarioId`.
   - Nuevo método `AsignarUsuario(Guid tareaId, Guid? usuarioId)` para asignar/desasignar.
   - Al borrar un usuario: desasignar `UsuarioId` en todas las tareas que lo referenciaban (igual que el patrón de `BorrarCategoria`).

### `Program.cs`
- Añadir `var usuarios = app.MapGroup("/usuarios");` con los 5 endpoints CRUD.
- Añadir los dos sub-endpoints de asignación bajo el grupo `tareas`.
- Extender `CrearTareaDto` y `ActualizarTareaDto` con `Guid? UsuarioId = null`.
- Añadir validación de `UsuarioId` en los handlers de crear y actualizar tarea.
- Nuevos DTOs al final del archivo:
  - `CrearUsuarioDto(string Nombre, string Email)`
  - `ActualizarUsuarioDto(string Nombre, string Email)`
  - `AsignarUsuarioDto(Guid UsuarioId)`

### `ListaTareas.Api.http`
- Añadir ejemplos de todos los endpoints de `/usuarios`.
- Añadir ejemplos de `PUT /tareas/{id}/usuario` y `DELETE /tareas/{id}/usuario`.
- Añadir ejemplo de `POST /tareas` con `usuarioId`.

## Pasos de implementación

1. **`Tarea.cs`** — añadir `UsuarioId` en `Tarea`, clase `Usuario`, y ampliar `AlmacenTareas`:
   - Diccionario `_usuarios`.
   - Métodos CRUD de usuarios.
   - Validación de email duplicado (case-insensitive, trimmed).
   - Método `AsignarUsuario`.
   - Lógica de desasignación en cascada en `BorrarUsuario`.
   - Extender `Actualizar` con parámetro `Guid? usuarioId`.

2. **`Program.cs`** — añadir grupo `/usuarios` y endpoints:
   - CRUD completo de usuarios con sus validaciones y códigos HTTP.
   - Sub-endpoints `PUT /tareas/{id}/usuario` y `DELETE /tareas/{id}/usuario`.
   - Extender DTOs `CrearTareaDto` y `ActualizarTareaDto`.
   - Validar `UsuarioId` en handlers existentes de crear/actualizar tarea.

3. **`ListaTareas.Api.http`** — añadir ejemplos de todas las operaciones nuevas.

4. **Verificar compilación** con `dotnet build ListaTareas.slnx`.

5. **Validar end-to-end** ejecutando los escenarios del `.http` contra la API en ejecución.

## Criterios de aceptación

- `POST /usuarios` con datos válidos → `201 Created` con `Location: /usuarios/{id}`.
- `POST /usuarios` con nombre o email vacío → `400 Bad Request`.
- `POST /usuarios` con email ya existente (case-insensitive) → `409 Conflict`.
- `PUT /usuarios/{id}` con email de otro usuario → `409 Conflict`.
- `PUT /usuarios/{id}` inexistente → `404 Not Found`.
- `DELETE /usuarios/{id}` existente → `204 No Content`; las tareas que lo tenían asignado quedan con `UsuarioId = null`.
- `DELETE /usuarios/{id}` inexistente → `404 Not Found`.
- `PUT /tareas/{id}/usuario` con tarea y usuario válidos → `204 No Content`; `GET /tareas/{id}` devuelve `UsuarioId` correcto.
- `PUT /tareas/{id}/usuario` con tarea inexistente → `404 Not Found`.
- `PUT /tareas/{id}/usuario` con usuario inexistente → `404 Not Found`.
- `DELETE /tareas/{id}/usuario` → `204 No Content`; `GET /tareas/{id}` devuelve `UsuarioId: null`.
- `POST /tareas` sin `usuarioId` → `201 Created` (comportamiento previo intacto).
- `POST /tareas` con `usuarioId` válido → `201 Created`; `UsuarioId` en respuesta.
- `POST /tareas` con `usuarioId` inexistente → `400 Bad Request`.
- `PUT /tareas/{id}` con `usuarioId` inexistente → `400 Bad Request`.
- Endpoints actuales de `/tareas` y `/categorias` siguen operativos sin cambios.

## Casos de prueba recomendados

| # | Escenario | Resultado esperado |
|---|-----------|-------------------|
| 1 | Crear usuario con nombre y email válidos | `201` + body con `id` |
| 2 | Crear usuario con email vacío | `400` |
| 3 | Crear usuario con email duplicado (`"a@b.com"` y `"A@B.COM"`) | `409` |
| 4 | Obtener usuario existente | `200` con datos |
| 5 | Obtener usuario inexistente | `404` |
| 6 | Actualizar usuario a email de otro usuario | `409` |
| 7 | Borrar usuario asignado a tareas → verificar tareas | `204`; tareas con `UsuarioId = null` |
| 8 | Asignar usuario válido a tarea válida | `204`; GET tarea refleja cambio |
| 9 | Asignar usuario inexistente a tarea | `404` |
| 10 | Desasignar usuario de tarea | `204`; GET tarea con `UsuarioId = null` |
| 11 | Crear tarea con `usuarioId` válido | `201`; campo presente en respuesta |
| 12 | Crear tarea con `usuarioId` inexistente | `400` |
| 13 | Actualizar tarea sin `usuarioId` | `204`; comportamiento previo sin cambios |
| 14 | Crear/listar categorías tras añadir usuarios | CRUD categorías intacto |

## Riesgos y decisiones abiertas

1. **Validación de formato de email**: el plan propone unicidad por valor exacto (case-insensitive + trimmed), pero no valida que el string sea un email bien formado. Decisión abierta: ¿añadir validación de formato (`Contains('@')` o regex) o dejarlo como responsabilidad del cliente?

2. **Cascada al borrar usuario**: se propone desasignar (`UsuarioId = null`) siguiendo el mismo patrón que `BorrarCategoria`. Alternativa: bloquear el borrado con `409 Conflict` si hay tareas asignadas. Debe confirmarse con el equipo de producto.

3. **Un usuario por tarea (1:1)**: el requisito pide un único usuario asignado. Si en el futuro se necesitan múltiples asignados, el modelo `Guid? UsuarioId` no escala; habría que migrar a una lista. Documentar como limitación deliberada.

4. **Almacenamiento en memoria**: igual que categorías, los datos no persisten entre reinicios. No hay impacto en el plan, pero es una limitación conocida del almacén actual.

5. **Concurrencia en desasignación en cascada**: el bucle de desasignación en `BorrarUsuario` no es atómico respecto a operaciones concurrentes sobre `_tareas`. Al ser un almacén en memoria de demo, se acepta; en producción requeriría una transacción.
