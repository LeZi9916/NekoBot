using System;
using System.Collections;
using System.Collections.Generic;

namespace NekoBot.Interfaces
{
    public interface IDatabase<T>: IExtension, IList<T>
    {         
        void Add(T item);
        bool Remove(T item);
        void SetAll(T[] collection);
        T[] All();
        List<T> FindAll(Predicate<T> match);
        int FindIndex(Predicate<T> match);
        T? FindLast(Predicate<T> match);
        int FindLastIndex(Predicate<T> match);
    }
}
