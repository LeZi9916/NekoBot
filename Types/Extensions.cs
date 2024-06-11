using MongoDB.Driver;
using System.Linq;
using System.Threading.Tasks;

namespace NekoBot.Types;
public static class IFindFluentExtensions
{
    public static T[] ToArray<T>(this IFindFluent<T, T> source)
    {
        return source.ToList().ToArray();
    }
    public static async Task<T[]> ToArrayAsync<T>(this IFindFluent<T, T> source)
    {
        return (await source.ToListAsync()).ToArray();
    }
    public static T? Last<T>(this IFindFluent<T, T> source)
    {
        return source.ToList().Last();
    }
    public static T? LastOrDefault<T>(this IFindFluent<T, T> source)
    {
        return source.ToList().LastOrDefault();
    }
}
