using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DbDiver
{
    public static class CollectionExtension
    {
        public static string Join(this IEnumerable<string> strs, char ch)
        {
            StringBuilder builder = new StringBuilder();
            
            using(var enumerator = strs.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    builder.Append(enumerator.Current);

                    while (enumerator.MoveNext())
                    {
                        builder.Append(ch);
                        builder.Append(enumerator.Current);
                    }
                }
            }

            return builder.ToString();
        }
    }
}
