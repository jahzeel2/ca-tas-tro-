using GeoSit.Data.BusinessEntities.MesaEntradas;
using GeoSit.Data.BusinessEntities.Seguridad;
using SGTEntities.Interfaces;
using System;

namespace SGTEntities
{
    public class FinalizacionTramite
    {
        public int TramiteId { get; set; }
        public int SistemaExternoId { get { return 1; } }
        public string OperadorCuil { get; set; }
        public string OperadorNombre { get; set; }
        public string OperadorApellido { get; set; }
        public string TramiteRutaMotivoPase { get; set; }

        internal FinalizacionTramite() { }

        private FinalizacionTramite(int tramiteId, string operadorNombre, string operadorApellido, string operadorCUIL, string motivo) 
        {
            TramiteId = tramiteId;
            OperadorApellido = operadorApellido;
            OperadorCuil = operadorCUIL;
            OperadorNombre = operadorNombre;
            TramiteRutaMotivoPase = motivo;
        }

        public static FinalizacionTramite Create(METramite tramite, Usuarios operador, ITextGenerator<METramite> generador)
        {
            if (!tramite.IdSGT.HasValue) throw new ArgumentNullException("IdSGT");
            return new FinalizacionTramite(tramite.IdSGT.Value, operador.Nombre, operador.Apellido, operador.CUIL, generador.Generate(tramite));
        }
    }
}
