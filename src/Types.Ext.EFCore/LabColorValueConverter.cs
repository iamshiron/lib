using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shiron.Lib.Types;

namespace Shiron.Lib.Types.Ext.EFCore;

public class LabColorValueConverter : ValueConverter<LabColor, double[]> {
    public LabColorValueConverter() : base(
        lab => new double[] { lab.L, lab.A, lab.B },
        arr => new LabColor(arr[0], arr[1], arr[2])
    ) { }
}
