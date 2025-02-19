using GeoSit.Data.BusinessEntities.MesaEntradas;

namespace GeoSit.Data.BusinessEntities.Common
{
    public class METramiteParameters
    {
        public METramite Tramite { get; set; }
        public METramiteDocumento[] TramitesDocumentos { get; set; }
        public MEDatosEspecificos[] DatosOrigen { get; set; }
        public MEDatosEspecificos[] DatosDestino { get; set; }
        //public bool Ingresar { get; set; }
        public int TipoMovimiento { get; set; }
    }
}
