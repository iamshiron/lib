using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shiron.Lib.Types;

namespace Shiron.Lib.Types.Ext.EFCore;

/// <summary>
/// Converts a <see cref="Color32"/>[] palette to a compact JSON array of packed
/// RGBA integers for storage in a <c>jsonb</c> column. The packed-int format
/// mirrors <see cref="Color32ValueConverter"/> (single color &#8594; int), keeping
/// the single-value and palette representations consistent.
/// </summary>
public class Color32ArrayValueConverter : ValueConverter<Color32[], string> {
    public Color32ArrayValueConverter() : base(
        colors => JsonSerializer.Serialize(colors.Select(c => c.ToRgba).ToArray()),
        json => JsonSerializer.Deserialize<int[]>(json)!
            .Select(v => new Color32(v)).ToArray()
    ) { }
}
