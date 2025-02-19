using GeoSit.Data.BusinessEntities.MesaEntradas;
using GeoSit.Data.BusinessEntities.Seguridad;
using SGTEntities.Interfaces;
using System;

namespace SGTEntities
{
    public class NovedadTramite
    {
        public int TramiteId { get; set; }
        public int SistemaExternoId { get { return 1; } }
        public string OperadorCuil { get; set; }
        public string OperadorNombre { get; set; }
        public string OperadorApellido { get; set; }
        public string TramiteRutaAgenteTexto { get; set; }

        internal NovedadTramite() { }

        private NovedadTramite(int tramiteId, string operadorNombre, string operadorApellido, string operadorCUIL, string novedad) 
        {
            TramiteId = tramiteId;
            OperadorApellido = operadorApellido;
            OperadorCuil = operadorCUIL;
            OperadorNombre = operadorNombre;
            TramiteRutaAgenteTexto = novedad;
        }

        public static NovedadTramite Create(METramite tramite, Usuarios operador, ITextGenerator<METramite> generador)
        {
            if (!tramite.IdSGT.HasValue) throw new ArgumentNullException("IdSGT");
            return new NovedadTramite(tramite.IdSGT.Value, operador.Nombre, operador.Apellido, operador.CUIL, generador.Generate(tramite));
        }
    }
}
