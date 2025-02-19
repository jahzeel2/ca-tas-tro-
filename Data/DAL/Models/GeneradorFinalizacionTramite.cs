using GeoSit.Data.BusinessEntities.MesaEntradas;
using GeoSit.Data.BusinessEntities.MesaEntradas.DTO;
using SGTEntities.Interfaces;
using System;

namespace GeoSit.Data.DAL.Models
{
    public class GeneradorFinalizacionTramite : ITextGenerator<METramite>
    {
        private readonly Observacion observacion;
        internal GeneradorFinalizacionTramite(Observacion observacion)
        {
            this.observacion = observacion;
        }
        public string Generate(METramite tramite)
        {
            return string.Format("El trámite {0} ha sido archivado.{1}{1}Motivo: {2}",
                                 tramite.Numero, Environment.NewLine, observacion.Motivo);
        }
    }
}
