using GeoSit.Data.BusinessEntities.LogicalTransactionUnits;
using GeoSit.Data.DAL.OperacionesParcelarias;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class OperacionParcelaria
    {
        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        public async Task Test_Should_Return_Operacion_Valida(int operacion)
        {
            var uap = new UnidadAlfanumericoParcela()
            {
                Operacion = operacion
            };
            var result = Generator.Create(uap);
            Assert.IsNotNull(result.Item1);
            Assert.IsNull(result.Item2);
        }

        [TestMethod]
        public async Task Test_Should_Return_Operacion_Invalida()
        {
            var uap = new UnidadAlfanumericoParcela()
            {
                Operacion = 7
            };
            var result = Generator.Create(uap);
            Assert.IsNull(result.Item1);
            Assert.IsNotNull(result.Item2);
        }
    }
}
