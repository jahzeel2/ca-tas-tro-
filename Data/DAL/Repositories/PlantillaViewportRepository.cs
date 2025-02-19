﻿using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using GeoSit.Data.BusinessEntities.ModuloPloteo;
using GeoSit.Data.BusinessEntities.MapasTematicos;
using System;
using System.Data;
using GeoSit.Data.DAL.Common.ExtensionMethods.Data;
using GeoSit.Data.DAL.Common.ExtensionMethods.Atributos;
using Oracle.ManagedDataAccess.Client;
using System.Configuration;
using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.DAL.Common.Enums;

namespace GeoSit.Data.DAL.Repositories
{
    public class PlantillaViewportRepository : IPlantillaViewportRepository
    {
        private readonly GeoSITMContext _context;

        public PlantillaViewportRepository(GeoSITMContext context)
        {
            _context = context;
        }

        public List<PlantillaViewport> ObtenerAllPlantillasViewPorts()
        {
            List<PlantillaViewport> viewports = _context.PlantillasViewports.ToList();

            for (int i = 0; i < viewports.Count; i++)
            {

                //viewports[i] = CargarPlantillaViewport(viewports[i]);
            }
            return viewports;
        }

        public List<Plantilla> ObtenerAllPlantillasViewPortsInPlantilla()
        {
            List<PlantillaViewport> viewports = _context.PlantillasViewports.ToList();
            List<Plantilla> lstPlantillas = new List<Plantilla>();

            for (int i = 0; i < viewports.Count; i++)
            {

                lstPlantillas.Add(CargarPlantillaViewport(viewports[i]));
            }
            return lstPlantillas;
        }

        public PlantillaViewport ObtenerPlantillasViewPortsById(long pId)
        {
            PlantillaViewport viewports = _context.PlantillasViewports.Where(p => p.IdPlantillaViewport == pId).FirstOrDefault();

            return viewports;
        }

        public Plantilla CargarPlantillaViewport(PlantillaViewport PlantiView)
        {
            throw new NotImplementedException("No implemento para que de error en caso de usarse. Hay código que no entiendo y no quiero perder tiempo en ésto ahora");
        }

        public List<Plantilla> ObtenerAllPlantillasViewPortsInPlantillaByPloteoFrecuente(int pIdPlotFrec, string idDistrito)
        {
            List<PloteoFrecuenteEspecial> lstPlotFrecEsp;
            List<Plantilla> lstPlantillas = new List<Plantilla>();
            List<PlantillaViewport> viewports = new List<PlantillaViewport>();
            try
            {
                lstPlotFrecEsp = _context.PloteosFrecuentesEspeciales.Where(pf => pf.IdPloteoFrecuente == pIdPlotFrec && pf.IdDistrito == idDistrito).ToList();

                foreach (var plotfrecesp in lstPlotFrecEsp)
                {
                    if (plotfrecesp.IdPlantillaViewport.HasValue)
                    {
                        PlantillaViewport plantview = _context.PlantillasViewports.Where(p => plotfrecesp.IdPlantillaViewport == p.IdPlantillaViewport).FirstOrDefault();
                        Plantilla plantilla = CargarPlantillaViewport(plantview);
                        plantilla.Geometry = GetLayerGrafPlantillaViewPortGeometry(plotfrecesp.IdPloteoFrecuenteEspecial);
                        if (plantilla.Geometry == null)
                        {
                            plantilla.Geometry = GetLayerGrafPlantillaViewPortObjets(plotfrecesp.IdPloteoFrecuenteEspecial);
                        }
                        List<PlantillaEscala> plotFrecEspEscalas = GetEscalasPlotFrecEsp(plotfrecesp.IdPloteoFrecuenteEspecial);
                        if (plotFrecEspEscalas != null && plotFrecEspEscalas.Count > 0)
                        {
                            if (plantilla.PlantillaEscalas != null)
                                plantilla.PlantillaEscalas.Clear();
                            plantilla.PlantillaEscalas = plotFrecEspEscalas;
                        }

                        //plantilla.esViewport = true;
                        lstPlantillas.Add(plantilla);
                    }
                    else
                    {
                        PlantillaRepository pr = new PlantillaRepository(_context);

                        Plantilla plantillaPrincipal = new Plantilla();

                        plantillaPrincipal = pr.GetPlantillaById(plotfrecesp.IdPlantilla);

                        //PloteoFrecuenteEspecial plantillaViewportPrincipal = lstPlotFrecEsp.Where(ps => !ps.IdPlantillaViewport.HasValue).FirstOrDefault();
                        //PlantillaViewport plantview = _context.PlantillasViewports.Where(p => plotfrecesp.IdPlantillaViewport == p.IdPlantillaViewport).FirstOrDefault();

                        plantillaPrincipal.esViewport = true;
                        plantillaPrincipal.Geometry = GetLayerGrafPlantillaViewPortGeometry(plotfrecesp.IdPloteoFrecuenteEspecial);
                        if (plantillaPrincipal.Geometry == null)
                        {
                            plantillaPrincipal.Geometry = GetLayerGrafPlantillaViewPortObjets(plotfrecesp.IdPloteoFrecuenteEspecial);
                        }
                        //plantillaPrincipal.Orientacion = plotfrecesp.Orientacion.HasValue ? (int)plotfrecesp.Orientacion : plantillaPrincipal.Orientacion;


                        List<PlantillaEscala> plotFrecEspEscalas = GetEscalasPlotFrecEsp(plotfrecesp.IdPloteoFrecuenteEspecial);
                        if (plotFrecEspEscalas != null && plotFrecEspEscalas.Count > 0)
                        {
                            if (plantillaPrincipal.PlantillaEscalas != null)
                                plantillaPrincipal.PlantillaEscalas.Clear();
                            plantillaPrincipal.PlantillaEscalas = plotFrecEspEscalas;
                        }

                        //plantillaPrincipal.esViewport = true;
                        lstPlantillas.Add(plantillaPrincipal);
                    }
                }
            }
            catch (Exception)
            {

            }
            return lstPlantillas;
        }

        public List<PlantillaEscala> GetEscalasPlotFrecEsp(long idPlantillaViewport)
        {
            PloteoFrecuenteEspecial plotfrecesp = _context.PloteosFrecuentesEspeciales.Where(p => p.IdPloteoFrecuenteEspecial == idPlantillaViewport).FirstOrDefault();

            List<PlantillaEscala> listPlantEsc = new List<PlantillaEscala>();
            PlantillaEscala plantEsc = new PlantillaEscala();

            if (plotfrecesp.Escala != null)
            {
                plantEsc.Escala = (int)plotfrecesp.Escala;
                listPlantEsc.Add(plantEsc);
            }

            return listPlantEsc;
        }

        public LayerGraf[] GetLayerGrafPlantillaViewPortObjets(long idPlantillaViewport)
        {
            var plotfrecesp = _context.PloteosFrecuentesEspeciales.FirstOrDefault(p => p.IdPloteoFrecuenteEspecial == idPlantillaViewport);

            var lstPlotFrecGeom = _context.PloteoFrecuenteGeometrias.Where(p => p.IdPloteoFrecuenteEspecial == plotfrecesp.IdPloteoFrecuenteEspecial && !p.Fecha_Baja.HasValue).ToList();
            var queries = new List<Interfaces.ISQLQueryBuilder>();
            foreach (var item in lstPlotFrecGeom)
            {
                Componente comp = _context.Componente.Include(a => a.Atributos).SingleOrDefault(c => c.Tabla == item.Tabla);

                #region comento por ahora porque es propio de aysa
                ///*en caso de que el componente sea A_RADSERV 
                // hacer un bucle while con los id_padre
                // */
                //int idComp_RadServ = Convert.ToInt32(_context.ParametrosGenerales.FirstOrDefault(p => p.Descripcion.ToUpper() == "ID_COMPONENTE_A_RADSERV").Valor);
                //if (comp.ComponenteId == idComp_RadServ)//En ge_parametro
                //{

                //    /*Buscar el id_padre*/

                //    int idServ = item.IdObjeto;
                //    DateTime? fechaBaja = null;
                //    //int? idPadre = item.IdObjeto;
                //    bool encontroPadre = true;
                //    while (encontroPadre && !fechaBaja.HasValue)
                //    {
                //        using (IDbCommand objComm = _context.Database.Connection.CreateCommand())
                //        {
                //            _context.Database.Connection.Open();
                //            objComm.CommandText = string.Format("select id_radserv, fecha_baja from " + item.Esquema + "." + item.Tabla + " where id_padre = " + idServ);
                //            try
                //            {
                //                using (IDataReader data = objComm.ExecuteReader(CommandBehavior.CloseConnection))
                //                {
                //                    encontroPadre = false;
                //                    while (data.Read())
                //                    {
                //                        #region colectar datos
                //                        idServ = data.GetInt32(0);
                //                        data[0].ToString();
                //                        if (data[1].ToString() != "")
                //                            fechaBaja = data.GetDateTime(1);
                //                        else
                //                            fechaBaja = null;
                //                        #endregion
                //                        encontroPadre = true;
                //                    }
                //                }
                //            }
                //            catch (OracleException)
                //            {
                //                if (_context.Database.Connection.State == ConnectionState.Open) _context.Database.Connection.Close();
                //                _context.GetLogger().LogInfo(string.Format("sql query: {0}", objComm.CommandText));
                //                throw;
                //            }
                //        }
                //    }


                //    if (fechaBaja != null)
                //    {
                //        continue;
                //    }
                //    else
                //    {
                //        item.IdObjeto = idServ;
                //    }
                //} 
                #endregion

                var campoFeatId = comp.Atributos.GetAtributoFeatId();
                var campoGeometry = comp.Atributos.GetAtributoGeometry();
                using (var builder = _context.CreateSQLQueryBuilder())
                {
                    queries.Add(builder.AddTable(comp, "t1")
                           .AddFilter(campoFeatId, item.IdObjeto, SQLOperators.EqualsTo)
                           .AddGeometryField(builder.CreateGeometryFieldBuilder(campoGeometry, "t1"), "geom"));
                }
            }
            using (var builder = _context.CreateSQLQueryBuilder())
            {
                return builder.AddTable($"({string.Join(" union all ", builder)})", "final")
                              .AddGeometryField(builder.CreateGeometryFieldBuilder(new Atributo() { Campo = "geom" }, "final").ChangeToSRID(SRID.App).ToWKT(), "geom")
                              .ExecuteQuery((IDataReader reader) =>
                              {
                                  return new LayerGraf()
                                  {
                                      FeatId = 0,
                                      Nombre = "Obras",
                                      Geom = reader.GetGeometryFromField(0),
                                      Rotation = 0
                                  };
                              }).ToArray();
            }
        }

        #region Metodo Viejo

        public LayerGraf[] GetLayerGrafPlantillaViewPortGeometry(long idPlantillaViewport)
        {
            var campoFeatId = new Atributo { Campo = "ID_PLOTEO_FRECUENTE_ESPECIAL" };
            var campoLabel = new Atributo { Campo = "DESCRIPCION" };
            var campoGeometry = new Atributo { Campo = "GEOMETRY" };
            var componente = new Componente { Esquema = ConfigurationManager.AppSettings["DATABASE"], Tabla = "mp_ploteo_frecuente_especial" };

            try
            {
                using (var builder = _context.CreateSQLQueryBuilder())
                {
                    return builder.AddTable(componente, "t1")
                                  .AddFields(campoFeatId, campoLabel)
                                  .AddGeometryField(builder.CreateGeometryFieldBuilder(campoGeometry, "t1").ChangeToSRID(SRID.App).ToWKT(), "geom")
                                  .ExecuteQuery((IDataReader reader) =>
                                  {
                                      return new LayerGraf()
                                      {
                                          FeatId = reader.GetNullableInt64(0).Value,
                                          Nombre = reader.GetStringOrEmpty(1),
                                          Geom = reader.GetGeometryFromField(reader.GetOrdinal("geom")),
                                          Rotation = 0
                                      };
                                  }).ToArray();
                }
            }
            catch (Exception ex)
            {
                _context.GetLogger().LogError(string.Format("GetLayerGrafPlantillaViewPort(" + idPlantillaViewport + ")"), ex);
                throw;
            }
        }
        #endregion
    }
}
