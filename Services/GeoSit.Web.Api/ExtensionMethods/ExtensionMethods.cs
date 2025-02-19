using GeoSit.Data.DAL.Common;
using GeoSit.Web.Api.Ploteo;

namespace GeoSit.Web.Api.ExtensionMethods
{
    public static class ExtensionMethods
    {
        internal static string PlotearEstampilla(this UnitOfWork unitOfWork, long parcelaId)
        {
            var modPlot = new ModPlot(unitOfWork.PlantillaRepository, unitOfWork.LayerGrafRepository,
                                      unitOfWork.PlantillaFondoRepository, unitOfWork.HojaRepository,
                                      unitOfWork.NorteRepository, unitOfWork.ParcelaPlotRepository,
                                      unitOfWork.CuadraPlotRepository, unitOfWork.ManzanaPlotRepository,
                                      unitOfWork.CallePlotRepository, unitOfWork.ParametroRepository,
                                      unitOfWork.ImagenSatelitalRepository, unitOfWork.ExpansionPlotRepository,
                                      unitOfWork.TipoPlanoRepository, unitOfWork.PartidoRepository,
                                      unitOfWork.CensoRepository, unitOfWork.PloteoFrecuenteRepository,
                                      unitOfWork.PloteoFrecuenteEspecialRepository,
                                      unitOfWork.PlantillaViewportRepository, unitOfWork.TipoViewportRepository,
                                      unitOfWork.LayerViewportReposirory, unitOfWork.AtributoRepository,
                                      unitOfWork.ComponenteRepository);

            return modPlot.GetEstampilla(parcelaId);
        }
    }
}