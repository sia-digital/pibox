namespace PiBox.Testing.Utils;

#nullable enable

public static class TestUtils
{
    public static bool CompareSequences<T>(IEnumerable<T>? first, IEnumerable<T>? second, IEqualityComparer<T>? comparer)
    {
        if (first is null && second is null)
            return true;

        if ((first is not null && second is null)
            || (first is null && second is not null))
            return false;

        return first!.SequenceEqual(second!, comparer);
    }
}

#nullable restore
