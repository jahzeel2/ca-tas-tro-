using GeoSit.Data.BusinessEntities.Temporal;
using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoSit.Data.Mappers.Oracle.Temporal.Valuacion
{
    public class VALValuacionTempDeptoMapper : EntityTypeConfiguration<VALValuacionTempDepto>
    {
        public VALValuacionTempDeptoMapper()
        {
            this.ToTable("VW_VAL_VALUACION_TMP_X_DEPTO")
                .HasKey(vtd => new { vtd.Corrida, vtd.Depto });

            this.Property(vtc => vtc.Corrida)
                .HasColumnName("CORRIDA")
                .IsRequired();

            this.Property(vtd => vtd.Depto)
                .HasColumnName("DEPTO");

            this.Property(vtd => vtd.FechaProc)
                .HasColumnName("FECHA_PROC");

            this.Property(vtd => vtd.CantidadParcProc)
                .HasColumnName("CANT_PARC_PROC");

            this.Property(vtd => vtd.SupValuada)
                .HasColumnName("SUP_VALUADA_KM2");

            this.Property(vtd => vtd.ValTotal)
                .HasColumnName("VAL_TOTAL");

            this.Property(vtd => vtd.ValMax)
                .HasColumnName("VAL_MAX");

            this.Property(vtd => vtd.ValMin)
                .HasColumnName("VAL_MIN");

            this.Property(vtd => vtd.PromedioValParc)
                .HasColumnName("VAL_PROM");
        }
    }
}
