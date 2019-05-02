using System;
using System.Collections.Generic;
using Syncfusion.DataSource;
using Syncfusion.DataSource.Extensions;

namespace Gab.Base.Grouping
{
    public class GroupComparer : IComparer<GroupResult>, ISortDirection
    { 
        public GroupComparer()
        {
            SortDirection = ListSortDirection.Ascending;
        }

        public ListSortDirection SortDirection { get; set; }


        public int Compare(GroupResult x, GroupResult y)
        {
            var xKey = (GroupKey)x.Key;
            var yKey = (GroupKey)y.Key;

            // Objects are compared and return the SortDirection
            if (xKey.CompareTo(yKey) > 0)
                return SortDirection == ListSortDirection.Ascending ? 1 : -1;
            if (xKey.CompareTo(yKey) == -1)
                return SortDirection == ListSortDirection.Ascending ? -1 : 1;
            return 0;
        }
    }
}
