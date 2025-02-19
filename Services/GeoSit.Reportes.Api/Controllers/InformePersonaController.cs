using GeoSit.Data.BusinessEntities.Personas;
using GeoSit.Data.BusinessEntities.Seguridad;
using GeoSit.Reportes.Api.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Web.Http;

namespace GeoSit.Reportes.Api.Controllers
{
    public class InformePersonaController : ApiController
    {
        public IHttpActionResult Get(long idPersona, string usuario)
        {
            using (var client = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiUrl"]) })
            {
                var response = client.GetAsync($"api/Persona/GetDatos?id={idPersona}").Result;
                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest();
                }
                return Ok(ReporteHelper.GenerarReporte(new Reportes.InformePersona(), response.Content.ReadAsAsync<Persona>().Result, usuario));
            }
        }

        [HttpGet]
        public IHttpActionResult GenerarInformeUsuarios(long id, bool tipoFiltro, string usuario, string filtro)
        {
            using (var client = new HttpClient() { BaseAddress = new Uri(ConfigurationManager.AppSettings["webApiUrl"]) })
            {
                HttpResponseMessage response;
                if (tipoFiltro) // Filtro por id_perfil
                {
                    response = client.GetAsync($"api/SeguridadService/GetUsuariosFiltrados/?idSector=&idPerfil={id}").Result;
                }
                else // Filtro por id_sector
                {
                    response = client.GetAsync($"api/SeguridadService/GetUsuariosFiltrados/?idSector={id}&idPerfil=").Result;
                }
                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest();
                }
                List<Usuarios> usuarios = response.Content.ReadAsAsync<List<Usuarios>>().Result.Where(u => u.Habilitado).OrderBy(u => u.Login).ToList();
                return Ok(ReporteHelper.GenerarInformeUsuarios(usuarios, filtro, usuario));
            }
        }
    }
}
