# Nexus PLM for OrcaSlicer

Product lifecycle management for OrcaSlicer: the print project (`.3mf`) and its G-code tracked in and
out of Nexus PLM - the same command set the Nexus PLM add-ins give Word, LibreOffice, OpenOffice,
ONLYOFFICE, Inkscape and GIMP.

**Status: scaffold.** The integration route is being decided; see `CLAUDE.md` for what was
measured about OrcaSlicer before any code was written.

## What this talks to

Only the Nexus PLM **Addin Service** on `http://localhost:5100`, hosted by the Nexus PLM tray
application (`Nexus.PLM.WPF.Addins`). Never the Engine or the vault directly.

## Licence

MIT - see `LICENSE`.


## Server-side translators (the repo's first shipped code)

Per the three-deliverables convention (the Addin SDK's `docs/architecture.md`, section 10),
`Nexus.PLM.OrcaSlicer.Translators` ships this repo's example conversions for the Translation
server, deployed to `Nexus\Translators\orcaslicer\` — all pure .NET, no slicer needed:

| Key | Conversion | Why |
|---|---|---|
| `3mf_to_glb` | 3MF → binary glTF | a checked-in Orca project becomes viewable in the web 3D viewer |
| `3mf_to_obj` | 3MF → Wavefront OBJ | neutral mesh interchange |
| `gcode_to_png` | G-code → PNG preview | the slicer's embedded thumbnail, decoded from the comments |

A 3MF is a zip with an XML model part, and slicer G-code embeds its preview as base64
comments, so none of this shells out to anything. A `v*` tag attaches
`Nexus.PLM.OrcaSlicer.Translators.zip` to the GitHub release; keys are reserved in
`Nexus.PLM.Services/docs/translator-ownership.md` (PrusaSlicer owns 3mf→stl/png/gcode).
