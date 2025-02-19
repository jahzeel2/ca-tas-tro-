using GeoSit.Data.BusinessEntities.MesaEntradas;
using GeoSit.Data.BusinessEntities.Seguridad;
using System;

namespace SGTEntities
{
    public class PaseInternoTramite
    {
        public int TramiteId { get; set; }
        public int SistemaExternoId { get { return 1; } }
        public string OperadorCuil { get; set; }
        public string OperadorNombre { get; set; }
        public string OperadorApellido { get; set; }
        public int TramiteRutaJurisdiccionDestinoId { get { return 3; } }
        public int TramiteRutaDependenciaDestinoCuof { get; set; }
        public int TramiteRutaDependenciaDestinoAnexo { get { return 0; } }
        public int TramiteRutaDependenciaDestinoGestion { get { return 0; } }

        internal PaseInternoTramite() { }

        private PaseInternoTramite(int tramiteId, string operadorNombre, string operadorApellido, string operadorCUIL, int cuof)
        {
            TramiteId = tramiteId;
            OperadorApellido = operadorApellido;
            OperadorCuil = operadorCUIL;
            OperadorNombre = operadorNombre;
            TramiteRutaDependenciaDestinoCuof = cuof;
        }

        public static PaseInternoTramite Create(METramite tramite, Sector destino, Usuarios operador)
        {
            if (!tramite.IdSGT.HasValue) throw new ArgumentNullException("IdSGT");
            return new PaseInternoTramite(tramite.IdSGT.Value, operador.Nombre, operador.Apellido, operador.CUIL, destino.CUOF);
        }
    }
}
