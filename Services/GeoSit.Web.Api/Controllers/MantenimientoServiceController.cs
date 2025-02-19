using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using GeoSit.Data.BusinessEntities.Mantenimiento;
using GeoSit.Data.BusinessEntities.MapasTematicos;
using GeoSit.Data.DAL.Common.Enums;
using GeoSit.Data.DAL.Common.ExtensionMethods.Data;
using GeoSit.Data.DAL.Contexts;

namespace GeoSit.Web.Api.Controllers
{
    public class MantenimientoServiceController : ApiController
    {
        private readonly GeoSITMContext db = GeoSITMContext.CreateContext();

        [HttpGet]
        [ResponseType(typeof(List<ComponenteTA>))]
        public IHttpActionResult GetListaComponenteTA()
        {
            var lista = db.ComponenteTA.ToList();
            return Ok(lista);
        }

        [HttpGet]
        [ResponseType(typeof(List<AtributoTA>))]
        public IHttpActionResult GetListaAtributoTA()
        {
            var lista = db.AtributoTA.OrderBy(x => x.Orden).ToList();
            return Ok(lista);
        }

        [HttpGet]
        [ResponseType(typeof(List<AtributoTA>))]
        public IHttpActionResult GetListaAtributoTAById(long Id)
        {
            var lista = db.AtributoTA.Where(x => x.Id_Componente == Id && (x.Es_Visible || x.Es_Clave == 1)).OrderBy(x => x.Orden).ToList();
            foreach (var elemento in lista.Where(e => !string.IsNullOrEmpty(e.Tabla)))
            {
                elemento.Opciones = GetListaAtributoRelacionado(elemento.Tabla, elemento.Esquema, elemento.Campo_Relac, elemento.Descriptor);
            }
            return Ok(lista);
        }

        [HttpGet]
        [ResponseType(typeof(DataTable))]
        public IHttpActionResult GetContenidoTabla(long Id)
        {
            try
            {
                var agrupado = (from componente in db.ComponenteTA
                                join attr in db.AtributoTA on componente.Id_Compoente equals attr.Id_Componente
                                where componente.Id_Compoente == Id && (attr.Es_Visible || attr.Es_Clave == 1)
                                group attr by componente into grupo
                                select new { componente = grupo.Key, atributos = grupo }).Single();

                if (agrupado.atributos.Any(attr => attr.Es_Clave == 1))
                {
                    return Ok(db.CreateSQLQueryBuilder()
                                    .AddTable(agrupado.componente.Esquema, agrupado.componente.Tabla, "t1")
                                    .AddFields(agrupado.atributos
                                                       .Where(atr => atr.Es_Clave == 1).Concat(agrupado.atributos.Where(atr => atr.Es_Clave != 1).OrderBy(atr => atr.Orden))
                                                       .Select(atr => atr.Campo).ToArray())
                                    .AddFilter("fecha_baja", null, SQLOperators.IsNull)
                                    .ExecuteDataTable());
                }
                return Ok(new DataTable());
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError(string.Format("GetContenidoTabla({0})", Id), ex);
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [ResponseType(typeof(Dictionary<string, string>))]
        public Dictionary<string, string> GetContenidoTablaAsignacion(long Id, long IdTabla)
        {
            try
            {
                var agrupado = (from comp in db.ComponenteTA
                                where comp.Id_Compoente == Id
                                join attr in db.AtributoTA on comp.Id_Compoente equals attr.Id_Componente
                                group attr by comp into grupo
                                select new { componente = grupo.Key, atributos = grupo }).Single();

                var valores = new Dictionary<string, string>();
                db.CreateSQLQueryBuilder()
                         .AddTable(agrupado.componente.Esquema, agrupado.componente.Tabla, "t1")
                         .AddFilter(agrupado.atributos.First(f => f.Es_Clave == 1).Campo, IdTabla, SQLOperators.EqualsTo)
                         .AddFields(agrupado.atributos.Select(f => f.Campo).ToArray())
                         .ExecuteQuery((IDataReader reader) =>
                         {
                             for (int i = 0; i < reader.FieldCount; i++)
                             {
                                 valores.Add(reader.GetName(i), reader.GetTypeFormattedStringValue(i));
                             }
                         });
                return valores;
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError(string.Format("GetContenidoTablaAsignacion({0},{1})", Id, IdTabla), ex);
                return new Dictionary<string, string>();
            }
        }

        //DELETE LOGICO REGISTRO
        [HttpPost]
        [ResponseType(typeof(bool))]
        public IHttpActionResult GetEliminaRegistroAsignacion(TablasAuxiliares Valores)
        {
            try
            {
                long idComponente = long.Parse(Valores.ComponentesId);
                var metadata = (from comp in db.ComponenteTA
                                where comp.Id_Compoente == idComponente
                                join attr in db.AtributoTA on comp.Id_Compoente equals attr.Id_Componente
                                where attr.Es_Clave == 1
                                select new { componente = comp, clave = attr }).Single();

                if (metadata == null)
                {
                    return Ok(false);
                }

                var actualizar = new Dictionary<Atributo, object>();
                using (var metadataBuilder = db.CreateSQLQueryBuilder())
                {
                    foreach (string campo in metadataBuilder.GetTableFields(metadata.componente.Esquema, metadata.componente.Tabla))
                    {
                        if (string.Compare(campo, "fecha_baja", true) == 0 || string.Compare(campo, "fecha_modif", true) == 0)
                        {
                            actualizar.Add(new Atributo { Campo = campo, TipoDatoId = 666 }, null);
                        }
                        else if (string.Compare(campo, "id_usu_baja", true) == 0 || string.Compare(campo, "id_usu_modif", true) == 0)
                        {
                            actualizar.Add(new Atributo { Campo = campo }, Valores.Id_Usuario);
                        }
                    }
                }
                using (var updateBuilder = db.CreateSQLQueryBuilder())
                {
                    updateBuilder.AddTable(metadata.componente.Esquema, metadata.componente.Tabla, null)
                                 .AddFieldsToUpdate(actualizar.ToArray())
                                 .AddFilter(metadata.clave.Campo, Valores.TablaID, SQLOperators.EqualsTo);

                    return Ok(updateBuilder.ExecuteUpdate() != 0); //true si actualiza, false si no
                }
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError(string.Format("GetEliminaRegistroAsignacion(comp:{0},id:{1})", Valores.ComponentesId, Valores.TablaID), ex);
                return Ok(false);
            }
        }

        //INSERT REGISTRO
        [HttpPost]
        [ResponseType(typeof(bool))]
        public IHttpActionResult SetAgregarRegistro(TablasAuxiliares Valores)
        {
            try
            {
                long idComponente = long.Parse(Valores.ComponentesId);
                var metadata = (from comp in db.ComponenteTA
                                where comp.Id_Compoente == idComponente
                                join attr in db.AtributoTA on comp.Id_Compoente equals attr.Id_Componente
                                where attr.Es_Clave == 1 || attr.Es_Visible
                                group attr by comp into grp
                                select new { componente = grp.Key, campos = grp }).FirstOrDefault();

                var attrClave = metadata.campos.FirstOrDefault(c => c.Es_Clave == 1);
                bool esNuevo = string.IsNullOrEmpty(Valores.TablaID);
                if (!esNuevo)
                {
                    using (var checkExistenciaBuilder = db.CreateSQLQueryBuilder())
                    {
                        esNuevo = checkExistenciaBuilder.AddTable(metadata.componente.Esquema, metadata.componente.Tabla, "t1")
                                              .AddFormattedField("count(1)")
                                              .AddFilter(attrClave.Campo, Valores.TablaID, SQLOperators.EqualsTo)
                                              .ExecuteQuery((IDataReader reader) => reader.GetInt32(0) == 0)
                                              .Single();
                    }
                }
                var actualizar = new Dictionary<Atributo, object>();
                using (var metadataBuilder = db.CreateSQLQueryBuilder())
                {
                    foreach (string campo in metadataBuilder.GetTableFields(metadata.componente.Esquema, metadata.componente.Tabla))
                    {
                        int idx = 0;
                        if (esNuevo)
                        {
                            if (string.Compare(campo, "fecha_alta", true) == 0)
                            {
                                actualizar.Add(new Atributo { Campo = campo, TipoDatoId = 666, ComponenteId = metadata.componente.Id_Compoente }, null);
                                continue;
                            }
                            else if (string.Compare(campo, "id_usu_alta", true) == 0)
                            {
                                actualizar.Add(new Atributo { Campo = campo, ComponenteId = metadata.componente.Id_Compoente }, Valores.Id_Usuario);
                                continue;
                            }
                        }
                        if (string.Compare(campo, "fecha_modif", true) == 0)
                        {
                            actualizar.Add(new Atributo { Campo = campo, TipoDatoId = 666, ComponenteId = metadata.componente.Id_Compoente }, null);
                        }
                        else if (string.Compare(campo, "id_usu_modif", true) == 0)
                        {
                            actualizar.Add(new Atributo { Campo = campo, ComponenteId = metadata.componente.Id_Compoente }, Valores.Id_Usuario);
                        }
                        else if ((idx = Valores.CamposTablas.FindIndex(ct => string.Compare(campo, ct, true) == 0)) != -1)
                        {
                            actualizar.Add(new Atributo
                            {
                                Campo = campo,
                                ComponenteId = metadata.componente.Id_Compoente,
                                TipoDatoId = metadata.campos.Single(a => string.Compare(campo, a.Campo, true) == 0).Id_Tipo_Dato
                            }, Valores.ValoresTablas[idx]);
                        }
                    }
                }
                using (var upsertBuilder = db.CreateSQLQueryBuilder())
                {
                    upsertBuilder.AddTable(new Componente { Esquema = metadata.componente.Esquema, Tabla = metadata.componente.Tabla, ComponenteId = metadata.componente.Id_Compoente }, null);

                    Func<int> execute = upsertBuilder.ExecuteInsert;
                    if (esNuevo)
                    {
                        upsertBuilder.AddFieldsToInsert(actualizar.ToArray());
                    }
                    else
                    {
                        execute = upsertBuilder.ExecuteUpdate;
                        upsertBuilder.AddFieldsToUpdate(actualizar.ToArray())
                                     .AddFilter(new Atributo { Campo = attrClave.Campo, TipoDatoId = attrClave.Id_Tipo_Dato, ComponenteId = attrClave.Id_Componente }, Valores.TablaID, SQLOperators.EqualsTo);
                    }
                    return Ok(execute() != 0); //true si actualiza o inserta, false si no
                }
            }
            catch (Exception ex)
            {
                Global.GetLogger().LogError(string.Format("SetAgregarRegistro(comp:{0},id:{1})", Valores.ComponentesId, Valores.TablaID), ex);
                return Ok(false);
            }
        }

        [HttpGet]
        [ResponseType(typeof(AtributoRelacionado))]
        public List<AtributoRelacionado> GetListaAtributoRelacionado(string tabla_relacion, string esquema_relacion, string campo_relacion, string descripcion_relacion)
        {
            return db.CreateSQLQueryBuilder()
                      .AddTable(esquema_relacion, tabla_relacion, "t1")
                      .AddFields(campo_relacion, descripcion_relacion)
                      .ExecuteQuery((IDataReader reader) =>
                      {
                          return new AtributoRelacionado()
                          {
                              Id_Atributo = reader.GetNullableInt64(0).GetValueOrDefault(),
                              Descripcion = reader.GetStringOrEmpty(1)
                          };
                      });
        }
    }
}