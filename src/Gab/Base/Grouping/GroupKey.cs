using System;

namespace Gab.Base.Grouping
{
    public class GroupKey : IComparable<GroupKey>
    {
        public int Value { get; set; }
        public string Name { get; set; }
        public int CompareTo(GroupKey other)
        {
            return other == null ? 1 : Value.CompareTo(other.Value);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GroupKey o))
                return false;
            return Value.Equals(o.Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
