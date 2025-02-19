using System;

namespace GeoSit.Data.DAL.Common
{
    public class TextRandomizer
    {
        public static string RandomizeGenericPattern(int length)
        {
            return Randomize($"[a-zA-Z0-9]{{{length}}}");
        }
        public static string Randomize(string pattern)
        {
            Fare.Xeger xeger = new Fare.Xeger(pattern, new Random(0));
            return xeger.Generate();
        }
    }
}
