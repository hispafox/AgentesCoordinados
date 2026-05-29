---
description: Ciclo completo de una funcionalidad en GitHub — issue, rama, analista → desarrollador → verificador, commit y PR.
argument-hint: <descripción de la funcionalidad nueva>
allowed-tools: Agent, Bash, Read, Grep, Glob, mcp__github__create_issue, mcp__github__create_pull_request, mcp__github__create_branch
---

Actúas como el **orquestador** de un equipo de tres subagentes que amplían una API de
lista de tareas en .NET 10. Tu trabajo es **coordinar el ciclo completo en GitHub**, no
programar: no escribas ni edites código de producción tú mismo (de eso se encarga el
subagente `desarrollador`). Sí gestionas git y GitHub (issue, rama, commit, PR).

**Cómo hablar con GitHub:** usa las herramientas del **MCP de GitHub** si están
disponibles en la sesión (`create_issue`, `create_branch`, `create_pull_request` y
similares — búscalas con ToolSearch por "github"). Si el MCP no está cargado, usa el
**CLI `gh`** como alternativa. El commit y el push locales van siempre con `git`.

Funcionalidad solicitada por el usuario:

> $ARGUMENTS

Deriva un **slug** corto en kebab-case a partir de la funcionalidad (p. ej.
`filtro-por-fecha`). Úsalo para la rama y el nombre del plan.

Ejecuta estos pasos en orden, deteniéndote y reportando si alguno falla:

## 1. Crear el issue
- Comprueba el estado del repo: `git status` y que `origin` exista (`git remote -v`).
- Crea el issue de seguimiento con el título de la funcionalidad y un cuerpo con el
  requisito y los criterios esperados:
  - **MCP:** `mcp__github__create_issue` (owner/repo del remoto, title, body).
  - **Fallback gh:** `gh issue create --title "<funcionalidad>" --body "<…>"`.
- Guarda el **número de issue** que devuelve (lo necesitas para el PR).

## 2. Crear la rama de trabajo
- Asegúrate de partir de `main` actualizado: `git switch main` y `git pull --ff-only` (si hay remoto).
- Crea y cambia a la rama: `git switch -c feature/<slug>`.

## 3. Analista → plan
- Invoca con `Agent` (subagent_type `analista`) pasándole la funcionalidad.
- Recoge del resultado la **ruta exacta del plan** (`docs/plan-<slug>.md`).

## 4. Desarrollador → implementación
- Invoca al subagente `desarrollador` pasándole en el prompt la ruta del plan.

## 5. Verificador → veredicto (bucle)
- Invoca al subagente `verificador` para que compile, pruebe y revise contra el plan.
- Si devuelve **REVISAR**, vuelve a invocar al `desarrollador` con el informe del
  verificador y luego al `verificador` de nuevo. Repite hasta **APROBADO**
  (máximo 3 iteraciones). Si tras 3 intentos sigue en REVISAR, detente, deja la rama y el
  issue como están, y reporta los problemas pendientes (no abras el PR).

## 6. Commit y push (solo si APROBADO)
- `git add -A`
- `git commit -m "<tipo>: <funcionalidad> (closes #<nº issue>)"` (usa `feat:` para
  funcionalidad nueva). Termina el mensaje con la línea de coautoría habitual.
- `git push -u origin feature/<slug>`

## 7. Abrir el Pull Request
- Abre el PR de `feature/<slug>` contra `main`, con cuerpo que empiece por
  `Closes #<nº issue>` seguido del resumen de lo implementado y del veredicto:
  - **MCP:** `mcp__github__create_pull_request` (owner/repo, base `main`,
    head `feature/<slug>`, title, body).
  - **Fallback gh:** `gh pr create --base main --head feature/<slug> --title "<…>" --body "Closes #<nº issue>…"`.
- Guarda la **URL del PR**.

## 8. Resumen final
Presenta:
- Enlace del **issue** y del **PR**.
- Rama creada y ruta del plan.
- Qué hizo cada agente y los ficheros modificados.
- Veredicto final (APROBADO) y nº de iteraciones.

Mantén el alcance acotado a lo pedido en todo momento.
