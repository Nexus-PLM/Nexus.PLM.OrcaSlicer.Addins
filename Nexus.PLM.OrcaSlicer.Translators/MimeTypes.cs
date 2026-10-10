namespace Nexus.PLM.OrcaSlicer.Translators;

/// <summary>
/// The MIME values this repo's translators speak. Values follow the installation's format
/// catalog (the MIME Types app), which is the source of truth — 3MF is the catalog's
/// <c>model/3mf</c>, never the long vendor-tree value.
/// </summary>
public static class MimeTypes
{
    /// <summary>3MF project/model (<c>.3mf</c>) — a zip carrying a 3D model XML part.</summary>
    public const string ThreeMf = "model/3mf";

    /// <summary>Wavefront OBJ mesh.</summary>
    public const string Obj = "model/obj";

    /// <summary>Binary glTF 2.0 — what the Nexus web 3D viewer renders.</summary>
    public const string GltfBinary = "model/gltf-binary";

    /// <summary>G-code toolpath (<c>.gcode</c>).</summary>
    public const string GCode = "text/x-gcode";

    /// <summary>PNG raster.</summary>
    public const string Png = "image/png";
}
