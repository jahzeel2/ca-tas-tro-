using GeoSit.SGT.Api.Models;
using SGTEntities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace GeoSit.SGT.Api.Helpers
{
    internal class SGTAPI
    {
        private static SGTAPI __instance;
        private static MemoryCache __cache;
        private const string AUTH_CACHE_KEY = "auth_cache_key";

        internal static SGTAPI Instance { get { return __instance ?? (__instance = new SGTAPI()); } }

        private readonly string _sgtApiURL;
        private readonly string _authApiUrl;
        private readonly string _authClientId;
        private readonly string _authUser;
        private readonly string _authPassword;
        private readonly HttpClientHandler __httpClientHandler;
        private SGTAPI()
        {
            __cache = new MemoryCache("__cache");
            __httpClientHandler = new HttpClientHandler();
            _sgtApiURL = ConfigurationManager.AppSettings["sgtApiUrl"];
            _authApiUrl = ConfigurationManager.AppSettings["authApiUrl"];
            _authClientId = ConfigurationManager.AppSettings["authClientId"];
            _authUser = ConfigurationManager.AppSettings["authUser"];
            _authPassword = ConfigurationManager.AppSettings["authPassword"];
        }

        private bool CacheValue(string key, object value, int secstolive = 3600)
        {
            return __cache.Add(key, value, DateTimeOffset.Now.AddSeconds(secstolive)); 
        }

        private T GetCachedValue<T>(string key)
        {
            return (T)Convert.ChangeType(__cache.Get(key), typeof(T));
        }

        private HttpClient GetHttpClient()
        {
            return new HttpClient(__httpClientHandler, false);
        }

        private async Task<SecurityToken> GetAuthToken()
        {
            var secToken = GetCachedValue<SecurityToken>(AUTH_CACHE_KEY);
            if (secToken == null)
            {
                var content = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "client_id", _authClientId  },
                    { "username", _authUser  },
                    { "password", _authPassword  }
                });

                var client = GetHttpClient();
                using (client)
                using (var resp = await client.PostAsync($"{_authApiUrl}/access_token", content))
                {
                    resp.EnsureSuccessStatusCode();
                    secToken = await resp.Content.ReadAsAsync<SecurityToken>();
                    CacheValue(AUTH_CACHE_KEY, secToken);
                }
            }
            return secToken;
        }

        private async Task<T> POST<T>(string method, HttpContent data)
        {
            var secToken = await GetAuthToken();
            var client = GetHttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(secToken.token_type, secToken.access_token);

            using (client)
            using (var resp = await client.PostAsync($"{_sgtApiURL}/{method}", data))
            {
                if(!resp.IsSuccessStatusCode && resp.StatusCode != System.Net.HttpStatusCode.BadRequest)
                {
                    resp.EnsureSuccessStatusCode();
                }
                return await resp.Content.ReadAsAsync<T>();
            }
        }

        internal async Task<InsertTramiteResponse> InsertTramite(Tramite tramite)
        {
            try
            {
                return await POST<InsertTramiteResponse>("InsertTramite", new ObjectContent<dynamic>(new { SDTTramiteIntegrado = tramite }, new JsonMediaTypeFormatter()));
            }
            catch (Exception ex)
            {
                return new InsertTramiteResponse()
                {
                    IsOkey = false,
                    mensaje = string.Join(Environment.NewLine, ex.Message)
                };
            }
        }

        internal async Task<TramiteResponse> InformarNovedad(NovedadTramite novedad)
        {
            try
            {
                return await POST<TramiteResponse>("InsertNovedadTramite", new ObjectContent<dynamic>(new { SdtNovedadTramite = novedad }, new JsonMediaTypeFormatter()));
            }
            catch (Exception ex)
            {
                return new InsertTramiteResponse()
                {
                    IsOkey = false,
                    mensaje = string.Join(Environment.NewLine, ex.Message)
                };
            }
        }

        internal async Task<TramiteResponse> InformarPases(List<PaseInternoTramite> pases)
        {
            var errores = new List<string>();
            foreach (var pase in pases)
            {
                try
                {
                    var ret = await POST<TramiteResponse>("MoverOficinaTramite", new ObjectContent<dynamic>(new { SdtMoverTramite = pase }, new JsonMediaTypeFormatter()));
                    if (ret?.IsOkey ?? false)
                    {
                        errores.Add($"ID SGT: {pase.TramiteId}\tError: {ret?.mensaje ?? "Ha ocurrido un error al registrar la novedad."}");
                    }
                }
                catch (Exception ex)
                {
                    errores.Add($"ID SGT: {pase.TramiteId}\tError: {ex.Message}");
                }
            }
            return new TramiteResponse()
            {
                IsOkey = !errores.Any(),
                mensaje = string.Join(Environment.NewLine, errores)
            };
        }

        internal async Task<TramiteResponse> ArchivarTramite(FinalizacionTramite motivo)
        {
            try
            {
                return await POST<TramiteResponse>("ArchivarTramite", new ObjectContent<dynamic>(new { SdtRetornarGestionSGT = motivo }, new JsonMediaTypeFormatter()));
            }
            catch (Exception ex)
            {
                return new InsertTramiteResponse()
                {
                    IsOkey = false,
                    mensaje = string.Join(Environment.NewLine, ex.Message)
                };
            }
        }
    }
}