using System.Collections.Concurrent;

namespace ListaTareas.Api;

/// <summary>Una tarea de la lista.</summary>
public sealed class Tarea
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Titulo { get; set; }
    public bool Completada { get; set; }
    public Guid? CategoriaId { get; set; }
    public Guid? UsuarioId { get; set; }
    public DateTimeOffset CreadaEn { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>Una categoria de tareas.</summary>
public sealed class Categoria
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Nombre { get; set; }
    public DateTimeOffset CreadaEn { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>Un usuario asignable a tareas.</summary>
public sealed class Usuario
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Nombre { get; set; }
    public required string Email { get; set; }
    public DateTimeOffset CreadoEn { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>Almacén de tareas en memoria.</summary>
public sealed class AlmacenTareas
{
    private readonly ConcurrentDictionary<Guid, Tarea> _tareas = new();
    private readonly ConcurrentDictionary<Guid, Categoria> _categorias = new();
    private readonly ConcurrentDictionary<Guid, Usuario> _usuarios = new();

    public IReadOnlyCollection<Tarea> Listar() =>
        _tareas.Values.OrderByDescending(t => t.CreadaEn).ToList();

    public Tarea? Obtener(Guid id) => _tareas.GetValueOrDefault(id);

    public bool ExisteCategoria(Guid id) => _categorias.ContainsKey(id);

    public bool ExisteUsuario(Guid id) => _usuarios.ContainsKey(id);

    public void Agregar(Tarea tarea) => _tareas[tarea.Id] = tarea;

    public bool Actualizar(Guid id, string titulo, bool completada, Guid? categoriaId, Guid? usuarioId)
    {
        if (_tareas.TryGetValue(id, out var tarea))
        {
            tarea.Titulo = titulo;
            tarea.Completada = completada;
            tarea.CategoriaId = categoriaId;
            tarea.UsuarioId = usuarioId;
            return true;
        }
        return false;
    }

    public bool AsignarUsuario(Guid tareaId, Guid? usuarioId)
    {
        if (!_tareas.TryGetValue(tareaId, out var tarea))
            return false;

        if (usuarioId.HasValue && !ExisteUsuario(usuarioId.Value))
            return false;

        tarea.UsuarioId = usuarioId;
        return true;
    }

    public bool Completar(Guid id)
    {
        if (_tareas.TryGetValue(id, out var tarea))
        {
            tarea.Completada = true;
            return true;
        }
        return false;
    }

    public bool Borrar(Guid id) => _tareas.TryRemove(id, out _);

    public IReadOnlyCollection<Categoria> ListarCategorias() =>
        _categorias.Values.OrderBy(c => c.Nombre).ToList();

    public Categoria? ObtenerCategoria(Guid id) => _categorias.GetValueOrDefault(id);

    public bool AgregarCategoria(Categoria categoria)
    {
        if (CategoriaDuplicada(categoria.Nombre))
            return false;

        return _categorias.TryAdd(categoria.Id, categoria);
    }

    public bool ActualizarCategoria(Guid id, string nombre)
    {
        if (!_categorias.TryGetValue(id, out var categoria))
            return false;

        if (CategoriaDuplicada(nombre, id))
            return false;

        categoria.Nombre = nombre;
        return true;
    }

    public bool BorrarCategoria(Guid id)
    {
        if (!_categorias.TryRemove(id, out _))
            return false;

        foreach (var tarea in _tareas.Values.Where(t => t.CategoriaId == id))
        {
            tarea.CategoriaId = null;
        }

        return true;
    }

    public bool CategoriaDuplicada(string nombre, Guid? excluirId = null)
    {
        var normalizado = nombre.Trim();

        foreach (var categoria in _categorias.Values)
        {
            if (excluirId.HasValue && categoria.Id == excluirId.Value)
                continue;

            if (string.Equals(categoria.Nombre.Trim(), normalizado, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public IReadOnlyCollection<Usuario> ListarUsuarios() =>
        _usuarios.Values.OrderBy(u => u.Nombre).ToList();

    public Usuario? ObtenerUsuario(Guid id) => _usuarios.GetValueOrDefault(id);

    public bool AgregarUsuario(Usuario usuario)
    {
        if (EmailDuplicado(usuario.Email))
            return false;

        return _usuarios.TryAdd(usuario.Id, usuario);
    }

    public bool ActualizarUsuario(Guid id, string nombre, string email)
    {
        if (!_usuarios.TryGetValue(id, out var usuario))
            return false;

        if (EmailDuplicado(email, id))
            return false;

        usuario.Nombre = nombre;
        usuario.Email = email;
        return true;
    }

    public bool BorrarUsuario(Guid id)
    {
        if (!_usuarios.TryRemove(id, out _))
            return false;

        foreach (var tarea in _tareas.Values.Where(t => t.UsuarioId == id))
        {
            tarea.UsuarioId = null;
        }

        return true;
    }

    public bool EmailDuplicado(string email, Guid? excluirId = null)
    {
        var normalizado = email.Trim();

        foreach (var usuario in _usuarios.Values)
        {
            if (excluirId.HasValue && usuario.Id == excluirId.Value)
                continue;

            if (string.Equals(usuario.Email.Trim(), normalizado, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
