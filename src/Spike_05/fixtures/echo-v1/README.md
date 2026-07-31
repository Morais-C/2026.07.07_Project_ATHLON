# Fixture: echo-v1

Checked-in baseline for Spike_04 (immutable — Applier copies, never overwrites).

| Item | Value |
|------|-------|
| Entry project | `Echo/Echo.csproj` |
| Behavior | Read a line → echo it |
| Bounds | net9.0 console; 1 source file + `.csproj` |

```bash
dotnet build Echo/Echo.csproj
```
