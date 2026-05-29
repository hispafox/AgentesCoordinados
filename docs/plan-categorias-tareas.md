# Plan: categorías de tareas

## Objetivo
Incorporar una funcionalidad mínima para gestionar categorías y relacionarlas con tareas existentes, manteniendo compatibilidad con la API actual de `/tareas`.
La solución debe seguir el estilo Minimal API, usar `AlmacenTareas` en memoria y no romper clientes que ya consumen los endpoints actuales.

## Endpoints y DTOs afectados

### Nuevos endpoints en `/categorias`
- `GET /categorias`:
  - Lista categorías.
  - `200 OK`.
- `GET /categorias/{id}`:
  - Obtiene una categoría por id.
  - `200 OK`, `404 Not Found`.
- `POST /categorias`:
  - Crea categoría.
  - Body: `CrearCategoriaDto(string Nombre)`.
  - `201 Created`, `400 Bad Request` (nombre vacío), `409 Conflict` (nombre duplicado).
- `PUT /categorias/{id}`:
  - Renombra categoría.
  - Body: `ActualizarCategoriaDto(string Nombre)`.
  - `204 No Content`, `400 Bad Request`, `404 Not Found`, `409 Conflict`.
- `DELETE /categorias/{id}`:
  - Elimina categoría.
  - Comportamiento mínimo propuesto: desasignar categoría de tareas relacionadas y luego borrar.
  - `204 No Content`, `404 Not Found`.

### Endpoints existentes en `/tareas` a extender
- `POST /tareas`:
  - Extender `CrearTareaDto` con `Guid? CategoriaId = null`.
  - `201 Created`, `400 Bad Request` si `CategoriaId` no existe.
- `PUT /tareas/{id}`:
  - Extender `ActualizarTareaDto` con `Guid? CategoriaId = null`.
  - `204 No Content`, `404 Not Found`, `400 Bad Request` si `CategoriaId` no existe.
- `GET /tareas` y `GET /tareas/{id}`:
  - Mantener comportamiento actual y devolver también `CategoriaId` en la entidad `Tarea`.

## Cambios en el modelo
- En `Tarea`:
  - Agregar `Guid? CategoriaId { get; set; }` (nullable para compatibilidad).
- Nuevo modelo `Categoria` (en `Tarea.cs` para mantener proyecto simple):
  - `Guid Id` (init, `Guid.NewGuid()`).
  - `string Nombre` (required, mutable para rename).
  - `DateTimeOffset CreadaEn` (init, UTC).
- En `AlmacenTareas`:
  - Agregar estructura en memoria para categorías (`ConcurrentDictionary<Guid, Categoria>`).
  - Agregar métodos CRUD de categorías.
  - Agregar validaciones de existencia de categoría al crear/actualizar tarea.
  - Agregar operación para desasignar una categoría en tareas al eliminarla.
  - Agregar validación de unicidad de nombre de categoría (normalizado y case-insensitive).

## Pasos de implementación
1. Actualizar `src/ListaTareas.Api/Tarea.cs`:
   - Añadir `Categoria` y `CategoriaId` en `Tarea`.
   - Extender `AlmacenTareas` con operaciones de categorías y validaciones relacionadas.
2. Actualizar `src/ListaTareas.Api/Program.cs`:
   - Crear `MapGroup("/categorias")` y mapear endpoints CRUD.
   - Extender DTOs existentes (`CrearTareaDto`, `ActualizarTareaDto`) con `CategoriaId` opcional.
   - Añadir DTOs nuevos (`CrearCategoriaDto`, `ActualizarCategoriaDto`).
   - Ajustar handlers de crear/actualizar tarea para validar categoría y devolver códigos HTTP definidos.
3. Actualizar ejemplos en `src/ListaTareas.Api/ListaTareas.Api.http`:
   - Añadir requests de categorías (listar, crear, obtener, renombrar, borrar).
   - Añadir ejemplos de creación/actualización de tarea con `categoriaId`.
4. Verificar compatibilidad:
   - Confirmar que requests existentes sin `categoriaId` siguen funcionando sin cambios.
   - Confirmar que responses mantienen campos previos y solo agregan campos opcionales.
5. Validar comportamiento end-to-end en ejecución local (`dotnet run`) usando el `.http`.

## Criterios de aceptación
- Existe un grupo `/categorias` con CRUD funcional y códigos HTTP esperados.
- Se puede crear una tarea sin categoría (comportamiento previo intacto).
- Se puede crear/actualizar una tarea con `categoriaId` válido.
- Si `categoriaId` no existe, la API responde `400 Bad Request` en crear/actualizar tarea.
- Al eliminar una categoría, las tareas que la referencian quedan con `CategoriaId = null`.
- No se permiten categorías con nombre vacío ni duplicado por comparación case-insensitive.
- Los endpoints actuales de `/tareas` siguen operativos para clientes previos.

## Riesgos
- Decisión de negocio pendiente: al borrar categoría, desasignar tareas (propuesto) vs bloquear borrado con `409 Conflict` si hay tareas asociadas.
- Al usar almacenamiento en memoria, no hay persistencia ni migraciones reales entre reinicios; la validación solo aplica al estado en ejecución.
- Si no se normaliza bien el nombre (trim + case-insensitive), podrían colarse duplicados semánticos.

## Estrategia de migración en memoria
- No hay migración de datos persistidos porque el almacén es en memoria.
- La "migración" funcional consiste en:
  - Introducir `CategoriaId` como nullable para no romper objetos existentes.
  - Mantener DTOs compatibles (nuevo campo opcional).
  - Conservar rutas actuales y agregar solo comportamiento opt-in para categorías.
- Resultado esperado: despliegue sin ruptura para consumidores actuales; la funcionalidad de categorías se activa gradualmente al enviar `categoriaId` o usar `/categorias`.

## Casos de prueba recomendados
- Crear categoría con nombre válido -> `201` y `Location` correcto.
- Crear categoría con nombre vacío/espacios -> `400`.
- Crear categoría duplicada (`"Trabajo"` y `"trabajo"`) -> `409`.
- Renombrar categoría existente a nombre válido -> `204`.
- Renombrar categoría inexistente -> `404`.
- Crear tarea sin `categoriaId` -> `201`.
- Crear tarea con `categoriaId` válido -> `201` y tarea con categoría asignada.
- Crear tarea con `categoriaId` inexistente -> `400`.
- Actualizar tarea para asignar, cambiar y desasignar categoría (`null`) -> `204`.
- Borrar categoría con tareas asociadas -> `204` y tareas quedan desasignadas.
- Consultar `GET /tareas` y `GET /tareas/{id}` tras operaciones de categoría -> datos coherentes y sin regresiones.
