# SAM-BIM runtime URLs (Q4) - SAM_Windows

PR: SAM-BIM/SAM_Windows#13. Branch `fix/sam-bim-runtime-urls-q4` -> base `sow/2026-Q4` (Q4 base `328808a`). Record date: 2026-10-06.

## Current status

PR open, **not merged**. Source-only, minimum change: material "help" links that opened the HoareLea wiki now
open the SAM-BIM wiki. No `.gitmodules`, gitlink, workflow, `master`, `sow/2026-Q3` or icon-redesign change.

## Work completed

| Old HoareLea destination | New SAM-BIM destination | Where |
|---|---|---|
| `https://github.com/HoareLea/SAM/wiki/Construction#...` | `https://github.com/SAM-BIM/SAM/wiki/Construction#...` | `SAM_Windows/SAM.Core.Windows/Forms/MaterialForm.cs` (4 links), `SelectMaterialForm.cs` (4), `MaterialLibraryForm.cs` (1) |

Anchors unchanged: `#materials`, `#gas-material`, `#transparent-material`, `#opaque-material`.
A second commit adds the SPDX + copyright header the repository's `spdx` check requires in every changed `.cs` file (these three legacy files had none); header lines only.

## Why the change is required

SAM-BIM is now the authoritative development ecosystem and HoareLea is no longer the synchronised operational source, so
the in-product help should open the maintained SAM-BIM wiki instead of an upstream copy that may drift.

## Decisions and assumptions

- The SAM-BIM wiki (`SAM-BIM/SAM.wiki`) holds an identical `Construction` page; its headings (`Materials`, `Opaque material`,
  `Transparent Material`, `Gas Material`) generate exactly these four anchors.
- Only the destination changed; no label or tooltip names HoareLea, and behaviour is otherwise identical.

## Files changed

The three `.cs` files above (9 URL lines, plus the 3 header lines in each) and this record.

## Validation

- `git diff --check` clean; diff reviewed line by line.
- `msbuild SAM_Windows.sln -p:Configuration=Release` with `APPDATA`/`USERPROFILE` redirected: 0 errors; `SAM.Core.Windows.dll` contains the new `SAM-BIM/SAM/wiki/Construction#...` links and no `HoareLea` string.
- No test project in this repository. PR CI (`build`, `spdx`) green; see the PR.

## Unresolved issues, risks

- None introduced by this change.

## Exact next step

Merge into `sow/2026-Q4` after green CI (maintainer-approved). After merge, the `PROJECT_PROGRESS.md` closeout is a direct docs commit on `sow/2026-Q4` (never on this branch).
