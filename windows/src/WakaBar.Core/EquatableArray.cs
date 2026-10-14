using System.Collections;

namespace WakaBar.Core;

/// <summary>
/// Read-only list compared by its items instead of by reference, so records holding it get
/// value equality for free: a new field in <see cref="Snapshot"/> is covered by equality
/// (and by <see cref="Snapshot.HasSameStats"/>) without touching any comparison code.
/// </summary>
public sealed class EquatableArray<T> : IReadOnlyList<T>, IEquatable<EquatableArray<T>>
{
    private readonly T[] items;

    public EquatableArray(IEnumerable<T> items)
    {
        this.items = items.ToArray();
    }

    public static EquatableArray<T> Empty { get; } = new(Array.Empty<T>());

    public int Count => items.Length;

    public T this[int index] => items[index];

    public bool Equals(EquatableArray<T>? other) =>
        other is not null && items.SequenceEqual(other.items);

    public override bool Equals(object? obj) => Equals(obj as EquatableArray<T>);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in items)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
