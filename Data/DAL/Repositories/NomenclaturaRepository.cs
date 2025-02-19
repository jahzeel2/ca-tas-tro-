using System.Linq;
using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Data.DAL.Interfaces;
using System;
using GeoSit.Data.DAL.Contexts;
using System.Data.Entity;
using GeoSit.Data.BusinessEntities.MapasTematicos;
using System.Collections.Generic;
using GeoSit.Data.DAL.Common.CustomErrors;
using GeoSit.Data.BusinessEntities.Inmuebles.DTO;
using Geosit.Data.DAL.DDJJyValuaciones.Enums;
using GeoSit.Data.DAL.Common.CustomErrors.Nomenclaturas;
using GeoSit.Data.BusinessEntities.ObjetosAdministrativos;

namespace GeoSit.Data.DAL.Repositories
{
    public class NomenclaturaRepository : INomenclaturaRepository
    {
        private const long NOMENCLATURA_CATASTRO = 3;

        private readonly GeoSITMContext _context;

        public NomenclaturaRepository(GeoSITMContext context)
        {
            _context = context;
        }
        public Nomenclatura GetNomenclatura(string nomenclatura)
        {
            return _context.Nomenclaturas.FirstOrDefault(x => x.Nombre == nomenclatura);
        }
        public Nomenclatura GetNomenclaturaById(long id)
        {
            return _context.Nomenclaturas.Find(id);
        }
        public void InsertNomenclatura(Nomenclatura nomenclatura)
        {
            nomenclatura.UsuarioAltaID = nomenclatura.UsuarioModificacionID;
            nomenclatura.FechaModificacion = DateTime.Now;
            nomenclatura.FechaAlta = nomenclatura.FechaModificacion;
            _context.Nomenclaturas.Add(nomenclatura);
        }

        public void UpdateNomenclatura(Nomenclatura nomenclatura)
        {
            nomenclatura.FechaModificacion = DateTime.Now;
            _context.Entry(nomenclatura).State = EntityState.Modified;
            _context.Entry(nomenclatura).Property(p => p.UsuarioAltaID).IsModified = false;
            _context.Entry(nomenclatura).Property(p => p.FechaAlta).IsModified = false;
        }


        public void UpdateNombreNomenclatura(Nomenclatura nomenclatura)
        {
            var existingNomenclatura = _context.Nomenclaturas.Find(nomenclatura.NomenclaturaID);
            if (existingNomenclatura != null)
            {
                existingNomenclatura.Nombre = nomenclatura.Nombre;
                existingNomenclatura.FechaModificacion = DateTime.Now;
                _context.Entry(existingNomenclatura).Property(n => n.Nombre).IsModified = true;
                _context.Entry(existingNomenclatura).Property(n => n.UsuarioAltaID).IsModified = false;
                _context.Entry(existingNomenclatura).Property(n => n.FechaAlta).IsModified = false;
            }
            _context.SaveChanges();
        }


        public void DeleteNomenclatura(Nomenclatura nomenclatura)
        {
            nomenclatura.UsuarioBajaID = nomenclatura.UsuarioModificacionID;
            nomenclatura.FechaModificacion = DateTime.Now;
            nomenclatura.FechaBaja = nomenclatura.FechaModificacion;
            _context.Entry(nomenclatura).State = EntityState.Modified;
            _context.Entry(nomenclatura).Property(p => p.UsuarioAltaID).IsModified = false;
            _context.Entry(nomenclatura).Property(p => p.FechaAlta).IsModified = false;
        }
        public Nomenclatura GetNomenclatura(long idParcela, long idTipoNomenclatura)
        {
            return _context.Nomenclaturas.FirstOrDefault(n => n.ParcelaID == idParcela && n.TipoNomenclaturaID == idTipoNomenclatura);
        }
        public Objeto GetObjetoByTipo(long id, string depto)
        {
            return _context.Objetos.Where(a => a.TipoObjetoId == id && a.FechaBaja == null && a.Codigo == depto).FirstOrDefault();
        }
        public string Generar(long idParcela, long tipo)
        {
            if (tipo == NOMENCLATURA_CATASTRO)
            {
                using (var builder = _context.CreateSQLQueryBuilder())
                {
                    string wkt = builder.AddTable("inm_parcela_grafica", "pg")
                                        .AddFilter("id_parcela", idParcela, Common.Enums.SQLOperators.EqualsTo)
                                        .AddFilter("fecha_baja", null, Common.Enums.SQLOperators.IsNull, Common.Enums.SQLConnectors.And)
                                        .AddGeometryField(builder.CreateGeometryFieldBuilder(new Atributo() { Campo = "geometry" }, "pg").ToWKT(), "geomwkt")
                                        .MaxResults(1)
                                        .ExecuteQuery((reader, status) =>
                                        {
                                            return reader.GetString(reader.GetOrdinal("geomwkt"));
                                        })
                                        .SingleOrDefault();

                    return ""; // new MesaEntradasRepository(_context).GenerarNomenclaturaParcela(wkt);
                }
            }
            return string.Empty;
        }

        public IEnumerable<Nomenclatura> GetByIdParcela(long idParcela)
        {
            return _context.Nomenclaturas.Where(n => n.ParcelaID == idParcela && n.FechaBaja == null);
        }

        public Tuple<string, ICustomError> ValidarDisponibilidad(NomenclaturaValidable nomenclatura)
        {
            //identifico qué tipo de nomenclatura quiere ingresar analizando qué es lo que debería venir en 0
            bool especificaRural = nomenclatura.Manzana.PadLeft(4, '0') == "0000" && nomenclatura.Partida.PadLeft(6, '0') != "000000";
            bool especificaUrbana = new[] { nomenclatura.Chacra, nomenclatura.Quinta, nomenclatura.Fraccion }.All(p => p.PadLeft(4, '0') == "0000")
                                        && nomenclatura.Manzana.Length <= 4 && nomenclatura.Manzana.PadLeft(4, '0') != "0000"
                                        && nomenclatura.Partida.PadLeft(6, '0') == "000000";
                                        
            if (!especificaRural && !especificaUrbana)
            {
                return Tuple.Create<string, ICustomError>(null, new NomenclaturaIncompleta()); //error
            }
            if (nomenclatura.IdTipoParcela == (long)TipoParcelaEnum.Urbana && especificaRural)
            {
                return Tuple.Create<string, ICustomError>(null, new NomenclaturaRuralParaParcelaUrbana()); //error
            }
            if (nomenclatura.IdTipoParcela != (long)TipoParcelaEnum.Urbana && especificaUrbana)
            {
                return Tuple.Create<string, ICustomError>(null, new NomenclaturaUrbanaParaParcelaRural()); //error
            }
            var circunscripcion = _context.Objetos.Find(nomenclatura.FeatIdCircunscripcion);
            if (circunscripcion.ObjetoPadreId != nomenclatura.FeatIdDepartamento)
            {
                return Tuple.Create<string, ICustomError>(null, new CircunscripcionNoPertenecienteDepartamento()); //error
            }

            var seccion = _context.Objetos.Find(nomenclatura.FeatIdSeccion);
            if (seccion.ObjetoPadreId != nomenclatura.FeatIdCircunscripcion)
            {
                return Tuple.Create<string, ICustomError>(null, new SeccionNoPertenecienteCircunscripcion()); //error
            }

            var departamento = _context.Objetos.Find(nomenclatura.FeatIdDepartamento);

            string formatted = ($"{departamento.Codigo.PadLeft(2, '0')}{circunscripcion.Codigo.PadLeft(3, '0')}{seccion.Codigo.PadLeft(2, '0')}" +
                                $"{nomenclatura.Chacra.PadLeft(4, '0')}{nomenclatura.Quinta.PadLeft(4, '0')}{nomenclatura.Fraccion.PadLeft(4, '0')}" +
                                $"{nomenclatura.Manzana.PadLeft(4, '0')}{nomenclatura.Parcela.PadLeft(5, '0')}").ToUpper();

            bool nomenclaturaExistente = _context.Nomenclaturas.Any(n => n.Nombre.ToUpper() == formatted);
            if (nomenclaturaExistente)
            {
                return Tuple.Create<string, ICustomError>(null, new NomenclaturaExistente()); //error
            }

            return Tuple.Create<string, ICustomError>(formatted, null); //error
        }

        public bool ValidarExistenciaNomenclatura(string nomenclatura)
        {
            return _context.Nomenclaturas.Any(n => n.Nombre.ToUpper() == nomenclatura.ToUpper());
        }
    }
}
