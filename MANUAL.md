# Manual — Ampliar la API de Lista de Tareas con agentes orquestados de GitHub Copilot

Este documento explica, de principio a fin, cómo usar el equipo de agentes de Copilot que viene en `.github/agents/` para añadir funcionalidades a la API de lista de tareas. El orquestador no solo reparte el trabajo entre los especialistas: lleva el **ciclo completo en GitHub**, desde el issue hasta el Pull Request. Incluye un ejemplo que puedes reproducir tal cual.

---

## 1. Qué es esto

En `.github/agents/` hay cuatro agentes de Copilot:

| Agente | Rol | Qué puede hacer |
|--------|-----|-----------------|
| **orquestador** | Coordina y gestiona GitHub. No programa. | Crear el issue, la rama y el PR; invocar a los otros tres agentes en orden; hacer commit y push. |
| **analista** | Analiza el requisito. | Leer el código y escribir un plan en `docs/`. No toca código. |
| **desarrollador** | Implementa. | Editar código y compilar. |
| **verificador** | Control de calidad. | Compilar, probar y revisar. No toca código. |

La idea: tú hablas **solo con el orquestador**. Él abre el issue, crea la rama, reparte el trabajo entre analista → desarrollador → verificador (repitiendo si algo falla) y, cuando el verificador aprueba, hace el commit y abre el Pull Request. Eso es el *handoff*, envuelto de punta a punta en GitHub.

---

## 2. Qué necesitas

- **VS Code** con la extensión de **GitHub Copilot** y **Copilot Chat**, y una suscripción Copilot activa (Pro, Pro+, Business o Enterprise).
- **.NET 10 SDK** instalado (`dotnet --version` para comprobarlo).
- **GitHub CLI** instalado y autenticado: comprueba con `gh auth status`. Es el camino fiable para que el orquestador cree el issue y el PR. (Si tienes el **MCP de GitHub** conectado en Copilot, el orquestador lo intentará primero; pero el MCP es opcional y `gh` cubre el caso.)
- Un **remoto `origin`** configurado (`git remote -v`), porque el orquestador hace push de la rama.
- Abrir en VS Code la carpeta **raíz** del proyecto (la que contiene `.github/`). Si abres una subcarpeta, Copilot no verá los agentes.

> Los agentes funcionan también con el coding agent de github.com y con Copilot CLI, pero este manual asume **VS Code**, que es lo más cómodo para probar y grabar.

---

## 3. Cómo funciona el handoff por dentro

Tres piezas del frontmatter hacen que el orquestador pueda llamar a los demás y, además, tocar GitHub (mira `orquestador.agent.md`):

```yaml
tools: ["agent", "read", "search", "execute"]
agents: ["analista", "desarrollador", "verificador"]
```

- La herramienta **`agent`** es la que permite a un agente invocar a otro.
- La propiedad **`agents`** declara a quién puede invocar (sus subagentes).
- La herramienta **`execute`** es la que deja al orquestador ejecutar comandos: `git` para la rama, el commit y el push, y `gh` (o el MCP de GitHub) para el issue y el PR.

Sin la propiedad `agents`, el orquestador no enlazaría con los demás. Sin `execute`, podría coordinar pero no abriría issue ni PR. Con las tres, cuando le pides una funcionalidad, él lleva el ciclo entero y te va mostrando lo que hace en cada paso.

---

## 4. El ciclo completo en GitHub

Cuando le pides una funcionalidad al orquestador, no se limita a encadenar agentes: envuelve todo el trabajo en GitHub para que quede trazado. Son ocho pasos, y las tres acciones de GitHub —**issue**, **rama** y **Pull Request**— las hace siempre él; los subagentes no tocan nada de eso.

| # | Paso | Quién | Acción |
|---|------|-------|--------|
| 1 | **Crear el issue** | Orquestador | MCP `create_issue` o `gh issue create` → guarda el `#N` |
| 2 | **Crear la rama** | Orquestador | `git switch main` → `git pull --ff-only` → `git switch -c feature/<slug>` |
| 3 | **Planificar** | Analista | escribe `docs/plan-<slug>.md` y devuelve su ruta |
| 4 | **Implementar** | Desarrollador | edita el código según el plan + `dotnet build ListaTareas.slnx` |
| 5 | **Verificar** | Verificador | `dotnet build` + criterios → `APROBADO` / `REVISAR` (bucle, máx. 3) |
| 6 | **Commit + push** | Orquestador | `git commit -m "feat: … (closes #N)"` → `git push -u origin feature/<slug>` |
| 7 | **Abrir el PR** | Orquestador | MCP `create_pull_request` o `gh pr create --base main`, con `Closes #N` en el cuerpo |
| 8 | **Resumen** | Orquestador | enlaces del **issue** y del **PR**, ficheros y veredicto |

El detalle que más se agradece: el commit lleva `closes #N`. Así, cuando alguien fusione el PR, GitHub cierra el issue solo —no tienes que acordarte de nada. Y si tras tres vueltas el verificador sigue diciendo REVISAR, el orquestador **para y no abre el PR**: deja la rama y el issue como están y te cuenta qué quedó pendiente.

> ¿Prefieres la versión con Claude Code (el comando `/orquestar`)? El mismo ciclo, contado con diagramas, está en [`docs/ARQUITECTURA-AGENTES.md`](docs/ARQUITECTURA-AGENTES.md).

---

## 5. Paso a paso para añadir una funcionalidad

1. Abre la carpeta del proyecto en VS Code.
2. Abre **Copilot Chat** (icono de chat en la barra lateral).
3. En el **desplegable de agentes**, abajo junto a la caja de texto, selecciona **orquestador**.
   - Si no aparece: pulsa el desplegable → **Configure Custom Agents** y confirma que están los cuatro. También puedes escribir `/agents` en el chat para abrir ese menú.
4. Escribe la funcionalidad que quieres, en lenguaje natural, y envía.
5. Observa el ciclo: el orquestador abre el **issue**, crea la **rama** y va invocando a cada subagente. Te pedirá confirmación para ejecutar comandos de `git`/`gh` en la terminal.
6. Cuando el verificador apruebe, el orquestador hace el **commit** (`closes #N`), el **push** y abre el **Pull Request**. Te quedan los dos enlaces que importan: el del issue y el del PR.
7. Arranca y prueba: `cd src/ListaTareas.Api` y `dotnet run`; lanza las peticiones desde `ListaTareas.Api.http`.

---

## 6. Ejemplo completo y reproducible

Vamos a añadir **prioridad** a las tareas (Alta / Media / Baja) y un endpoint para filtrar por prioridad. Al final tendrás, además del código, un **issue** y un **Pull Request** abiertos.

### 6.1. El prompt exacto

Con el agente **orquestador** seleccionado, pega esto en el chat:

```
Añade un campo de prioridad a las tareas con valores Alta, Media o Baja
(por defecto Media). Que se pueda indicar al crear una tarea y añade un
endpoint para listar las tareas de una prioridad concreta. Coordina al
analista, al desarrollador y al verificador, y no amplíes el alcance.
```

### 6.2. Qué hará el orquestador y cada agente

0. **orquestador** → crea el issue (`gh issue create` o MCP) y la rama `feature/prioridad-tareas`.
1. **analista** → crea `docs/plan-prioridad.md`: nuevo campo `Prioridad` en `Tarea`, cambio en el DTO de creación, endpoint `GET /tareas/prioridad/{prioridad}`, y criterios de aceptación.
2. **desarrollador** → edita `Tarea.cs` (campo `Prioridad`), `Program.cs` (crear acepta prioridad + endpoint de filtro), añade el ejemplo al `.http`, y ejecuta `dotnet build ListaTareas.slnx`.
3. **verificador** → ejecuta `dotnet build`, comprueba los criterios del plan y emite el veredicto.
4. **orquestador** → tras APROBADO, hace commit (`closes #N`), push y abre el Pull Request.

### 6.3. Qué verás en la ventana del chat

> El **texto exacto varía** en cada ejecución (Copilot no es determinista) y según el modelo. Lo que sí será constante es la **estructura**: verás al orquestador anunciando cada paso y bloques de cada subagente. Tendrá un aspecto parecido a este:

```
orquestador
He recibido la petición. Abro el issue y creo la rama antes de coordinar al equipo.
  └ Issue creado: #5
  └ Rama: feature/prioridad-tareas

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
Verificador APROBADO. Hago commit, push y abro el PR.
  └ commit: feat: prioridad en tareas (closes #5)
  └ push:   feature/prioridad-tareas
  └ PR:     #6

Resumen:
  · analista     → plan en docs/plan-prioridad.md
  · desarrollador → 3 ficheros modificados, compila
  · verificador  → APROBADO
  · issue #5 · PR #6
La funcionalidad está lista para revisar y fusionar.
```

Verás también, según los pasos, **botones para aprobar acciones** (ejecutar `dotnet build`, `git` o `gh` en la terminal, o aplicar ediciones). Ahí confirmas tú.

### 6.4. Validar el resultado

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

## 7. Si algo no va

| Síntoma | Causa probable | Solución |
|---------|----------------|----------|
| No aparece el orquestador en el desplegable | No abriste la carpeta raíz, o el archivo está mal | Abre la carpeta que contiene `.github/`; revisa el frontmatter de `orquestador.agent.md`. |
| El orquestador hace el trabajo él mismo y no delega | Falta la propiedad `agents` o la herramienta `agent` | Comprueba que `orquestador.agent.md` tiene `tools: ["agent", ...]` y `agents: ["analista","desarrollador","verificador"]`. |
| Un subagente no se invoca | El `name` no coincide con el de la lista `agents` | Los `name` de cada archivo deben ser exactamente `analista`, `desarrollador`, `verificador`. |
| No crea el issue ni el PR, solo coordina agentes | Falta la herramienta `execute` en el orquestador | Añade `execute` a `tools` en `orquestador.agent.md`: `["agent","read","search","execute"]`. |
| `gh: command not found` | GitHub CLI sin instalar o fuera del PATH | `winget install GitHub.cli` y abre una terminal nueva; luego `gh auth login`. |
| El MCP de GitHub falla la autenticación (`does not support dynamic client registration`) | El endpoint MCP de GitHub no soporta el registro dinámico de cliente | No bloquea: el orquestador cae a `gh` CLI, que es el camino fiable. Asegúrate de tener `gh auth status` en verde. |
| El push falla por permisos | El token de `gh` no tiene scope `repo`, o el remoto no es tuyo | `gh auth status` para ver scopes; reautentica con `gh auth login` si falta `repo`. |
| El build falla por la versión de OpenApi | La versión del paquete no coincide con tu SDK | Ajusta la versión de `Microsoft.AspNetCore.OpenApi` en el `.csproj`. |

---

## 8. Notas honestas

- La salida del chat **no es palabra por palabra reproducible**: es un modelo de lenguaje. La estructura del flujo sí es estable.
- El ciclo en GitHub (issue → rama → commit → PR) está probado por la vía de **`gh` CLI**, que es la fiable. El **MCP de GitHub** es un atajo opcional: si está conectado, el orquestador lo intenta primero; si no autentica, no pasa nada, cae a `gh`.
- El reparto automático entre agentes está más asentado en **VS Code** y en el **coding agent de github.com**. En otros IDEs (JetBrains, Eclipse, Xcode) los custom agents están en preview.
- Si prefieres **control manual paso a paso** en vez de que el orquestador haga todo de una vez, existe la opción de *handoffs* (botones que te llevan al siguiente agente). Este proyecto está montado para el modo automático; si quieres el modo con botones, se configura con la propiedad `handoffs` en cada agente.
