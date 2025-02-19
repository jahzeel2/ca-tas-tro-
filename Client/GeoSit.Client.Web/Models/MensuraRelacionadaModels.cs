namespace GeoSit.Client.Web.Models
{
    public class MensuraRelacionadaModels
    {
        public long IdMensuraRelacionada { get; set; }
        public long? IdMensuraOrigen { get; set; }
        public long? IdMensuraDestino { get; set; }
        public string MensuraDescripcion { get; set; }
    }
}