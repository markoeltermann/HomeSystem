using System.Diagnostics.CodeAnalysis;

namespace Web.Client.Helpers;

public static class TrimmerPreserve
{
    // This ensures MinValue and MaxValue are preserved even with trimming
    [DynamicDependency("MinValue", typeof(double))]
    [DynamicDependency("MaxValue", typeof(double))]
    private static void PreserveDoubleFields() { }
}