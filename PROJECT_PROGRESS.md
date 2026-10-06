# Project Progress - SAM_Windows (2026-Q4)

## Branch

`sow/2026-Q4` - bootstrapped 2026-10-06 from `master` `b6e251d9`. Frozen Q3 record: `sow/2026-Q3` @ `05a8edae` (not modified).

## Last updated

2026-10-06 (Q4 operational cleanup).

## Current status

Q4 branch cut from `master` `b6e251d9`, which is the exact commit pinned in SAM_Deploy's frozen Q3 baseline (`v20261006.1`). Bootstrap added only internal docs (this file, `AGENTS.md`). No product source changed. No Q4 product work has started.

## Q4 priorities

Not yet set by the owner. Record them here at the first Q4 planning pass. Known carry-over work is listed below.

## Known carry-over work

- None identified for this repository at bootstrap.

## Repository-specific next steps

- Await Q4 planning. Open PRs for Q4 work against `sow/2026-Q4`.
- Follow the continuity convention in `AGENTS.md` for every PR and closeout.

## Decisions / assumptions

- Q4 base is `master` `b6e251d9`; the internal files were recovered from `sow/2026-Q3` into this branch only, never onto `master`.
- Q4 history intentionally does not contain the Q3 branch history (the maintained `master` is the promoted Q3 line, which is not a descendant of `sow/2026-Q3`); the frozen `sow/2026-Q3` branch is the permanent record.
- Historical Q2/Q3 content below is kept as evidence; its branch names, SHAs and next steps describe Q3 and are not current instructions.

## Validation

- Bootstrap verified 2026-10-06: `sow/2026-Q4` was created at exactly `b6e251d9` and the push was a normal (non-forced) branch creation.

## Issues / blockers

- None at bootstrap.

## Next step

- Owner to set Q4 priorities; then start the first Q4 task from this branch.

## Q4 operational cleanup (2026-10-06)

- Reviewed every active Q2/Q3 reference in this repository on `sow/2026-Q4` (workflow branch filters, dependency-branch resolution, `.gitmodules`/validation, docs). Historical Q2/Q3 mentions (feature documentation records, the frozen Q3 section below) are intentionally unchanged.
- Changed (`932cc0f`): removed the dead `$candidates += 'sow/2026-Q2'` fallback from the dependency-branch resolution in `.github/workflows/build.yml`. No dependency repository has a `sow/2026-Q2` branch, so the entry never matched and resolution already fell through to the default branch; behaviour is unchanged (PR head ref, current sow ref, then the dependency's default branch) and no per-quarter edit is needed.
- Checked, no action: the `github.repository_owner == 'SAM-BIM'` build guard (intentional; its comment names HoareLea only to explain why the guard exists), CODEOWNERS (SAM-BIM owners), and workflow secrets (no HoareLea-named secret). The local `upstream` (HoareLea) remote is preserved.
- Full cross-repository record, migration table and owner decisions: `SAM_Deploy:sow/2026-Q4` `PROJECT_PROGRESS.md`.

## Q4 runtime-URL cleanup (2026-10-06)

- **Status:** complete. SAM-BIM/SAM_Windows#13 merged into `sow/2026-Q4` as merge commit `3031612d6fda4823f27c62dafb78f85b527c2098` (PR head `658cc73ed288fabc8d1bc250faa8c23773d76da5`, Q4 base `328808a`); merge method: merge commit (repository convention). Remote and local `fix/sam-bim-runtime-urls-q4` removed.
- **Work completed:** The material forms' help links now open `https://github.com/SAM-BIM/SAM/wiki/Construction#...` (anchors `materials`, `gas-material`, `transparent-material`, `opaque-material`) instead of the `HoareLea/SAM` wiki; the SAM-BIM wiki holds the identical page. SAM-BIM is the authoritative ecosystem; HoareLea is no longer the synchronised operational source. Record: the PR's `SAM-BIM-RuntimeUrls-Q4.md` document.
- **Decisions / owner classifications:** Assembly author/contact strings (`Hoare Lea`, `@hoarelea.com` in `Kernel/AssemblyInfo.cs`) are provenance/metadata, not repository ownership: KEEP unchanged.
- **Files changed:** `MaterialForm.cs`, `SelectMaterialForm.cs`, `MaterialLibraryForm.cs` (9 URL lines, plus the SPDX header the `spdx` check requires in each changed `.cs` file), `docs/SAM-BIM-RuntimeUrls-Q4.md`.
- **Validation:** `msbuild SAM_Windows.sln -p:Configuration=Release` (APPDATA/USERPROFILE redirected): 0 errors; `SAM.Core.Windows.dll` contains the new links and no HoareLea string. No test project. PR CI build and spdx green.
- **Unresolved issues, risks:** None introduced.
- **Next step:** None for this change.

---

# Historical record - 2026-Q3 (frozen)

Source: last revision of the file on `sow/2026-Q3`, commit `b5bb64e` (the file was removed from the Q3 tip by `aedac63`; `sow/2026-Q3` tip is `05a8edae`). The Q3 file was an unpopulated template ("Not updated yet"); no Q3 work was recorded in this repository's progress file.
