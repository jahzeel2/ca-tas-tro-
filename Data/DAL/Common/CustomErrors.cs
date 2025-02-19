namespace GeoSit.Data.DAL.Common.CustomErrors
{
    public interface ICustomError
    {
        string Error { get; }
    };
}
namespace GeoSit.Data.DAL.Common.CustomErrors.UnidadesTributarias
{
    public class SinDDJJVigente : ICustomError
    {
        public string Error { get { return "SinDDJJVigente"; } }
    }

    public class SinDominiosSinSuperficie : ICustomError
    {
        public string Error { get { return "SinDominiosSinSuperficie"; } }
    }

}
namespace GeoSit.Data.DAL.Common.CustomErrors.Parcelas
{
    public class GraficoNoValidoParaAlfa : ICustomError
    {
        public string Error { get { return "GraficoNoValidoParaAlfa"; } }
    }

    public class DatosNoVigentes : ICustomError
    {
        public string Error { get { return "DatosNoVigentes"; } }
    }

    public class ErrorActualizacionVistaMaterializada : ICustomError
    {
        public string Error { get { return "ErrorActualizacionVistaMaterializada"; } }
    }

    public class PartidaInvalida : ICustomError
    {
        public string Error { get { return "La partida no es válida."; } }
    }

    public class PartidaInvalidaParaParcela : ICustomError
    {
        public string Error { get { return "La partida no es válida para la parcela."; } }
    }
}
namespace GeoSit.Data.DAL.Common.CustomErrors.OperacionesParcelarias
{
    public class OperacionParcelariaInvalida : ICustomError
    {
        public string Error { get { return "OperacionParcelariaInvalida"; } }
    }
    public class ErrorFormatoNumeroPlano : ICustomError
    {
        public string Error { get { return "El Número de Plano no tiene el formato correcto."; } }
    }
    public class ErrorBajaGrafico : ICustomError
    {
        public string Error { get { return "Error al dar de baja un gráfico de parcela."; } }
    }
    public class ErrorOperacionParcelaria : ICustomError
    {
        public string Error { get { return "Error al grabar la operación parcelaria."; } }
    }
}
namespace GeoSit.Data.DAL.Common.CustomErrors.Nomenclaturas
{
    public class NomenclaturaRuralParaParcelaUrbana : ICustomError
    {
        public string Error { get { return "La nomenclatura especificada no se corresponde con una parcela URBANA."; } }
    }
    public class NomenclaturaUrbanaParaParcelaRural : ICustomError
    {
        public string Error { get { return "La nomenclatura especificada no se corresponde con una parcela RURAL."; } }
    }
    public class NomenclaturaIncompleta : ICustomError
    {
        public string Error { get { return "La nomenclatura especificada no está completa o no tiene el formato válido."; } }
    }
    public class NomenclaturaExistente : ICustomError
    {
        public string Error { get { return "La nomenclatura especificada ya está asociada a otra parcela."; } }
    }
    public class CircunscripcionNoPertenecienteDepartamento : ICustomError
    {
        public string Error { get { return "La circunscripción especificada no pertenece al departamento indicado."; } }
    }
    public class SeccionNoPertenecienteCircunscripcion : ICustomError
    {
        public string Error { get { return "La sección especificada no pertenece a la circunscripción indicada."; } }
    }
}
