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

    var asignadosIds = dto.AsignadosIds ?? [];
    foreach (var uid in asignadosIds)
    {
        if (!almacen.ExisteUsuario(uid))
            return Results.BadRequest($"El usuario {uid} no existe.");
    }

    var tarea = new Tarea { Titulo = dto.Titulo, CategoriaId = dto.CategoriaId, AsignadosIds = new List<Guid>(asignadosIds) };
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

    var asignadosIds = dto.AsignadosIds ?? [];
    var uniqueIds = asignadosIds.Distinct().ToList();
    if (uniqueIds.Count != asignadosIds.Count)
        return Results.BadRequest("La lista de asignados contiene identificadores duplicados.");

    foreach (var uid in uniqueIds)
    {
        if (!almacen.ExisteUsuario(uid))
            return Results.BadRequest($"El usuario {uid} no existe.");
    }

    return almacen.Actualizar(id, dto.Titulo, dto.Completada, dto.CategoriaId, uniqueIds)
        ? Results.NoContent()
        : Results.NotFound();
});

// Añadir asignado a una tarea
tareas.MapPost("/{id:guid}/asignados", (Guid id, AgregarAsignadoDto dto, AlmacenTareas almacen) =>
    almacen.AgregarAsignado(id, dto.UsuarioId) switch
    {
        AgregarAsignadoResultado.Ok => Results.NoContent(),
        AgregarAsignadoResultado.TareaNoEncontrada => Results.NotFound("La tarea indicada no existe."),
        AgregarAsignadoResultado.UsuarioNoEncontrado => Results.NotFound("El usuario indicado no existe."),
        AgregarAsignadoResultado.YaAsignado => Results.BadRequest("El usuario ya está asignado a esta tarea."),
        _ => Results.StatusCode(500)
    });

// Quitar asignado de una tarea
tareas.MapDelete("/{id:guid}/asignados/{usuarioId:guid}", (Guid id, Guid usuarioId, AlmacenTareas almacen) =>
    almacen.QuitarAsignado(id, usuarioId) switch
    {
        QuitarAsignadoResultado.Ok => Results.NoContent(),
        QuitarAsignadoResultado.TareaNoEncontrada => Results.NotFound("La tarea indicada no existe."),
        QuitarAsignadoResultado.AsignadoNoEncontrado => Results.NotFound("El usuario no estaba asignado a esta tarea."),
        _ => Results.StatusCode(500)
    });

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
public sealed record CrearTareaDto(string Titulo, Guid? CategoriaId = null, IReadOnlyList<Guid>? AsignadosIds = null);
public sealed record ActualizarTareaDto(string Titulo, bool Completada, Guid? CategoriaId = null, IReadOnlyList<Guid>? AsignadosIds = null);
public sealed record AgregarAsignadoDto(Guid UsuarioId);
public sealed record CrearCategoriaDto(string Nombre);
public sealed record ActualizarCategoriaDto(string Nombre);
public sealed record CrearUsuarioDto(string Nombre, string Email);
public sealed record ActualizarUsuarioDto(string Nombre, string Email);
