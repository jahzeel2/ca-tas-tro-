using GeoSit.Data.BusinessEntities.MesaEntradas;
using GeoSit.Data.BusinessEntities.MesaEntradas.DTO;
using SGTEntities.Interfaces;
using System;

namespace GeoSit.Data.DAL.Models
{
    public class GeneradorObservacionTramite : ITextGenerator<METramite>
    {
        private readonly Observacion observacion;
        internal GeneradorObservacionTramite(Observacion observacion)
        {
            this.observacion = observacion;
        }
        public string Generate(METramite tramite)
        {
            return string.Format("El trámite {0} ha sido devuelto al profesional.{1}{1}Motivo: {2}",
                                 tramite.Numero, Environment.NewLine, observacion.Motivo);
        }
    }
}
