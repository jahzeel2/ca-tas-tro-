namespace GeoSit.Client.Web.Models
{
    public class TipoProfesionModels
    {
        public TipoProfesionModels()
        {
            TiposProfesiones = new TipoProfesioneModel();
        }
        public TipoProfesioneModel TiposProfesiones { get; set; }
        public string Mensaje { get; set; }
    }

    public class TipoProfesioneModel
    {
        public long TipoProfesionId { get; set; }
        public string Descripcion { get; set; }
    }
}