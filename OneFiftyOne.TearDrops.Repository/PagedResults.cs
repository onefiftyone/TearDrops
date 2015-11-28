using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OneFiftyOne.TearDrops.Repository
{
    [Serializable]
    public class PagedResults<T>
    {
        public PagedResults() { }

        public PagedResults(int totalRows, IList<T> items)
        {
            TotalRows = totalRows;
            Items = items;
        }

        public int TotalRows { get; set; }
        public IList<T> Items { get; set; }
    }

    [Serializable]
    public class DynamicPagedResults<T> : PagedResults<T>
    {
        public List<string> Columns { get; set; }
    }
}
