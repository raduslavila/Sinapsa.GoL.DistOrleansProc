# Orleans Upgrade Report

## Scope
- Solution: `Sinapsa.GoL.DistOrleansProc.sln`
- Upgrade goal: Orleans `3.8.x` -> `10.1.0` on `.NET 9`
- Branch: `feature/orleans-upgrade`

## Completed changes
- Introduced central package version management via `Directory.Packages.props`.
- Replaced legacy Orleans packages with Orleans 10 package set:
  - `Microsoft.Orleans.OrleansRuntime` -> `Microsoft.Orleans.Server`
  - `Microsoft.Orleans.Runtime.Abstractions` -> `Microsoft.Orleans.Runtime` / `Microsoft.Orleans.Core.Abstractions`
  - `Microsoft.Orleans.CodeGenerator.MSBuild` -> `Microsoft.Orleans.Sdk`
  - Removed legacy provider/telemetry package IDs from Orleans 3.x set.
- Refactored Orleans 10 API incompatibilities:
  - Removed deprecated `ConfigureApplicationParts` usage.
  - Removed obsolete `IGrainIdentity`-dependent abstractions/implementations.
  - Removed deprecated Linux stats extension usage.
- Added Orleans 10 serialization metadata:
  - `[GenerateSerializer]`, `[Alias]`, and `[Id]` on grain state/data contracts.
  - Added missing aliases on grain interfaces used by analyzers.
- Dashboard migration:
  - Replaced `OrleansDashboard` with `Microsoft.Orleans.Dashboard`.
  - Updated configuration to use Orleans 10 dashboard API (`AddDashboard`) and endpoint mapping (`MapOrleansDashboard`).
- Runtime stability fix in grain logic:
  - Removed persisted neighbor object graph dependency.
  - Replaced with computed internal-neighbor counting at runtime.
- Docker tooling fix:
  - Updated `docker-compose.yml` CPU values from millicores (`500m`, `25m`) to decimal format (`0.50`, `0.025`) to resolve `DT1001`.

## Validation
- Individual project builds: successful.
- Full solution build: successful.

## Notes
- Existing analyzer/style warnings are not blockers and were left for a separate code-quality pass.
