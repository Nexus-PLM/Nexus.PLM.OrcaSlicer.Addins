using System.Text;
using Nexus.PLM.Addin.Sdk.Translators;

namespace Nexus.PLM.OrcaSlicer.Translators;

/// <summary>
/// Extracts the preview thumbnail a slicer embeds in its G-code. OrcaSlicer (and PrusaSlicer)
/// write PNG thumbnails as base64 comment blocks, so the "conversion" is a decode — pure .NET
/// and instant, and it gives every sliced job a browsable preview image.
/// </summary>
public sealed class GcodeToPngTranslator : ITranslator
{
    /// <inheritdoc/>
    public string Key => "gcode_to_png";

    /// <inheritdoc/>
    public string Label => "G-code → PNG preview";

    /// <inheritdoc/>
    public string Description =>
        "Extracts the largest PNG preview thumbnail the slicer embedded in a G-code file's comments.";

    /// <inheritdoc/>
    public string InputMimeType => MimeTypes.GCode;

    /// <inheritdoc/>
    public string OutputMimeType => MimeTypes.Png;

    /// <inheritdoc/>
    public string OutputFileExtension => ".png";

    /// <inheritdoc/>
    public async Task<Stream> TranslateAsync(Stream input, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(input, Encoding.UTF8);

        var best = Array.Empty<byte>();
        var bestPixels = -1L;
        StringBuilder? current = null;
        long currentPixels = 0;

        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            var comment = line.StartsWith(';') ? line.TrimStart(';').Trim() : null;
            if (comment is null)
            {
                current = null; // thumbnails only live in comment blocks
                continue;
            }

            if (comment.StartsWith("thumbnail begin", StringComparison.OrdinalIgnoreCase))
            {
                // "; thumbnail begin 300x300 12345"
                var parts = comment.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var size = parts.Length > 2 ? parts[2].Split('x') : [];
                currentPixels = size.Length == 2
                    && long.TryParse(size[0], out var w) && long.TryParse(size[1], out var h)
                        ? w * h : 0;
                current = new StringBuilder();
            }
            else if (comment.StartsWith("thumbnail end", StringComparison.OrdinalIgnoreCase))
            {
                if (current is not null && currentPixels > bestPixels)
                {
                    try
                    {
                        best = Convert.FromBase64String(current.ToString());
                        bestPixels = currentPixels;
                    }
                    catch (FormatException) { /* malformed block; keep scanning */ }
                }
                current = null;
            }
            else
            {
                current?.Append(comment);
            }
        }

        if (best.Length == 0)
            throw new InvalidOperationException(
                "The G-code carries no embedded PNG thumbnail. Enable thumbnails in the " +
                "slicer's printer settings (G-code thumbnails) and re-slice.");
        return new MemoryStream(best);
    }
}
