using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NekoBot.Types;

public struct Version
{
    public required int Major;
    public required int Minor;
    public int Revision = 0;
    public int Build = 0;

    public Version()
    {
    }
    public bool Equals(Version ver)
    {
        return Major == ver.Major &&
               Minor == ver.Minor &&
               Revision == ver.Revision &&
               Build == ver.Build;
    }
    public override string ToString() => $"{Major}.{Minor}.{Revision}.{Build}";
    public static bool operator >(Version left, Version right)
    {
        return left.Major > right.Major ||
               left.Minor > right.Minor ||
               left.Revision > right.Revision ||
               left.Build > right.Build;
    }
    public static bool operator <(Version left, Version right)
    {
        return left.Major < right.Major ||
               left.Minor < right.Minor ||
               left.Revision < right.Revision ||
               left.Build < right.Build;
    }
    public static bool operator ==(Version left, Version right) => left.Equals(right);
    public static bool operator !=(Version left, Version right) => !left.Equals(right);

}
