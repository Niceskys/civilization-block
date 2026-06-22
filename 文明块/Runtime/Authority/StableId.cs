using System;

namespace WenMingBlocks.Runtime.Authority
{
    public readonly struct StableId : IEquatable<StableId>
    {
        public string Value { get; }

        public StableId(string value)
        {
            if (!IsValid(value))
            {
                throw new ArgumentException("Stable id must use namespace:type:id format.", nameof(value));
            }

            Value = value;
        }

        public static bool IsValid(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string[] parts = value.Split(':');
            if (parts.Length != 3)
            {
                return false;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(parts[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static StableId Create(string ns, string type, string id)
        {
            return new StableId($"{ns}:{type}:{id}");
        }

        public bool Equals(StableId other)
        {
            return StringComparer.Ordinal.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is StableId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
