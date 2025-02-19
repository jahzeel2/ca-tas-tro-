using GeoSit.Data.BusinessEntities.ObjetosAdministrativos;

namespace GeoSit.Data.BusinessEntities.Personas
{
    public class Profesion : IEntity
    {
        public long PersonaId { get; set; }
        public long TipoProfesionId { get; set; }
        public string Matricula { get; set; }
        public TipoProfesion TipoProfesion { get; set; }
    }
}
