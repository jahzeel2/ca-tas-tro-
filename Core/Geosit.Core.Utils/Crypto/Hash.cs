using System.Linq;
using System.Text;
using CRYPTO = System.Security.Cryptography;

namespace GeoSit.Core.Utils.Crypto
{
    public class Hash
    {
        public static string MD5(string text)
        {
            using (var md5 = CRYPTO.HashAlgorithm.Create(nameof(CRYPTO.MD5)))
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
                return string.Join(string.Empty, hash.Select(x => x.ToString("x2")));
            }
        }
    }
}
