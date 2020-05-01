using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinanceMarketMaker.BusinessLogic
{
    public static class ListExtension
    {
        public static List<T> Splice<T>(this List<T> source, int index, int count)
        {
            var items = source.GetRange(index, count);
            source.RemoveRange(index, count);
            return items;
        }
    }
}
