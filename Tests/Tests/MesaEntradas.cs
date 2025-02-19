using GeoSit.Data.BusinessEntities.Common;
using GeoSit.Data.BusinessEntities.GlobalResources;
using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.DAL.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using static GeoSit.Data.DAL.Repositories.MesaEntradasRepository;

namespace Tests
{
    [TestClass]
    public class MesaEntradas
    {
        [TestMethod]
        public void Test_Should_Return_No_Grid_Records()
        {
            using (var ctx = GeoSITMContext.CreateContext())
            {
                var parametros = new DataTableParameters()
                {
                    length = 1
                };
                var repo = new MesaEntradasRepository(ctx);
                var result = repo.RecuperarTramites(Grilla.Propios, parametros, 0);

                Assert.AreEqual(0, result.data.Length);
            }
        }

        [TestMethod]
        public void Test_Should_Return_No_Grid_Records_For_User_Sector()
        {
            using (var ctx = GeoSITMContext.CreateContext())
            {
                var parametros = new DataTableParameters()
                {
                    length = 1
                };
                var repo = new MesaEntradasRepository(ctx);
                var result = repo.RecuperarTramites(Grilla.Sector, parametros, 0);

                Assert.AreEqual(0, result.data.Length);
            }
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        [DataRow(4)]
        public void Test_Should_Return_No_Grid_Records_For_Given_User(long usuario)
        {
            using (var ctx = GeoSITMContext.CreateContext())
            {
                var parametros = new DataTableParameters()
                {
                    length = 1
                };
                var repo = new MesaEntradasRepository(ctx);
                var result = repo.RecuperarTramites(Grilla.Propios, parametros, usuario);

                Assert.AreEqual(0, result.data.Length);
            }
        }

        [DataTestMethod]
        [DataRow(10, 10)]
        public void Test_Should_Return_No_Grid_Records_For_Sector_When_User_Is_Profesional(long sector_profesionales, int muestreo)
        {
            using (var ctx = GeoSITMContext.CreateContext())
            {
                var parametros = new DataTableParameters()
                {
                    length = 1
                };
                var repoUsuarios = new UsuariosRepository(ctx);
                var repo = new MesaEntradasRepository(ctx);
                var usuariosProfesionales = repoUsuarios.GetUsuariosByIdSector(sector_profesionales).Take(muestreo);
                foreach (var usuario in usuariosProfesionales)
                {
                    var result = repo.RecuperarTramites(Grilla.Propios, parametros, usuario.Id_Usuario);
                    if(result.data.Length != 0)
                    {
                        Assert.Fail($"El usuario {usuario.NombreApellidoCompleto} (id: {usuario.Id_Usuario}) devuelve registros");
                    }
                }
                Assert.IsTrue(true);
            }
        }

        [DataTestMethod]
        [DataRow(0, Grilla.Propios, 10)]
        [DataRow(1, Grilla.Propios, 10)]
        [DataRow(4, Grilla.Propios, 10)]
        [DataRow(10, Grilla.Propios, 10)]
        [DataRow(0, Grilla.Sector, 10)]
        [DataRow(1, Grilla.Sector, 10)]
        [DataRow(4, Grilla.Sector, 10)]
        [DataRow(10, Grilla.Sector, 10)]
        [DataRow(0, Grilla.Catastro, 10)]
        [DataRow(1, Grilla.Catastro, 10)]
        [DataRow(4, Grilla.Catastro, 10)]
        [DataRow(10, Grilla.Catastro, 10)]
        public void Test_Should_Return_No_More_Than_Queried_Records(int queried_records, Grilla grilla, long usuario)
        {
            using (var ctx = GeoSITMContext.CreateContext())
            {
                var parametros = new DataTableParameters()
                {
                    length = queried_records
                };
                var repo = new MesaEntradasRepository(ctx);
                var result = repo.RecuperarTramites(grilla, parametros, usuario);

                Assert.IsTrue(result.data.Length <= queried_records);
            }
        }

        [DataTestMethod]
        [DataRow(Grilla.Propios)]
        [DataRow(Grilla.Sector)]
        [DataRow(Grilla.Catastro)]
        public void Test_Should_Return_Empty_Acciones(Grilla grilla)
        {
            using (var ctx = GeoSITMContext.CreateContext())
            {
                long usuario = 0;
                var repo = new MesaEntradasRepository(ctx);
                var acciones = repo.RecuperarAccionesDisponibles(grilla, new int[0], usuario);

                //si tiene elementos, da verdadero y el assert falla porque espera que .Any() devuelva falso
                Assert.IsFalse(acciones.Seleccion.Concat(acciones.Generales).Any());
            }
        }

        [TestMethod]
        public void Test_Should_Return_Only_Accion_NuevoTramite()
        {
            using (var ctx = GeoSITMContext.CreateContext())
            {
                long usuario = 1_000_175;
                var repo = new MesaEntradasRepository(ctx);
                var acciones = repo.RecuperarAccionesDisponibles(Grilla.Propios, new int[0], usuario);

                long accionNuevoTramite = Convert.ToInt64(FuncionesTramite.Nuevo);
                Assert.IsTrue(acciones.Generales.Count() == 1 && acciones.Generales[0].IdAccion == accionNuevoTramite);
            }
        }

        [DataTestMethod]
        [DataRow(Grilla.Propios, new int[] { 1, 2 })]
        [DataRow(Grilla.Propios, new int[0])]
        [DataRow(Grilla.Sector, new int[] { 1, 2 })]
        [DataRow(Grilla.Sector, new int[0])]
        [DataRow(Grilla.Catastro, new int[] { 1, 2 })]
        [DataRow(Grilla.Catastro, new int[0])]
        public void Test_Should_Not_Return_Accion_NuevoTramite_In_AccionesSeleccion(Grilla grilla, int[] seleccion)
        {
            using (var ctx = GeoSITMContext.CreateContext())
            {
                long usuario = 1;
                var repo = new MesaEntradasRepository(ctx);
                var acciones = repo.RecuperarAccionesDisponibles(grilla, seleccion, usuario);

                long accionNuevoTramite = Convert.ToInt64(FuncionesTramite.Nuevo);
                Assert.IsTrue(acciones.Seleccion.All(a=>a.IdAccion != accionNuevoTramite));
            }
        }

        [DataTestMethod]
        [DataRow(Grilla.Sector)]
        [DataRow(Grilla.Catastro)]
        public void Test_Should_Not_Return_Accion_NuevoTramite(Grilla grilla)
        {
            using (var ctx = GeoSITMContext.CreateContext())
            {
                long usuario = 1;
                var repo = new MesaEntradasRepository(ctx);
                var acciones = repo.RecuperarAccionesDisponibles(grilla, new int[0], usuario);

                long accionNuevoTramite = Convert.ToInt64(FuncionesTramite.Nuevo);
                Assert.IsTrue(acciones.Generales.All(a => a.IdAccion != accionNuevoTramite));
            }
        }

        [DataTestMethod]
        [DataRow(Grilla.Propios, new int[] { 1, 2 })]
        [DataRow(Grilla.Propios, new int[0])]
        [DataRow(Grilla.Sector, new int[] { 1, 2 })]
        [DataRow(Grilla.Sector, new int[0])]
        [DataRow(Grilla.Catastro, new int[] { 1, 2 })]
        [DataRow(Grilla.Catastro, new int[0])]
        public void Test_Should_Not_Return_Accion_ConsultaTramite(Grilla grilla, int[] seleccion)
        {
            using (var ctx = GeoSITMContext.CreateContext())
            {
                long usuario = 1;
                var repo = new MesaEntradasRepository(ctx);
                var acciones = repo.RecuperarAccionesDisponibles(grilla, seleccion, usuario);

                long accionConsultaTramite = Convert.ToInt64(FuncionesTramite.Consultar);
                Assert.IsTrue(seleccion.Length != 1 && acciones.Seleccion.All(a => a.IdAccion != accionConsultaTramite));
            }
        }

        [DataTestMethod]
        [DataRow(Grilla.Propios, new int[] { 1 })]
        [DataRow(Grilla.Sector, new int[] { 1 })]
        [DataRow(Grilla.Catastro, new int[] { 1 })]
        public void Test_Should_Return_Accion_ConsultaTramite(Grilla grilla, int[] seleccion)
        {
            using (var ctx = GeoSITMContext.CreateContext())
            {
                long usuario = 1;
                var repo = new MesaEntradasRepository(ctx);
                var acciones = repo.RecuperarAccionesDisponibles(grilla, seleccion, usuario);

                long accionConsultaTramite = Convert.ToInt64(FuncionesTramite.Consultar);
                Assert.IsTrue(acciones.Seleccion.Any(a => a.IdAccion == accionConsultaTramite));
            }
        }

    }
}
