using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nova.Core.Utils
{
    public class EnumerableNavigation<T,K>
    {
        public EnumerableNavigation(IEnumerable<T> items, K key, Func<T,K> keySelector)
        {
            if(items != null && items.Any() && keySelector != null)
            {
                First = items.First();
                Last = items.Last();

                foreach (T item in items)
                {
                    if (Current != null)
                    {
                        Next = item;
                        break;
                    }
                    if (key.Equals(keySelector(item)))
                    {
                        Current = item;
                    }
                    if (Current == null)
                    {
                        Previous = item;
                    }  
                }
                if(Current == null)
                {
                    Previous = default(T);
                }
            }
        }

        public T First { get; private set; }
        public T Previous { get; private set; }
        public T Current { get; private set; }
        public T Next { get; private set; }
        public T Last { get; private set; }

    }
}
