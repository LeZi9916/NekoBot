using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NekoBot.Interfaces;
public interface IDatabase<T> : IEnumerable<T>, IEnumerable, IDestroyable
{
    int Count { get; }
    void Import(T[] collection);
    T[] Export();
    T? Find(Expression<Func<T, bool>> match);
    List<T> FindAll(Expression<Func<T, bool>> match);
    T? FindLast(Expression<Func<T, bool>> match);
    bool Exists(Expression<Func<T, bool>> match);
    bool Update(Expression<Func<T, bool>> match, T item);

    void Insert(T item, Expression<Func<T, bool>>? match = null);
    void Clear();
    bool Remove(Expression<Func<T, bool>> match);
}
