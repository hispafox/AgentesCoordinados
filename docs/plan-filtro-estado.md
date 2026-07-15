# Plan — Filtrar tareas por estado

## Objetivo
Permitir listar las tareas filtrando por si están completadas o pendientes, mediante un
nuevo endpoint de solo lectura. Cierra el issue #1.

## Endpoints y DTOs afectados
- **Nuevo:** `GET /tareas/estado/{completada:bool}` — `{completada}` es `true` o `false`.
  Devuelve `200 OK` con la lista de tareas cuyo `Completada` coincide. No requiere DTO.
- No se modifican endpoints existentes ni DTOs.

## Cambios en el modelo
- Ninguno en `Tarea`.
- En `AlmacenTareas`: nuevo método `ListarPorEstado(bool completada)` que filtra sobre
  la colección existente y mantiene el orden actual (más recientes primero).

## Pasos de implementación
1. En `Tarea.cs`, añadir a `AlmacenTareas` el método `ListarPorEstado(bool completada)`
   reutilizando el mismo orden que `Listar()` (`OrderByDescending(t => t.CreadaEn)`).
2. En `Program.cs`, registrar el endpoint en el grupo `tareas` (`MapGet`), antes o
   después de los existentes, con la restricción de ruta `:bool`.
3. Añadir dos ejemplos al `ListaTareas.Api.http` (filtrar completadas y pendientes).

## Criterios de aceptación
1. `GET /tareas/estado/true` devuelve solo tareas con `Completada == true`.
2. `GET /tareas/estado/false` devuelve solo tareas con `Completada == false`.
3. El orden es el mismo que en `GET /tareas` (más recientes primero).
4. El acceso a datos pasa por `AlmacenTareas` (no se filtra en el endpoint).
5. El `.http` incluye un ejemplo por cada caso.

## Riesgos
- Bajo. Es una consulta de solo lectura sobre una colección en memoria.
- La restricción `:bool` rechaza valores que no sean `true`/`false` con 404 de ruta;
  es el comportamiento esperado y no necesita validación extra.
