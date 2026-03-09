using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSSsh.Lib
{
    internal static class EnumerableExtensions
    {
        /// <summary>
        /// 列挙体の最初と最後の部分の
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<TSource> Trim<TSource>(this IEnumerable<TSource> source)
        {
            List<TSource> list = source.ToList();

            if (typeof(TSource) == typeof(string))
            {
                while (list.Count > 0 && string.IsNullOrEmpty(list[0] as string))
                {
                    list.RemoveAt(0);
                }
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (string.IsNullOrEmpty(list[i] as string))
                    {
                        list.RemoveAt(i);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                while (list.Count > 0 && list[0] == null)
                {
                    list.RemoveAt(0);
                }
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i] == null)
                    {
                        list.RemoveAt(i);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return list;
        }

        public static TSource DefinedFirst<TSource>(this IEnumerable<TSource> source) where TSource : class
        {
            if (typeof(TSource) == typeof(string))
            {
                foreach (var item in source)
                {
                    if (!string.IsNullOrEmpty(item as string))
                    {
                        return item;
                    }
                }
            }
            else
            {
                foreach(var item in source)
                {
                    if(item != null)
                    {
                        return item;
                    }
                }
            }

            return null;
        }
    }
}
