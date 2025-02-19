using System;

namespace GeoSit.Client.Web.Models
{
    public class MensuraDocumentoModels
    {
        public long IdMensuraDocumento { get; set; }
        public long IdMensura { get; set; }
        public long IdDocumento { get; set; }
        public string Nombre { get; set; }
        public string Extension { get; set; }
        public string Tipo { get; set; }
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; }
    }
}