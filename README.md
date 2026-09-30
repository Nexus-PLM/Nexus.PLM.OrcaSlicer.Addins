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
