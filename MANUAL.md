# Manual — Ampliar la API de Lista de Tareas con agentes orquestados de GitHub Copilot

Este documento explica, de principio a fin, cómo usar el equipo de agentes de Copilot que viene en `.github/agents/` para añadir funcionalidades a la API de lista de tareas. Incluye un ejemplo completo que puedes reproducir tal cual.

---

## 1. Qué es esto

En `.github/agents/` hay cuatro agentes de Copilot:

| Agente | Rol | Qué puede hacer |
|--------|-----|-----------------|
| **orquestador** | Coordina. No programa. | Invocar a los otros tres agentes en orden. |
| **analista** | Analiza el requisito. | Leer el código y escribir un plan en `docs/`. No toca código. |
| **desarrollador** | Implementa. | Editar código y compilar. |
| **verificador** | Control de calidad. | Compilar, probar y revisar. No toca código. |

La idea: tú hablas **solo con el orquestador**. Él se encarga de repartir el trabajo entre analista → desarrollador → verificador, y de repetir si algo falla. Eso es el *handoff*.

---

## 2. Qué necesitas

- **VS Code** con la extensión de **GitHub Copilot** y **Copilot Chat**, y una suscripción Copilot activa (Pro, Pro+, Business o Enterprise).
- **.NET 10 SDK** instalado (`dotnet --version` para comprobarlo).
- Abrir en VS Code la carpeta **raíz** del proyecto (la que contiene `.github/`). Si abres una subcarpeta, Copilot no verá los agentes.

> Los agentes funcionan también con el coding agent de github.com y con Copilot CLI, pero este manual asume **VS Code**, que es lo más cómodo para probar y grabar.

---

## 3. Cómo funciona el handoff por dentro

Dos cosas hacen que el orquestador pueda llamar a los demás (mira `orquestador.agent.md`):

```yaml
tools: ["agent", "read", "search"]
agents: ["analista", "desarrollador", "verificador"]
```

- La herramienta **`agent`** es la que permite a un agente invocar a otro.
- La propiedad **`agents`** declara a quién puede invocar (sus subagentes).

Sin la propiedad `agents`, el orquestador no enlazaría con los demás. Con ella, cuando le pides una funcionalidad, él decide cuándo llamar a cada subagente y te va mostrando lo que hace cada uno.

---

## 4. Paso a paso para añadir una funcionalidad

1. Abre la carpeta del proyecto en VS Code.
2. Abre **Copilot Chat** (icono de chat en la barra lateral).
3. En el **desplegable de agentes**, abajo junto a la caja de texto, selecciona **orquestador**.
   - Si no aparece: pulsa el desplegable → **Configure Custom Agents** y confirma que están los cuatro. También puedes escribir `/agents` en el chat para abrir ese menú.
4. Escribe la funcionalidad que quieres, en lenguaje natural, y envía.
5. Observa cómo el orquestador va invocando a cada subagente. Cuando el verificador apruebe, revisa los cambios y prueba la API.
6. Arranca y prueba: `cd src/ListaTareas.Api` y `dotnet run`; lanza las peticiones desde `ListaTareas.Api.http`.

---

## 5. Ejemplo completo y reproducible

Vamos a añadir **prioridad** a las tareas (Alta / Media / Baja) y un endpoint para filtrar por prioridad.

### 5.1. El prompt exacto

Con el agente **orquestador** seleccionado, pega esto en el chat:

```
Añade un campo de prioridad a las tareas con valores Alta, Media o Baja
(por defecto Media). Que se pueda indicar al crear una tarea y añade un
endpoint para listar las tareas de una prioridad concreta. Coordina al
analista, al desarrollador y al verificador, y no amplíes el alcance.
```

### 5.2. Qué hará cada agente

1. **analista** → crea `docs/plan-prioridad.md`: nuevo campo `Prioridad` en `Tarea`, cambio en el DTO de creación, endpoint `GET /tareas/prioridad/{prioridad}`, y criterios de aceptación.
2. **desarrollador** → edita `Tarea.cs` (campo `Prioridad`), `Program.cs` (crear acepta prioridad + endpoint de filtro), añade el ejemplo al `.http`, y ejecuta `dotnet build`.
3. **verificador** → ejecuta `dotnet build`, comprueba los criterios del plan y emite el veredicto.

### 5.3. Qué verás en la ventana del chat

> El **texto exacto varía** en cada ejecución (Copilot no es determinista) y según el modelo. Lo que sí será constante es la **estructura**: verás al orquestador anunciando cada paso y bloques de cada subagente. Tendrá un aspecto parecido a este:

```
orquestador
He recibido la petición. Voy a coordinar al equipo en tres pasos.

▸ Invocando al subagente: analista
```
```
analista
He revisado Program.cs y Tarea.cs. Dejo el plan en docs/plan-prioridad.md
con el nuevo campo Prioridad, el cambio en CrearTareaDto y el endpoint
GET /tareas/prioridad/{prioridad}. Criterios de aceptación incluidos.
  └ Creado: docs/plan-prioridad.md
```
```
orquestador
Plan listo. Continúo con la implementación.

▸ Invocando al subagente: desarrollador
```
```
desarrollador
Cambios realizados:
  · Tarea.cs        → añadida propiedad Prioridad (por defecto "Media")
  · Program.cs      → POST /tareas acepta prioridad; nuevo GET /tareas/prioridad/{prioridad}
  · ListaTareas.Api.http → ejemplos añadidos
Ejecutando dotnet build... compilación correcta (0 errores).
```
```
orquestador
Implementado. Lanzo la verificación.

▸ Invocando al subagente: verificador
```
```
verificador
dotnet build: correcto.
Criterios de aceptación:
  ✓ Prioridad por defecto "Media"
  ✓ Se puede indicar al crear
  ✓ Endpoint de filtro responde
  ✓ Convenciones del repo respetadas
Veredicto: APROBADO
```
```
orquestador
Resumen:
  · analista     → plan en docs/plan-prioridad.md
  · desarrollador → 3 ficheros modificados, compila
  · verificador  → APROBADO
La funcionalidad está lista para probar.
```

Verás también, según los pasos, **botones para aprobar acciones** (por ejemplo ejecutar `dotnet build` en la terminal o aplicar ediciones). Ahí confirmas tú.

### 5.4. Validar el resultado

```bash
cd src/ListaTareas.Api
dotnet run
```

Crear una tarea con prioridad:

```
POST http://localhost:5080/tareas
Content-Type: application/json

{ "titulo": "Pagar facturas", "prioridad": "Alta" }
```

Filtrar por prioridad:

```
GET http://localhost:5080/tareas/prioridad/Alta
```

Si responde con la tarea creada, la ampliación funciona.

---

## 6. Si algo no va

| Síntoma | Causa probable | Solución |
|---------|----------------|----------|
| No aparece el orquestador en el desplegable | No abriste la carpeta raíz, o el archivo está mal | Abre la carpeta que contiene `.github/`; revisa el frontmatter de `orquestador.agent.md`. |
| El orquestador hace el trabajo él mismo y no delega | Falta la propiedad `agents` o la herramienta `agent` | Comprueba que `orquestador.agent.md` tiene `tools: ["agent", ...]` y `agents: ["analista","desarrollador","verificador"]`. |
| Un subagente no se invoca | El `name` no coincide con el de la lista `agents` | Los `name` de cada archivo deben ser exactamente `analista`, `desarrollador`, `verificador`. |
| El build falla por la versión de OpenApi | La versión del paquete no coincide con tu SDK | Ajusta la versión de `Microsoft.AspNetCore.OpenApi` en el `.csproj`. |

---

## 7. Notas honestas

- La salida del chat **no es palabra por palabra reproducible**: es un modelo de lenguaje. La estructura del flujo sí es estable.
- El reparto automático entre agentes está más asentado en **VS Code** y en el **coding agent de github.com**. En otros IDEs (JetBrains, Eclipse, Xcode) los custom agents están en preview.
- Si prefieres **control manual paso a paso** en vez de que el orquestador haga todo de una vez, existe la opción de *handoffs* (botones que te llevan al siguiente agente). Este proyecto está montado para el modo automático; si quieres el modo con botones, se configura con la propiedad `handoffs` en cada agente.
