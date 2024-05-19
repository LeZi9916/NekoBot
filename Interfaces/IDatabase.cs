using System;
using System.Collections;
using System.Collections.Generic;

namespace NekoBot.Interfaces
{
    public interface IDatabase<T>: IExtension, IList<T>
    {         
        void SetAll(T[] collection);
        T[] All();
        T? Find(Predicate<T> match);
        List<T> FindAll(Predicate<T> match);
        int FindIndex(Predicate<T> match);
        T? FindLast(Predicate<T> match);
        int FindLastIndex(Predicate<T> match);
        bool Exists(Predicate<T> match);
        bool Update(Predicate<T> match, T item);
    }
}
