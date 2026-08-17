using System;

namespace kekchpek.BindableValues
{
    /// <summary>
    /// Strongly typed identifier for a value stored in <see cref="IBindableValueRegistry"/>.
    /// </summary>
    public readonly struct BindableValueId : IEquatable<BindableValueId>
    {
        /// <summary>
        /// Stable string identifier used in code and locale placeholders.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Creates a new value identifier.
        /// </summary>
        /// <param name="id">Unique non-null identifier string.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is null.</exception>
        public BindableValueId(string id)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

        public bool Equals(BindableValueId other) => Id == other.Id;

        public override bool Equals(object obj) => obj is BindableValueId other && Equals(other);

        public override int GetHashCode() => Id.GetHashCode();

        public override string ToString() => Id;
    }
}
