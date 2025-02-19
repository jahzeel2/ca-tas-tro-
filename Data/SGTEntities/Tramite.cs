using GeoSit.Data.BusinessEntities.MesaEntradas;
using GeoSit.Data.BusinessEntities.Seguridad;
using SGTEntities.Interfaces;
using System.Linq;

namespace SGTEntities
{
    public class Tramite
    {
        public int SistemaExternoId { get { return 1; } }
        public int TramiteJurisdiccionId { get { return 3; } }
        public int DependenciaOrigenCuof { get; set; }
        public int DependenciaOrigenAnexo { get { return 0; } }
        public int DependenciaOrigenUngi { get { return 0; } }
        public string TipoTramiteCodigo { get { return "Ae"; } }
        public bool TramiteReservado { get { return false; } }
        public int AsuntoId { get; set; }
        public int AsuntoCausaId { get; set; }
        public string TramiteExtracto { get { return ""; } }
        public int IniciadorTramitePersonaTipo { get { return 1; } }
        public string IniciadorTramitePersonaNombre { get; set; }
        public string IniciadorTramitePersonaApeRazonSoc { get; set; }
        public int IniciadorTramitePersonaDocPrefijo { get; set; }
        public int IniciadorTramitePersonaDocNro { get; set; }
        public int IniciadorTramitePersonaDocDigito { get; set; }
        public int IniciadorPublicoPrivado { get { return 2; } }
        public int IniciadorPrivadoTipo { get { return 1; } }
        public int CargoIniciadorJurisdiccionId { get; set; }
        public int CargoIniciadorOficinaId { get { return 0; } }
        public int CargoIniciadorOficinaAnexo { get { return 0; } }
        public string IniciadorEmpresaNombre { get { return ""; } }
        public int IniciadorEmpresaCuit { get { return 0; } }
        public int EstadoTramiteId { get { return 1; } }
        public int TramitePrioridad { get; set; }
        public string TramiteNumeroRelacionado { get; set; }
        public long OperadorCuil { get; set; }
        public string OperadorNombre { get; set; }
        public string OperadorApellido { get; set; }
        public EParte[] TramiteEPartes { get; set; }

        internal Tramite() { }

        private Tramite(string idTramite, int asunto, int causa, int prioridad, string iniciadorNombre, string iniciadorApellido, 
                        int iniciadorCuilPrefijo, int iniciadorCuilDocumento, int iniciadorCuilDigito, string operadorNombre, 
                        string operadorApellido, long operadorCuil, int cuof, EParte eparte)
        {
            AsuntoId = asunto;
            AsuntoCausaId = causa;
            DependenciaOrigenCuof = cuof;
            IniciadorTramitePersonaNombre = iniciadorNombre;
            IniciadorTramitePersonaApeRazonSoc = iniciadorApellido;
            IniciadorTramitePersonaDocPrefijo = iniciadorCuilPrefijo;
            IniciadorTramitePersonaDocNro = iniciadorCuilDocumento;
            IniciadorTramitePersonaDocDigito = iniciadorCuilDigito;
            TramitePrioridad = prioridad;
            TramiteNumeroRelacionado = idTramite;
            OperadorNombre = operadorNombre;
            OperadorApellido = operadorApellido;
            OperadorCuil = operadorCuil;
            TramiteEPartes = new[] { eparte };
        }

        public static Tramite Create(METramite tramite, Usuarios profesional, Sector sectorGestionInterna, ITextGenerator<METramite> generador)
        {
            return new Tramite(tramite.IdTramite.ToString(), tramite.Tipo.IdSistemaExterno, tramite.Objeto.IdSistemaExterno, tramite.IdPrioridad,
                               profesional.Nombre, profesional.Apellido,
                               int.Parse(profesional.CUIL.Substring(0, 2)), int.Parse(profesional.CUIL.Substring(2, profesional.CUIL.Length - 3)), int.Parse(profesional.CUIL.Last().ToString()),
                               profesional.Nombre, profesional.Apellido, long.Parse(profesional.CUIL), sectorGestionInterna.CUOF,
                               EParte.Create("Carátula y Orígenes", 1, generador.Generate(tramite)));
        }
    }
}
