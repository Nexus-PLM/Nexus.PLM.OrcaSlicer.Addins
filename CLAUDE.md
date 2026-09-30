# CLAUDE.md — Nexus.PLM.OrcaSlicer.Addins

The Nexus PLM add-in for **OrcaSlicer**. Scaffold created 29 Sep 2026; no add-in code yet.

## Measured before writing anything (29 Sep 2026)

- **OrcaSlicer 2.4.2 stable (installed, `C:\Program Files\OrcaSlicer`) has NO plugin runtime.**
  `OrcaSlicer.dll` contains no `orca_plugins`, `Py_Initialize` or capability strings. The Python
  plugin system is **nightly-only** for now; the stable release neither shows nor loads plugins.
- **The plugin system** (orcaslicer.com/wiki/developer_reference/plugin_development):
  - plugins live in `%APPDATA%\OrcaSlicer\orca_plugins\<name>\` - one entry file, a single `.py`
    (PEP 723 `# /// script` block with `[tool.orcaslicer.plugin] name/description/author/version`)
    or a `.whl`; exactly one class marked `@orca.plugin` subclassing `orca.base`, whose
    `register_capabilities()` calls `orca.register_capability(cls)`;
  - capability types: `script` (`execute()`, run from **File > Plugins > Run** or the Speed Dial),
    `slicing-pipeline` (`execute(ctx)` at slicing steps / `psGCodePostProcess`), `printer-connection`
    (agent, still WIP);
  - read-only host access: `orca.host.model()`, `orca.host.plater()`, `orca.host.preset_bundle()`;
    mesh access via `ModelVolume.mesh()`;
  - UI: `orca.host.ui.message(...)`, `show_dialog(html, ...)` (modal), `create_window(html, ...)`,
    `create_dock_panel(...)`, `create_progress_dialog(...)`;
  - results are `orca.ExecutionResult.success/skipped/failure`; `print()`/stderr go to
    `%APPDATA%\OrcaSlicer\log\python_*.log`;
  - plugin instances are captured at load: reload = reopen the Plugins dialog or restart.
  - **No menu-bar entry point** - commands live in the Plugins dialog. Whether a plugin can learn
    the open project's file path is NOT documented; measure it on a nightly before designing.
- Same print-host upload hooks as PrusaSlicer (OctoPrint, native Moonraker, ...), and the same
  `.3mf` project format (Bambu Studio lineage: `Metadata/project_settings.config` etc.). Whether
  Orca preserves foreign 3MF metadata on save has NOT been measured.

## Rules that are not negotiable (inherited from every other Nexus add-in)

- **The service is the only thing this talks to.** `http://localhost:5100`; never the Engine,
  never the vault. Anything host-specific is declared by the add-in (`HOST_NAME`,
  `FILE_EXTENSIONS`) and travels with the request; the service keeps no list of hosts.
- **Uploads send the document's OWN path**, never a temp copy: `SaveRequest.FilePath` is one path
  the service both reads and records as `plm_file_path`. Marc: "it must get written to the
  staging directory."
- **Revise ups the revision in place** when the staged file is the open document.
- **Save As New offers only filled-in values that are not PLM's own** (part number, revision, the
  four stamps). The service merges the offer onto the new revision as-is.
- **No toast of our own where the service already toasts** (Sign Out).
- **Never commit to `main` or `next`.** Work on a `Marc/` branch; PRs to `next`; Marc merges.
- Every change adds a test. Tests run without OrcaSlicer present.

## Sibling add-ins to copy from

`Nexus.PLM.Inkscape.Addins` and `Nexus.PLM.Gimp.Addins` (Python; `nexusplm/{client,state,
identity,navigator}.py` are host-independent and were copied verbatim between them).
`Nexus.PLM.PrusaSlicer.Addins` is this repo's twin.
