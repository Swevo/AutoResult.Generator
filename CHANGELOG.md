# Changelog

All notable changes to AutoResult.Generator are documented here.

## [1.0.0] - 2025-06-25

### Added
- Core result types: `Result<T>`, `Result<T,TError>`, `Unit`, `ResultExtensions`
- `[TryWrap]` attribute generates `Try*()` wrappers for public methods on partial classes
- Supports sync, void, async (`Task<T>`), and async-void (`Task`) methods
- `void`/`Task` methods wrapped to `Result<Unit>` using `Unit.Value`
- Methods already prefixed with `Try` are skipped to avoid double-wrapping
- AR001 diagnostic (Error): `[TryWrap]` on non-partial class
- AR002 diagnostic (Warning): `[TryWrap]` class with no wrappable methods
- GitHub Actions CI with NuGet publish on `v*` tags
