using System.Diagnostics.CodeAnalysis;

namespace CommonLibrary.Extensions;

public static class CollectionExtensions
{
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this ICollection<T>? collection)
    {
        return collection == null || collection.Count == 0;
    }
}
