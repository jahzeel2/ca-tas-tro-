using System;

namespace GeoSit.Client.Web.Models
{
    public class PersonaModels
    {
        public PersonaModels()
        {
            DatosPersona = new PersonaModel();
            Mensaje = "";
            TextoBusqueda = "";
        }
        public PersonaModel DatosPersona { get; set; }
        public string Mensaje { get; set; }
        public string TextoBusqueda { get; set; }
    }

    public class PersonaModel
    {
        public long PersonaId { get; set; }
        public long TipoDocId { get; set; }
        public string TipoDocumento { get; set; }
        public long IdIniciador { get; set; }
        public string Iniciador { get; set; }
        public string NroDocumento { get; set; }
        public long TipoPersonaId { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public long? Sexo { get; set; }
        public long? EstadoCivil { get; set; }
        public long? Nacionalidad { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public string CUIL { get; set; }
    }
}