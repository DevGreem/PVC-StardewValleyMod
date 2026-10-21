# Convenciones de ramas y commits

Este repositorio usa un flujo con tres ramas estáticas y una rama efímera por cambio.

## Modelo de ramas

```
feature/<slug> ──PR/merge──> features ──PR/merge──> tests ──PR/merge──> main
```

Las tres ramas estáticas **solo reciben pull requests o merges**. Nunca se commitea
directamente en ellas y nunca se les hace force-push.

| Rama | Propósito | Qué recibe |
| --- | --- | --- |
| `features` | Integración de cambios terminados | PR/merges de ramas de cambio |
| `tests` | Validación e integración antes de producción | PR/merges desde `features` |
| `main` | Código estable | PR/merges desde `tests` |

### Ramas de cambio

Cada cambio parte **siempre de `features`**, nunca de `main` ni de `tests`:

```bash
git switch features
git pull --ff-only
git switch -c feature/<slug>
```

Nombres permitidos, en kebab-case y en inglés:

- `feature/<slug>` — comportamiento nuevo.
- `fix/<slug>` — corrección de un fallo.
- `docs/<slug>` — documentación, OpenSpec o README.
- `test/<slug>` — solo pruebas.
- `refactor/<slug>` — cambio interno sin cambiar comportamiento.
- `perf/<slug>` — mejora de rendimiento.
- `build/<slug>` — build, dependencias o empaquetado.
- `ci/<slug>` — automatización.
- `chore/<slug>` — mantenimiento.

Una rama de cambio debe contener **un solo cambio lógico**. Si el trabajo mezcla
una feature con un arreglo o una refactorización, se separa en ramas y PRs distintos.

### Flujo de integración

1. Crear la rama de cambio desde `features`.
2. Implementar, probar y documentar el cambio.
3. Abrir un PR de `feature/<slug>` hacia `features`.
4. Cuando `features` esté integrada y validada, abrir un PR/merge de `features` hacia `tests`.
5. Cuando `tests` pase la validación, abrir un PR/merge de `tests` hacia `main`.
6. Archivar el cambio de OpenSpec cuando corresponda.

Los merges hacia las ramas estáticas se hacen con merge commit (`--no-ff`) para
conservar la trazabilidad del PR. No se hace squash de una rama completa si eso
borra la separación de commits.

## Commits

Se sigue [Conventional Commits 1.0.0](https://www.conventionalcommits.org/es/v1.0.0/).

Formato:

```
<type>(<scope>): <descripción>

[cuerpo opcional]

[footer opcional]
```

Reglas:

- `type` es obligatorio y va en minúsculas.
- `scope` es opcional; si se usa, describe el área (`audio`, `network`, `config`,
  `ui`, `openspec`, `docs`, `build`, etc.) y va en minúsculas.
- La descripción es imperativa, en minúsculas, sin punto final y de 72 caracteres
  como máximo.
- Los mensajes de commit se escriben en inglés. La documentación y la conversación
  pueden estar en español; los comentarios del código van siempre en inglés.
- El cuerpo explica **qué** y **por qué**, no el detalle línea a línea; se envuelve
  a 72 columnas.
- Un commit contiene un solo cambio lógico. No se mezclan `feat`, `fix`, `docs` y
  `test` de temas distintos en el mismo commit.
- Los tests y la documentación de un mismo cambio pueden ir en el mismo commit o en
  commits `test:` / `docs:` separados, pero nunca en un commit de otro cambio.
- Los cambios incompatibles llevan `!` después del tipo/scope y un footer
  `BREAKING CHANGE: ...`.
- No se usan mensajes genéricos como `wip`, `changes`, `fix`, `update` o `stuff`.

### Tipos permitidos

| Tipo | Uso |
| --- | --- |
| `feat` | Comportamiento nuevo |
| `fix` | Corrección de un fallo |
| `docs` | Documentación, README, OpenSpec |
| `style` | Formato, sin cambio de comportamiento |
| `refactor` | Cambio interno sin cambiar comportamiento |
| `perf` | Mejora de rendimiento |
| `test` | Pruebas nuevas o corregidas |
| `build` | Build, dependencias, empaquetado |
| `ci` | Automatización |
| `chore` | Mantenimiento que no encaja en otro tipo |
| `revert` | Revertir un commit anterior |

### Ejemplos

```text
feat(audio): add static wall occlusion
fix(audio): apply master volume after limiting
docs(openspec): propose static wall occlusion
test(audio): cover one-pole occlusion filter
refactor(config): extract radius normalization
chore(build): pin mod build package
```

Con breaking change:

```text
feat(audio)!: make voice reach sender-controlled

BREAKING CHANGE: the local radius no longer gates incoming voice.
```

## Antes de commitear

- Ejecutar `dotnet test PVC.slnx` y `dotnet build PVC.slnx`; no debe haber fallos
  ni warnings.
- No commitear `bin/`, `obj/`, artefactos de build, `.ai/`, configuraciones locales
  ni secretos.
- Revisar `git status` y `git diff --staged` antes de crear el commit.
- Si el cambio tiene un OpenSpec change, enlazarlo en el cuerpo del commit o en el PR.

## Pull requests

- El título del PR sigue el mismo formato Conventional Commits.
- La descripción explica el cambio, cómo se probó y qué queda fuera de alcance.
- Cada PR debe indicar su rama base (`features` para cambios normales).
- No se mergea un PR sin tests verdes.
- Los PRs hacia `tests` y `main` solo salen de la rama estática anterior.
