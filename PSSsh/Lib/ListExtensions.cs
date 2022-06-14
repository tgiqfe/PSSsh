using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSSsh.Lib
{
    internal static class ListExtensions
    {
        public static void Trim<T>(this List<T> list)
        {
            if (typeof(T) == typeof(string))
            {
                while (list.Count > 0 && string.IsNullOrEmpty(list[0] as string))
                {
                    list.RemoveAt(0);
                }
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (string.IsNullOrEmpty(list[i] as string))
                    {
                        list.RemoveAt(0);
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
                        list.RemoveAt(0);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
}
