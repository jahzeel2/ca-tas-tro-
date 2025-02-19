using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using GeoSit.Data.BusinessEntities.Inmuebles.DTO;
using GeoSit.Data.BusinessEntities.ObrasPublicas;
using GeoSit.Data.DAL.Contexts;
using GeoSit.Data.DAL.Interfaces;

namespace GeoSit.Data.DAL.Repositories
{
    public class TramiteUnidadTributariaRepository : ITramiteUnidadTributariaRepository
    {
        private readonly GeoSITMContext _context;
        public TramiteUnidadTributariaRepository(GeoSITMContext context)
        {
            _context = context;
        }

        public IEnumerable<TramiteUnidadTributaria> GetTramitesUTSs(long pIdTramiteUts)
        {
            return _context.Tramite_UTS.Where(n => n.Id_Usu_Baja == null && n.Id_Tramite_Uts == pIdTramiteUts).ToList();
        }

        public TramiteUnidadTributaria GetTramiteUTSById(long pIdTramiteUts)
        {
            return _context.Tramite_UTS.FirstOrDefault(n => n.Id_Usu_Baja == null && n.Id_Tramite_Uts == pIdTramiteUts);
        }

        public IEnumerable<TramiteUnidadTributaria> GetTramiteUTSByTramite(long pIdTramite)
        {
            return _context.Tramite_UTS
                            .Include(l => l.UnidadTributaria)
                            .Where(n => n.Id_Usu_Baja == null && n.Id_Tramite == pIdTramite)
                            .ToList();
        }

        public List<NomenclaturaCertificados> GetTramiteNomenclaturaByTramite(long pIdTramite)
        {
            var query = from tr in _context.Tramite_UTS
                        join ut in _context.UnidadesTributarias on tr.Id_Unidad_Tributaria equals ut.UnidadTributariaId
                        join p in _context.Parcelas on ut.ParcelaID equals p.ParcelaID
                        join n in _context.Nomenclaturas on p.ParcelaID equals n.ParcelaID
                        where tr.Id_Tramite == pIdTramite && tr.Fecha_Baja == null
                        select new NomenclaturaCertificados
                        {
                            IdUnidadTributaria = ut.UnidadTributariaId,
                            Nomenclatura = n.Nombre,
                            Partida = ut.CodigoProvincial

                        };

            return query.ToList();
        }

        public void InsertTramiteUTS(TramiteUnidadTributaria mTramiteUts)
        {
            mTramiteUts.Id_Usu_Modif = mTramiteUts.Id_Usu_Alta;
            mTramiteUts.Fecha_Alta = DateTime.Now;
            mTramiteUts.Fecha_Modif = mTramiteUts.Fecha_Alta;
            _context.Tramite_UTS.Add(mTramiteUts);
        }

        public void DeleteTramiteUTS(TramiteUnidadTributaria mTramiteUts)
        {
            mTramiteUts.Id_Usu_Modif = mTramiteUts.Id_Usu_Baja;
            mTramiteUts.Fecha_Baja = DateTime.Now;
            mTramiteUts.Fecha_Modif = mTramiteUts.Fecha_Baja.Value;
            _context.Tramite_UTS.Attach(mTramiteUts);
            _context.Entry(mTramiteUts).State = EntityState.Modified;
        }
    }
}
