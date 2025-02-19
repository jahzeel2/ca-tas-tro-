using GeoSit.Data.BusinessEntities.Temporal;
using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoSit.Data.Mappers.Oracle.Temporal.Valuacion
{
    public class VALValuacionTempCorridaMapper : EntityTypeConfiguration<VALValuacionTempCorrida>
    {
        public VALValuacionTempCorridaMapper()
        {
            this.ToTable("VW_VAL_VALUACION_TMP_X_CORRIDA")
                .HasKey(vtc => vtc.Corrida);

            this.Property(vtc => vtc.Corrida)
                .HasColumnName("CORRIDA")
                .IsRequired();

            this.Property(vtc => vtc.FechaProc)
                .HasColumnName("MAX");
          
            this.Property(vtc => vtc.CantidadParcProc)
                .HasColumnName("CANT_PARC_PROC");

            this.Property(vtc => vtc.SupValuada)
                .HasColumnName("SUP_VALUADA_KM2");

            this.Property(vtc => vtc.ValTotal)
                .HasColumnName("VAL_TOTAL");

            this.Property(vtc => vtc.ValMax)
                .HasColumnName("VAL_MAX");

            this.Property(vtc => vtc.ValMin)
                .HasColumnName("VAL_MIN");

            this.Property(vtc => vtc.PromedioValParc)
                .HasColumnName("PROM_VAL_PARCELA");   
        }
    }
}
