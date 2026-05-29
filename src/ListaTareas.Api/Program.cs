using ListaTareas.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<AlmacenTareas>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // spec en /openapi/v1.json
}

var tareas = app.MapGroup("/tareas");
var categorias = app.MapGroup("/categorias");
var usuarios = app.MapGroup("/usuarios");

// Listar todas
tareas.MapGet("/", (AlmacenTareas almacen) => Results.Ok(almacen.Listar()));

// Listar por estado (completadas / pendientes)
tareas.MapGet("/estado/{completada:bool}", (bool completada, AlmacenTareas almacen) =>
    Results.Ok(almacen.ListarPorEstado(completada)));

// Obtener una
tareas.MapGet("/{id:guid}", (Guid id, AlmacenTareas almacen) =>
    almacen.Obtener(id) is { } tarea ? Results.Ok(tarea) : Results.NotFound());

// Crear
tareas.MapPost("/", (CrearTareaDto dto, AlmacenTareas almacen) =>
{
    if (string.IsNullOrWhiteSpace(dto.Titulo))
        return Results.BadRequest("El título es obligatorio.");

    if (dto.CategoriaId is Guid categoriaId && !almacen.ExisteCategoria(categoriaId))
        return Results.BadRequest("La categoría indicada no existe.");

    if (dto.UsuarioId is Guid usuarioId && !almacen.ExisteUsuario(usuarioId))
        return Results.BadRequest("El usuario indicado no existe.");

    var tarea = new Tarea { Titulo = dto.Titulo, CategoriaId = dto.CategoriaId, UsuarioId = dto.UsuarioId };
    almacen.Agregar(tarea);
    return Results.Created($"/tareas/{tarea.Id}", tarea);
});

// Actualizar (título y estado)
tareas.MapPut("/{id:guid}", (Guid id, ActualizarTareaDto dto, AlmacenTareas almacen) =>
{
    if (string.IsNullOrWhiteSpace(dto.Titulo))
        return Results.BadRequest("El título es obligatorio.");

    if (dto.CategoriaId is Guid categoriaId && !almacen.ExisteCategoria(categoriaId))
        return Results.BadRequest("La categoría indicada no existe.");

    if (dto.UsuarioId is Guid usuarioId && !almacen.ExisteUsuario(usuarioId))
        return Results.BadRequest("El usuario indicado no existe.");

    return almacen.Actualizar(id, dto.Titulo, dto.Completada, dto.CategoriaId, dto.UsuarioId)
        ? Results.NoContent()
        : Results.NotFound();
});

// Asignar usuario a tarea
tareas.MapPut("/{id:guid}/usuario", (Guid id, AsignarUsuarioDto dto, AlmacenTareas almacen) =>
{
    if (almacen.Obtener(id) is null)
        return Results.NotFound("La tarea indicada no existe.");

    if (!almacen.ExisteUsuario(dto.UsuarioId))
        return Results.NotFound("El usuario indicado no existe.");

    _ = almacen.AsignarUsuario(id, dto.UsuarioId);
    return Results.NoContent();
});

// Desasignar usuario de tarea
tareas.MapDelete("/{id:guid}/usuario", (Guid id, AlmacenTareas almacen) =>
    almacen.AsignarUsuario(id, null) ? Results.NoContent() : Results.NotFound());

// Marcar como completada
tareas.MapPost("/{id:guid}/completar", (Guid id, AlmacenTareas almacen) =>
    almacen.Completar(id) ? Results.NoContent() : Results.NotFound());

// Borrar
tareas.MapDelete("/{id:guid}", (Guid id, AlmacenTareas almacen) =>
    almacen.Borrar(id) ? Results.NoContent() : Results.NotFound());

// Listar categorias
categorias.MapGet("/", (AlmacenTareas almacen) => Results.Ok(almacen.ListarCategorias()));

// Obtener una categoria
categorias.MapGet("/{id:guid}", (Guid id, AlmacenTareas almacen) =>
    almacen.ObtenerCategoria(id) is { } categoria ? Results.Ok(categoria) : Results.NotFound());

// Crear categoria
categorias.MapPost("/", (CrearCategoriaDto dto, AlmacenTareas almacen) =>
{
    if (string.IsNullOrWhiteSpace(dto.Nombre))
        return Results.BadRequest("El nombre es obligatorio.");

    var categoria = new Categoria { Nombre = dto.Nombre.Trim() };
    if (!almacen.AgregarCategoria(categoria))
        return Results.Conflict("Ya existe una categoría con ese nombre.");

    return Results.Created($"/categorias/{categoria.Id}", categoria);
});

// Renombrar categoria
categorias.MapPut("/{id:guid}", (Guid id, ActualizarCategoriaDto dto, AlmacenTareas almacen) =>
{
    if (string.IsNullOrWhiteSpace(dto.Nombre))
        return Results.BadRequest("El nombre es obligatorio.");

    if (almacen.ObtenerCategoria(id) is null)
        return Results.NotFound();

    if (almacen.CategoriaDuplicada(dto.Nombre, id))
        return Results.Conflict("Ya existe una categoría con ese nombre.");

    _ = almacen.ActualizarCategoria(id, dto.Nombre.Trim());
    return Results.NoContent();
});

// Borrar categoria (desasigna tareas relacionadas)
categorias.MapDelete("/{id:guid}", (Guid id, AlmacenTareas almacen) =>
    almacen.BorrarCategoria(id) ? Results.NoContent() : Results.NotFound());

// Listar usuarios
usuarios.MapGet("/", (AlmacenTareas almacen) => Results.Ok(almacen.ListarUsuarios()));

// Obtener un usuario
usuarios.MapGet("/{id:guid}", (Guid id, AlmacenTareas almacen) =>
    almacen.ObtenerUsuario(id) is { } usuario ? Results.Ok(usuario) : Results.NotFound());

// Crear usuario
usuarios.MapPost("/", (CrearUsuarioDto dto, AlmacenTareas almacen) =>
{
    if (string.IsNullOrWhiteSpace(dto.Nombre))
        return Results.BadRequest("El nombre es obligatorio.");

    if (string.IsNullOrWhiteSpace(dto.Email))
        return Results.BadRequest("El email es obligatorio.");

    var usuario = new Usuario { Nombre = dto.Nombre.Trim(), Email = dto.Email.Trim() };
    if (!almacen.AgregarUsuario(usuario))
        return Results.Conflict("Ya existe un usuario con ese email.");

    return Results.Created($"/usuarios/{usuario.Id}", usuario);
});

// Actualizar usuario
usuarios.MapPut("/{id:guid}", (Guid id, ActualizarUsuarioDto dto, AlmacenTareas almacen) =>
{
    if (string.IsNullOrWhiteSpace(dto.Nombre))
        return Results.BadRequest("El nombre es obligatorio.");

    if (string.IsNullOrWhiteSpace(dto.Email))
        return Results.BadRequest("El email es obligatorio.");

    if (almacen.ObtenerUsuario(id) is null)
        return Results.NotFound();

    if (almacen.EmailDuplicado(dto.Email, id))
        return Results.Conflict("Ya existe un usuario con ese email.");

    _ = almacen.ActualizarUsuario(id, dto.Nombre.Trim(), dto.Email.Trim());
    return Results.NoContent();
});

// Borrar usuario (desasigna tareas relacionadas)
usuarios.MapDelete("/{id:guid}", (Guid id, AlmacenTareas almacen) =>
    almacen.BorrarUsuario(id) ? Results.NoContent() : Results.NotFound());

app.Run();

// DTOs de entrada
public sealed record CrearTareaDto(string Titulo, Guid? CategoriaId = null, Guid? UsuarioId = null);
public sealed record ActualizarTareaDto(string Titulo, bool Completada, Guid? CategoriaId = null, Guid? UsuarioId = null);
public sealed record CrearCategoriaDto(string Nombre);
public sealed record ActualizarCategoriaDto(string Nombre);
public sealed record CrearUsuarioDto(string Nombre, string Email);
public sealed record ActualizarUsuarioDto(string Nombre, string Email);
public sealed record AsignarUsuarioDto(Guid UsuarioId);
