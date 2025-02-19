using GeoSit.Data.BusinessEntities.MesaEntradas;
using static GeoSit.Data.BusinessEntities.Common.Enumerators;
using System.Linq;

namespace GeoSit.Client.Web.ViewModels
{
    public class ResumenVisadosViewModel
    {
        public bool VisadoDominioCompleto { get; private set; }
        public bool VisadoCatastroCompleto { get; private set; }
        public bool VisadoGraficoCompleto { get; private set; }
        public bool VisadoTopograficoCompleto { get; private set; }
        public bool VisadoValuatorioCompleto { get; private set; }
        public bool Aprobado { get; private set; }

        private ResumenVisadosViewModel(bool visadoDominioCompleto, bool visadoCatastroCompleto, bool visadoGraficoCompleto, bool visadoTopograficoCompleto, bool visadoValuatorioCompleto, bool aprobado)
        {
            VisadoDominioCompleto = visadoDominioCompleto;
            VisadoCatastroCompleto = visadoCatastroCompleto;
            VisadoGraficoCompleto = visadoGraficoCompleto;
            VisadoTopograficoCompleto = visadoTopograficoCompleto;
            VisadoValuatorioCompleto = visadoValuatorioCompleto;
            Aprobado = aprobado;
        }

        internal static ResumenVisadosViewModel Create(METramite tramite)
        {

            return new ResumenVisadosViewModel(ExisteMovimiento(tramite, EnumTipoMovimiento.VisarDiminios),
                                               ExisteMovimiento(tramite, EnumTipoMovimiento.VisarCatastro),
                                               ExisteMovimiento(tramite, EnumTipoMovimiento.VisarGraficos),
                                               ExisteMovimiento(tramite, EnumTipoMovimiento.VisarTopografia),
                                               ExisteMovimiento(tramite, EnumTipoMovimiento.VisarValuaciones),
                                               ExisteMovimiento(tramite, EnumTipoMovimiento.AprobarGeneral));
        }

        private static bool ExisteMovimiento(METramite tramite, EnumTipoMovimiento tipoMovimiento)
        {
            return tramite.Movimientos.Any(m=>(EnumTipoMovimiento)m.IdTipoMovimiento == tipoMovimiento);
        }
    }
}