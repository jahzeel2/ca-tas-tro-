using GeoSit.Data.BusinessEntities.MesaEntradas;
using GeoSit.Data.BusinessEntities.MesaEntradas.DTO;
using SGTEntities.Interfaces;
using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Formatting;

namespace GeoSit.Data.DAL.Models
{
    internal class GeneradorResumenTramite : ITextGenerator<METramite>
    {
        private readonly MEDatosEspecificos[] datosOrigen;

        public GeneradorResumenTramite(MEDatosEspecificos[] datosOrigen)
        {
            this.datosOrigen = datosOrigen;
        }

        public string Generate(METramite tramite)
        {
            var httpContent = new ObjectContent<ResumenTramite>(ResumenTramite.Create(tramite, datosOrigen), new JsonMediaTypeFormatter());
            using (var httpClient = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiReportesURL"]) })
            using (var resp = httpClient.PostAsync("api/InformeTramiteResumen/Post", httpContent).Result)
            {
                resp.EnsureSuccessStatusCode();
                return $"data:application/pdf;base64,{resp.Content.ReadAsAsync<string>().Result}";
            }
        }
    }
}
