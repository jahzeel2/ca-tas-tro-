using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.DAL.Valuaciones;
using GeoSit.Data.DAL.Valuaciones.Computations;
using GeoSit.Data.DAL.Valuaciones.Validators;
using GeoSit.Data.DAL.Valuaciones.Validators.Validations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class DeclaracionesJuradas
    {
        private readonly IDDJJValidator _validator = new ValidadorDDJJSiempreValido();

        [TestMethod]
        public async Task Test_Should_Return_ValuacionGenerada()
        {
            var generador = new Generator(new TierraRuralComputation(GeoSITMContext.CreateContext()), _validator);

            var valuacion = await generador.Generate();

            Assert.IsNotNull(valuacion);
        }

        [TestMethod]
        public async Task Test_Should_Return_ValorTotal_IsEqualsTo_Zero()
        {
            var generador = new Generator(new TierraRuralComputation(GeoSITMContext.CreateContext()), _validator);

            var valuacion = await generador.Generate();

            Assert.AreEqual(valuacion.ValorTotal, 0m);
        }

        [TestMethod]
        public async Task Test_Should_Return_ValorTierra_IsEqualsTo_Zero()
        {
            var generador = new Generator(new TierraRuralComputation(GeoSITMContext.CreateContext()),_validator);

            var valuacion = await generador.Generate();

            Assert.AreEqual(valuacion.ValorTierra, 0m);
        }

        [TestMethod]
        public async Task Test_Should_Return_ValorMejoras_IsEqualTo_Zero()
        {
            var generador = new Generator(new TierraRuralComputation(GeoSITMContext.CreateContext()), _validator);

            var valuacion = await generador.Generate();

            Assert.AreEqual(0m, valuacion.ValorMejoras);
        }

        [TestMethod]
        public async Task Test_Should_Return_ValorTotal_EqualsTo_SumOf_ValorTierra_And_ValorMejoras()
        {
            var generador = new Generator(new TierraRuralComputation(GeoSITMContext.CreateContext()),_validator);
            var valuacion = await generador.Generate();

            Assert.AreEqual(valuacion.ValorTierra + valuacion.ValorMejoras, valuacion.ValorTotal);
        }

        [TestMethod]
        public async Task Test_Should_Throw_InvalidOperationException_When_FechaVigencia_Is_Missing()
        {
            var validator = new DDJJValidator(new DDJJ()).Validate(new FechaVigenciaValidation());
            var generador = new Generator(new TierraRuralComputation(GeoSITMContext.CreateContext()), validator);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => generador.Generate());
        }

        [TestMethod]
        public async Task Test_Should_Throw_InvalidOperationException_When_Superficies_Are_Missing()
        {
            var validator = new DDJJValidator(new DDJJ()).Validate(new SuperficiesValidation());
            var generador = new Generator(new TierraRuralComputation(GeoSITMContext.CreateContext()), validator);

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => generador.Generate());
        }

        [TestMethod]
        public async Task Test_Should_Throw_InvalidOperationException_When_Caracteristicas_Are_Missing()
        {
            using (var ctx = GeoSITMContext.CreateContext())
            {
                var validator = new DDJJValidator(new DDJJ()).Validate(new CaracteristicasValidation(ctx));
                var generador = new Generator(new TierraRuralComputation(ctx), validator);

                await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => generador.Generate());
            }
        }

        [TestMethod]
        public async Task Test_Should_Throw_InvalidOperationException_When_Caracteristicas_Are_Wrong()
        {
            using(var ctx = GeoSITMContext.CreateContext())
            {
                var superficies = new VALSuperficie[]
                {
                    new VALSuperficie
                    {
                        IdAptitud = 27,
                        Superficie = 99.9175,
                        Caracteristicas = new []
                        {
                            new DDJJSorCar { IdSorCar = 35 },
                            new DDJJSorCar { IdSorCar = 44 },
                            new DDJJSorCar { IdSorCar = 94 },
                            new DDJJSorCar { IdSorCar = 59 },
                            new DDJJSorCar { IdSorCar = 65 },
                            new DDJJSorCar { IdSorCar = 87 },
                            new DDJJSorCar { IdSorCar = 91 },
                            new DDJJSorCar { IdSorCar = 83 },
                            new DDJJSorCar { IdSorCar = 41 },
                            new DDJJSorCar { IdSorCar = 40 },
                        }
                    },
                };

                var ddjj = new DDJJ()
                {
                    Sor = new DDJJSor()
                    {
                        Superficies = superficies
                    }
                };
                var validator = new DDJJValidator(ddjj).Validate(new CaracteristicasValidation(ctx));

                var generador = new Generator(new TierraRuralComputation(ctx), validator);

                await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => generador.Generate());
                Assert.IsTrue(validator.Errors.Contains("Hay características del código de terreno Acueducto(99.9175) que no se han especificado."));
                Assert.IsTrue(validator.Errors.Contains("Hay características del código de terreno Acueducto(99.9175) que tienen más de una opción configurada."));
                Assert.IsTrue(validator.Errors.Contains("Hay características configuradas en el código de terreno Acueducto(99.9175) que no pertenecen al código de terreno especificado."));
            }
        }

        [TestMethod]
        public async Task Test_Should_Return_Valuacion_Valida()
        {
            var ddjj = new DDJJ()
            {
                IdUnidadTributaria = 33465,
                FechaVigencia = new DateTime(1995, 12, 31),
                Sor = new DDJJSor()
                {
                    Superficies = new[]
                                {
                                    new VALSuperficie
                                    {
                                        IdAptitud = 18,
                                        Superficie = 99.9175,
                                        Caracteristicas = new []
                                        {
                                            new DDJJSorCar { IdSorCar = 77 }, 
                                            new DDJJSorCar { IdSorCar = 79 },
                                            new DDJJSorCar { IdSorCar = 82 },
                                            new DDJJSorCar { IdSorCar = 87 },
                                            new DDJJSorCar { IdSorCar = 92 },
                                            new DDJJSorCar { IdSorCar = 75 },
                                        }
                                    },
                                }
                }
            };

            /*  
                |caracteristica             |valor         | id|puntaje|
                |---------------------------|--------------|---|-------|
                | ACCESO                    |DIRECTO       | 77|      5|
                | ESTADO CAMINO             |BUENO         | 79|      5|
                | DISTANCIA A RUTAS         |SOBRE CAMINO  | 82|      7|
                | DISTANCIA A CENTRO URBANO |HASTA 5 KM.   | 87|      7|
                | ELECTRIFICACIÓN           |HAY           | 92|      4|
                | LAGUNA                    |AGUA DULCE    | 75|     10|
                |---------------------------|--------------|---|     38|
            */

            int puntajeTotalEsperado = 38;
            decimal superficieTotalEsperada = 99_9175m;
            decimal valorBasico = 2204.89m;
            decimal puntajeSuperficie = CalculadorPuntajeSuperficie
                                            .CalcularPuntajeSuperficie(puntajeTotalEsperado, ddjj.Sor.Superficies.Single());

            using (var ctx = GeoSITMContext.CreateContext())
            using (var transaction = ctx.Database.BeginTransaction())
            {
                var validator = new DDJJValidator(ddjj)
                                        .Validate(new FechaVigenciaValidation())
                                        .Validate(new SuperficiesValidation())
                                        .Validate(new CaracteristicasValidation(ctx));

                var generador = new Generator(new TierraRuralComputation(ctx), validator);

                var result = await generador.SimulateAsync();

                Assert.IsNotNull(result);
                Assert.AreEqual(result.SuperficieValuadaMT2, superficieTotalEsperada);
                Assert.AreEqual(result.PuntajeValuado, puntajeSuperficie);
                Assert.AreEqual(result.ValorOptimo, valorBasico);
            }
        }

        [DataTestMethod]
        [DataRow(1,2)]
        [DataRow(2,2)]
        [DataRow(3,5)]
        [DataRow(4,4)]
        [DataRow(5,2)]
        [DataRow(6,3)]
        public async Task Test_Should_Return_Valuacion_Depreciable_Valida(int trazaDepreciable, int porcentajeDepreciacion)
        {
            var ddjj = new DDJJ()
            {
                IdUnidadTributaria = 33465,
                FechaVigencia = new DateTime(1995, 12, 31),
                Sor = new DDJJSor()
                {
                    Superficies = new[]
                                {
                                    new VALSuperficie
                                    {
                                        IdAptitud = 18,
                                        Superficie = 90.9175,
                                        Caracteristicas = new []
                                        {
                                            new DDJJSorCar { IdSorCar = 77 }, 
                                            new DDJJSorCar { IdSorCar = 79 },
                                            new DDJJSorCar { IdSorCar = 82 },
                                            new DDJJSorCar { IdSorCar = 87 },
                                            new DDJJSorCar { IdSorCar = 92 },
                                            new DDJJSorCar { IdSorCar = 75 },
                                        }
                                    },
                                    new VALSuperficie
                                    {
                                        IdAptitud = 18,
                                        Superficie = 9,
                                        Caracteristicas = new []
                                        {
                                            new DDJJSorCar { IdSorCar = 77 }, 
                                            new DDJJSorCar { IdSorCar = 79 },
                                            new DDJJSorCar { IdSorCar = 82 },
                                            new DDJJSorCar { IdSorCar = 87 },
                                            new DDJJSorCar { IdSorCar = 92 },
                                            new DDJJSorCar { IdSorCar = 75 },
                                        },
                                        TrazaDepreciable = (short)trazaDepreciable
                                    },
                                }
                }
            };

            /*  
                laguna no tiene depreciacion en puntaje por caracteristicas

                |caracteristica             |valor         | id|puntaje|
                |---------------------------|--------------|---|-------|
                | ACCESO                    |DIRECTO       | 77|      5|
                | ESTADO CAMINO             |BUENO         | 79|      5|
                | DISTANCIA A RUTAS         |SOBRE CAMINO  | 82|      7|
                | DISTANCIA A CENTRO URBANO |HASTA 5 KM.   | 87|      7|
                | ELECTRIFICACIÓN           |HAY           | 92|      4|
                | LAGUNA                    |AGUA DULCE    | 75|     10| 
                |---------------------------|--------------|---|     38|
            */

            decimal superficieTotalEsperada = 99_9175m;
            int idx = 0;
            decimal puntajeSuperficieTotal = new[] { 38, 38 }.Aggregate(0m, (accum, puntaje) =>
            {
                return accum + CalculadorPuntajeSuperficie.CalcularPuntajeSuperficie(puntaje, ddjj.Sor.Superficies.ElementAt(idx++));
            });
            decimal valorBasico = 2204.89m;

            using (var ctx = GeoSITMContext.CreateContext())
            using (var transaction = ctx.Database.BeginTransaction())
            {
                var validator = new DDJJValidator(ddjj)
                                        .Validate(new FechaVigenciaValidation())
                                        .Validate(new SuperficiesValidation())
                                        .Validate(new CaracteristicasValidation(ctx));

                var generador = new Generator(new TierraRuralComputation(ctx), validator);
                var result = await generador.SimulateAsync();

                Assert.IsNotNull(result);
                Assert.AreEqual(superficieTotalEsperada, result.SuperficieValuadaMT2);
                Assert.AreEqual(puntajeSuperficieTotal, result.PuntajeValuado);
                Assert.AreEqual(valorBasico, result.ValorOptimo);
                Assert.AreEqual(porcentajeDepreciacion, result.PorcentajeDepreciacion);
            }
        }

        [TestMethod]
        public void Test_Should_Throw_ArgumentOutOfRangeException_When_Any_Surface_NotGreaterThan_Zero()
        {
            var ddjj = new DDJJ()
            {
                IdUnidadTributaria = 37404,
                FechaVigencia = DateTime.Today,
                Sor = new DDJJSor()
                {
                    Superficies = new[]
                    {
                        new VALSuperficie
                        {
                            Superficie = 0,
                            Caracteristicas = new DDJJSorCar[0]
                        }
                    }
                }
            };

            var result = new DDJJValidator(ddjj).Validate(new SuperficiesValidation());

            Assert.IsTrue(result.Errors.Any());
            Assert.AreEqual(result.Errors.Single(), "El formulario está incompleto. Los códigos de terreno deben especificar la superficie.");
        }

        [TestMethod]
        public async Task Test_Should_Return_ValoresEsperados_For_Sample_DDJJ()
        {
            var ddjj = new DDJJ()
            {
                IdUnidadTributaria = 33465,
                FechaVigencia = new DateTime(1995, 12, 31),
                Sor = new DDJJSor()
                {
                    Superficies = new[]
                    {
                        new VALSuperficie
                        {
                            Superficie = 99.9175,
                            Caracteristicas = new []
                            {
                                new DDJJSorCar { IdSorCar = 77 },
                                new DDJJSorCar { IdSorCar = 44 },
                                new DDJJSorCar { IdSorCar = 94 },
                                new DDJJSorCar { IdSorCar = 59 },
                                new DDJJSorCar { IdSorCar = 65 },
                                new DDJJSorCar { IdSorCar = 87 },
                                new DDJJSorCar { IdSorCar = 83 },
                                new DDJJSorCar { IdSorCar = 92 },
                                new DDJJSorCar { IdSorCar = 79 },
                            }
                        },
                    }
                }
            };

            /*  
                |caracteristica           |valor           | id|puntaje|
                |-------------------------|----------------|---|-------|
                |ACCESO                   |DIRECTO         | 77|      5|
                |AGUA                     |DULCE           | 44|      3|
                |ALTIMETRÍA               |ALTO            | 94|     10|
                |COLOR CAPA ARABLE        |PARDO           | 59|      6|
                |CONSTITUCIÓN DE SUELOS   |HUMIFERO ARENOSO| 65|      6|
                |DISTANCIA A CENTRO URBANO|HASTA 5 KM.     | 87|      7|
                |DISTANCIA A RUTAS        |HASTA 1 KM.     | 83|      6|
                |ELECTRIFICACIÓN          |HAY             | 92|      4|
                |ESTADO CAMINO            |BUENO           | 79|      5|
                |-------------------------|----------------|---|     52|
            */

            int puntajeTotalEsperado = 52;
            decimal superficieTotalEsperada = 99_9175m;
            decimal puntajeSuperficie = CalculadorPuntajeSuperficie
                                            .CalcularPuntajeSuperficie(puntajeTotalEsperado, ddjj.Sor.Superficies.Single());
            decimal valorBasico = 2204.89m;

            var calc = new TierraRuralComputation(GeoSITMContext.CreateContext());
            var datosComputo = await calc.ComputeAsync(ddjj);

            Assert.AreEqual(puntajeSuperficie, datosComputo.PuntajeValuado);
            Assert.AreEqual(superficieTotalEsperada, datosComputo.SuperficieValuadaMT2);
            Assert.AreEqual(valorBasico, datosComputo.ValorOptimo);
        }

        [TestMethod]
        public async Task Test_Should_Return_ValorTierraCalculado_For_Sample_DDJJ()
        {
            var ddjj = new DDJJ()
            {
                IdUnidadTributaria = 74809,
                FechaVigencia = new DateTime(1995, 12, 31),
                Sor = new DDJJSor()
                {
                    Superficies = new[]
                    {
                        new VALSuperficie
                        {
                            Superficie = 98.557103,
                            Caracteristicas = new []
                            {
                                new DDJJSorCar { IdSorCar = 77 },
                                new DDJJSorCar { IdSorCar = 44 },
                                new DDJJSorCar { IdSorCar = 94 },
                                new DDJJSorCar { IdSorCar = 59 },
                                new DDJJSorCar { IdSorCar = 65 },
                                new DDJJSorCar { IdSorCar = 87 },
                                new DDJJSorCar { IdSorCar = 83 },
                                new DDJJSorCar { IdSorCar = 92 },
                                new DDJJSorCar { IdSorCar = 79 },
                            }
                        },
                    }
                }
            };

            /*  
                |caracteristica           |valor           | id|puntaje|
                |-------------------------|----------------|---|-------|
                |ACCESO                   |DIRECTO         | 77|      5|
                |AGUA                     |DULCE           | 44|      3|
                |ALTIMETRÍA               |ALTO            | 94|     10|
                |COLOR CAPA ARABLE        |PARDO           | 59|      6|
                |CONSTITUCIÓN DE SUELOS   |HUMIFERO ARENOSO| 65|      6|
                |DISTANCIA A CENTRO URBANO|HASTA 5 KM.     | 87|      7|
                |DISTANCIA A RUTAS        |HASTA 1 KM.     | 83|      6|
                |ELECTRIFICACIÓN          |HAY             | 92|      4|
                |ESTADO CAMINO            |BUENO           | 79|      5|
                |-------------------------|----------------|---|     52|
            */


            int puntajeTotalEsperado = 52;
            decimal superficieHATotalEsperada = 98.557103m;
            decimal puntajeSuperficie = CalculadorPuntajeSuperficie
                                            .CalcularPuntajeSuperficie(puntajeTotalEsperado, ddjj.Sor.Superficies.Single());
            decimal valorBasico = 890.22m;
            decimal valuacion = Math.Round(puntajeSuperficie * valorBasico, 2);

            var calc = new TierraRuralComputation(GeoSITMContext.CreateContext());
            var datosComputo = await calc.ComputeAsync(ddjj);

            Assert.AreEqual(superficieHATotalEsperada, datosComputo.SuperficieValuadaHA);
            Assert.AreEqual(valuacion, datosComputo.Valuacion);
        }

        [TestMethod]
        public async Task Test_Should_Return_ValorTierraCalculado_For_Sample_DDJJ_With_Multiple_Rows()
        {
            var ddjj = new DDJJ()
            {
                IdUnidadTributaria = 71700,
                FechaVigencia = new DateTime(1995, 12, 31),
                Sor = new DDJJSor()
                {
                    Superficies = new[]
                    {
                        new VALSuperficie
                        {
                            Superficie = 0.1200,
                            Caracteristicas = new []
                            {
                                new DDJJSorCar { IdSorCar = 78 },
                                new DDJJSorCar { IdSorCar = 87 },
                                new DDJJSorCar { IdSorCar = 82 },
                                new DDJJSorCar { IdSorCar = 93 },
                                new DDJJSorCar { IdSorCar = 81 },
                                new DDJJSorCar { IdSorCar = 75 },
                            }
                        },
                        new VALSuperficie
                        {
                            Superficie = 0.0025,
                            Caracteristicas = new []
                            {
                                new DDJJSorCar { IdSorCar = 77 },
                                new DDJJSorCar { IdSorCar = 89 },
                                new DDJJSorCar { IdSorCar = 84 },
                                new DDJJSorCar { IdSorCar = 92 },
                                new DDJJSorCar { IdSorCar = 80 },
                                new DDJJSorCar { IdSorCar = 74 },
                                new DDJJSorCar { IdSorCar = 69 },
                            }
                        },
                    }
                }
            };

            /*  SUP 1
                |caracteristica           |valor       |     id|puntaje|
                |-------------------------|------------|-------|-------|
                |ACCESO                   |INDIRECTO   |     78|      2|
                |DISTANCIA A CENTRO URBANO|HASTA 5 KM. |     87|      7|
                |DISTANCIA A RUTAS        |SOBRE CAMINO|     82|      7|
                |ELECTRIFICACIÓN          |NO HAY      |     93|      0|
                |ESTADO CAMINO            |MALO        |     81|      1|
                |LAGUNA                   |AGUA DULCE  |     75|     10|
                |-------------------------|------------|-------|     27|

                SUP 2
                |caracteristicas          |valor         |     id |puntaje|
                |-------------------------|--------------|--------|-------|
                |ACCESO                   |DIRECTO       |     77 |      5|
                |DISTANCIA A CENTRO URBANO|DE 10 A 20 KM.|     89 |      4|
                |DISTANCIA A RUTAS        |DE 1 A 3 KM.  |     84 |      5|
                |ELECTRIFICACIÓN          |HAY           |     92 |      4|
                |ESTADO CAMINO            |REGULAR       |     80 |      3|
                |EXPLOTADO                |DEGRADADO     |     74 |     10|
                |VIRGEN                   |BUENO         |     69 |     60|
                |-------------------------|--------------|--------|     91|

            */

            decimal valorBasico = 929.26m;
            int idx = 0;
            decimal puntajeSuperficieTotal = new[] { 27, 91 }.Aggregate(0m, (accum, puntaje) =>
            {
                return accum + CalculadorPuntajeSuperficie.CalcularPuntajeSuperficie(puntaje, ddjj.Sor.Superficies.ElementAt(idx++));
            });
            decimal valuacion = Math.Round(puntajeSuperficieTotal * valorBasico, 2);

            var calc = new TierraRuralComputation(GeoSITMContext.CreateContext());
            var datosComputo = await calc.ComputeAsync(ddjj);

            Assert.AreEqual(valuacion, datosComputo.Valuacion);
        }

        [TestMethod]
        public async Task Test_Should_Return_ValorTierraCalculado_For_Titular_Con_Unica_Parcela()
        {
            var ddjj = new DDJJ()
            {
                FechaVigencia = new DateTime(1995, 12, 31),
                IdUnidadTributaria = 347578,
                Dominios = new[]
                {
                    new DDJJDominio()
                    {
                        Titulares = new []
                        {
                            new DDJJDominioTitular()
                            {
                                IdPersona = 189660,
                            }
                        },
                    },
                },
                Sor = new DDJJSor()
                {
                    Superficies = new[]
                    {
                        new VALSuperficie
                        {
                            Superficie = 116.3822,
                            Caracteristicas = new []
                            {
                                new DDJJSorCar { IdSorCar = 77 },
                                new DDJJSorCar { IdSorCar = 44 },
                                new DDJJSorCar { IdSorCar = 94 },
                                new DDJJSorCar { IdSorCar = 59 },
                                new DDJJSorCar { IdSorCar = 65 },
                                new DDJJSorCar { IdSorCar = 87 },
                                new DDJJSorCar { IdSorCar = 83 },
                                new DDJJSorCar { IdSorCar = 92 },
                                new DDJJSorCar { IdSorCar = 79 },
                            }
                        },
                    }
                }
            };

            /*  
                |caracteristica           |valor           | id|puntaje|
                |-------------------------|----------------|---|-------|
                |ACCESO                   |DIRECTO         | 77|      5|
                |AGUA                     |DULCE           | 44|      3|
                |ALTIMETRÍA               |ALTO            | 94|     10|
                |COLOR CAPA ARABLE        |PARDO           | 59|      6|
                |CONSTITUCIÓN DE SUELOS   |HUMIFERO ARENOSO| 65|      6|
                |DISTANCIA A CENTRO URBANO|HASTA 5 KM.     | 87|      7|
                |DISTANCIA A RUTAS        |HASTA 1 KM.     | 83|      6|
                |ELECTRIFICACIÓN          |HAY             | 92|      4|
                |ESTADO CAMINO            |BUENO           | 79|      5|
                |-------------------------|----------------|---|     52|
            */

            int puntajeTotalEsperado = 52;
            decimal superficieHATotalEsperada = 116.3822m;
            decimal puntajeSuperficie = CalculadorPuntajeSuperficie
                                            .CalcularPuntajeSuperficie(puntajeTotalEsperado, ddjj.Sor.Superficies.Single());
            decimal valorBasico = 1111.19m;
            decimal valuacion = Math.Round(puntajeSuperficie * valorBasico, 2);

            var calc = new TierraRuralComputation(GeoSITMContext.CreateContext());
            var datosComputo = await calc.ComputeAsync(ddjj);

            Assert.AreEqual(superficieHATotalEsperada, datosComputo.SuperficieValuadaHA);
            Assert.AreEqual(valuacion, datosComputo.Valuacion);
        }

        [TestMethod]
        public async Task Test_Should_Return_ValorTierraCalculado_For_Titular_Con_Unica_Parcela_Superior_1000Ha()
        {
            var ddjj = new DDJJ()
            {
                FechaVigencia = new DateTime(1995, 12, 31),
                IdUnidadTributaria = 32369,
                Dominios = new[]
                {
                    new DDJJDominio()
                    {
                        Titulares = new []
                        {
                            new DDJJDominioTitular()
                            {
                                IdPersona = 189660,
                            }
                        },
                    },
                },
                Sor = new DDJJSor()
                {
                    Superficies = new[]
                    {
                        new VALSuperficie
                        {
                            Superficie = 99.0000,
                            Caracteristicas = new []
                            {
                                new DDJJSorCar { IdSorCar = 77 },
                                new DDJJSorCar { IdSorCar = 44 },
                                new DDJJSorCar { IdSorCar = 94 },
                                new DDJJSorCar { IdSorCar = 59 },
                                new DDJJSorCar { IdSorCar = 65 },
                                new DDJJSorCar { IdSorCar = 87 },
                                new DDJJSorCar { IdSorCar = 83 },
                                new DDJJSorCar { IdSorCar = 92 },
                                new DDJJSorCar { IdSorCar = 79 },
                            }
                        },
                    }
                }
            };

            /*  
                |caracteristica           |valor           | id|puntaje|
                |-------------------------|----------------|---|-------|
                |ACCESO                   |DIRECTO         | 77|      5|
                |AGUA                     |DULCE           | 44|      3|
                |ALTIMETRÍA               |ALTO            | 94|     10|
                |COLOR CAPA ARABLE        |PARDO           | 59|      6|
                |CONSTITUCIÓN DE SUELOS   |HUMIFERO ARENOSO| 65|      6|
                |DISTANCIA A CENTRO URBANO|HASTA 5 KM.     | 87|      7|
                |DISTANCIA A RUTAS        |HASTA 1 KM.     | 83|      6|
                |ELECTRIFICACIÓN          |HAY             | 92|      4|
                |ESTADO CAMINO            |BUENO           | 79|      5|
                |-------------------------|----------------|---|     52|
            */

            int puntajeTotalEsperado = 52;
            decimal valorBasico = 2204.89m;
            decimal puntajeSuperficie = CalculadorPuntajeSuperficie
                                            .CalcularPuntajeSuperficie(puntajeTotalEsperado, ddjj.Sor.Superficies.Single());
            decimal valuacion = Math.Round(puntajeSuperficie * valorBasico, 2);

            var calc = new TierraRuralComputation(GeoSITMContext.CreateContext());
            var datosComputo = await calc.ComputeAsync(ddjj);

            Assert.AreEqual(valuacion, datosComputo.Valuacion);
        }
    }

    static class CalculadorPuntajeSuperficie
    {
        public static decimal CalcularPuntajeSuperficie(int puntaje, VALSuperficie superficie)
        {
            return GetSoloHastaArea(superficie) * puntaje / 100;
        }

        private static decimal GetSoloHastaArea(VALSuperficie superficie)
        {
            return Math.Truncate(Convert.ToDecimal(superficie.Superficie ?? 0) * 100) / 100;
        }

    }
    class ValidadorDDJJSiempreValido : IDDJJValidator
    {
        public string[] Errors { get { return new string[0]; } }

        public DDJJ DDJJ { get; private set; }

        public ValidadorDDJJSiempreValido()
        {
            DDJJ = new DDJJ()
            {
                FechaVigencia = DateTime.Now,
                IdUnidadTributaria = 32369,
                Sor = new DDJJSor()
                {
                    Superficies = new VALSuperficie[0]
                }
            };
        }

        public IDDJJValidator Validate(IDDJJValidation validator)
        {
            return this;
        }
    }
}
