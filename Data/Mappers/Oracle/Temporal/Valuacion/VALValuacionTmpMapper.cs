using GeoSit.Data.BusinessEntities.Temporal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoSit.Data.Mappers.Oracle.Temporal.Valuacion
{
    public class VALValuacionTmpMapper : EntityTypeConfiguration<VALValuacionTmp>
    {
        public VALValuacionTmpMapper()
        {
            this.ToTable("VAL_VALUACION_TMP")
                .HasKey(a => a.IdValuacion);

            Property(a => a.IdValuacion)
                .IsRequired()
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)
                .HasColumnName("ID_VALUACION");

            Property(a => a.IdUnidadTributaria)
                .IsRequired()
                .HasColumnName("ID_UNIDAD_TRIBUTARIA");

            Property(a => a.IdDDJJ)
                .IsRequired()
                .HasColumnName("ID_DDJJ");

            Property(a => a.FechaDesde)
                .IsRequired()
                .HasColumnName("FECHA_DESDE");

            Property(a => a.FechaHasta)
                .HasColumnName("FECHA_HASTA");

            Property(a => a.ValorTierra)
                .HasColumnName("VALOR_TIERRA");

            Property(a => a.ValorMejoras)
                .HasColumnName("VALOR_MEJORAS");

            Property(a => a.ValorMejorasPropio)
                .HasColumnName("VALOR_MEJORAS_PROPIO");

            Property(a => a.ValorTotal)
                .HasColumnName("VALOR_TOTAL");

            Property(a => a.Superficie)
                .HasColumnName("SUPERFICIE");

            Property(a => a.CoefProrrateo)
                .HasColumnName("COEF_PRORRATEO");

            Property(a => a.IdUsuarioAlta)
                .IsRequired()
                .HasColumnName("ID_USU_ALTA");

            Property(a => a.FechaAlta)
                .IsRequired()
                .HasColumnName("FECHA_ALTA");

            Property(a => a.IdUsuarioModif)
                .IsRequired()
                .HasColumnName("ID_USU_MODIF");

            Property(a => a.FechaModif)
                .IsRequired()
                .HasColumnName("FECHA_MODIF");

            Property(a => a.IdUsuarioBaja)
                .HasColumnName("ID_USU_BAJA");

            Property(a => a.FechaBaja)
                .HasColumnName("FECHA_BAJA");

            this.Property(a => a.Corrida)
               .HasColumnName("CORRIDA");

            //HasRequired(a => a.UnidadTributaria)
                //.WithMany()
                //.HasForeignKey(a => new { a.IdUnidadTributaria, a.IdTramite });
        }
    }
}
