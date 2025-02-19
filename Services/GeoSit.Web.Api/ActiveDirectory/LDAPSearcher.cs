using Geosit.Core.Utils.Crypto;
using GeoSit.Data.DAL.Contexts;
using System;
using System.DirectoryServices;
using System.Linq;

namespace GeoSit.Web.Api.ActiveDirectory
{
    internal class LDAPSearcher
    {
        public enum SearchUserResult
        {
            NotFound,
            NotInGroup,
            Ok
        }
        readonly GeoSITMContext _context;
        public LDAPSearcher(GeoSITMContext context)
        {
            _context = context;
        }

        public Tuple<SearchUserResult, SearchResult> SearchUser(string user, string password)
        {
            try
            {
                var ldapConfig = RijndaelCypher.DecryptText(_context.ParametrosGenerales.Single(pg => pg.Clave == "LDAP_CONFIG").Valor).Split(new[] { "||" }, StringSplitOptions.None);
                string filter = $"(&(objectClass=user)(sAMAccountName={user}))";
                using (var adSearch = new DirectorySearcher(new DirectoryEntry($"LDAP://{ldapConfig[0]}", user, password), filter))
                {
                    string userADGroup = _context.ParametrosGenerales.Single(pg => pg.Clave == "Grupo_AD").Valor;
                    SearchResult searchResult;
                    try
                    {
                        searchResult = adSearch.FindOne();
                    }
                    catch (Exception ex)
                    {
                        Global.GetLogger().LogError("ADSearch", ex);
                        searchResult = null;
                    }
                    if (searchResult == null)
                    {
                        return Tuple.Create(SearchUserResult.NotFound, default(SearchResult));
                    }
                    if (!searchResult.Properties["MemberOf"].Cast<string>().SelectMany(grp => grp.Split(',').Select(elem => elem.Split('=')[1])).Any(value => value == userADGroup))
                    {
                        Global.GetLogger().LogInfo($"El usuario {user} no pertenece al grupo {userADGroup}.");
                        return Tuple.Create(SearchUserResult.NotInGroup, default(SearchResult));
                    }
                    return Tuple.Create(SearchUserResult.Ok, searchResult);
                }
            }
            catch (Exception)
            {
                Global.GetLogger().LogInfo($"El usuario {user} no existe o la contraseña es incorrecta.");
                return Tuple.Create(SearchUserResult.NotFound, default(SearchResult));
            }
        }
    }
}