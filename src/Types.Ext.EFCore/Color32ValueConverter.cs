using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shiron.Lib.Types;

namespace Shiron.Lib.Types.Ext.EFCore;

public class Color32ValueConverter : ValueConverter<Color32, int> {
    public Color32ValueConverter() : base(
        c => c.ToRgba,
        v => new Color32(v)
    ) { }
}
