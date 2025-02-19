using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Cors;
using System.Web.Http.Cors;

namespace GeoSit.RPI.Api.CorsPolicies
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class EnableCorsRPIAttribute : Attribute, ICorsPolicyProvider
    {
        private readonly CorsPolicy _policy;
        
        public EnableCorsRPIAttribute()
        {
            // Create a CORS policy.
            _policy = new CorsPolicy
            {
                AllowAnyMethod = true,
                AllowAnyHeader = true,
                SupportsCredentials = true
            };

            // Add allowed origins.
            _policy.Origins.Add(System.Configuration.ConfigurationManager.AppSettings["urlServerRPI"]);
            _policy.Origins.Add("https://localhost:44355");
        }

        public Task<CorsPolicy> GetCorsPolicyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_policy);
        }
    }
}