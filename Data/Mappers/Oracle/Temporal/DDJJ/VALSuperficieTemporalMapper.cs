﻿using GeoSit.Data.BusinessEntities.Temporal;
using GeoSit.Data.Mappers.Oracle.Temporal.Abstract;
using System.ComponentModel.DataAnnotations.Schema;

namespace GeoSit.Data.Mappers.Oracle.Temporal.DDJJ
{
    public class VALSuperficieTemporalMapper : TablaTemporalMapper<VALSuperficieTemporal>
    {
        public VALSuperficieTemporalMapper()
            : base("VAL_SUPERFICIES")
        {
            HasKey(a => a.IdSuperficie);

            Property(a => a.IdSuperficie)
                .IsRequired()
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)
                .HasColumnName("ID_SUPERFICIE");

            Property(a => a.IdAptitud)
                .IsRequired()
                .HasColumnName("ID_APTITUD");

            Property(a => a.IdSor)
                .HasColumnName("ID_DDJJ_SOR");

            Property(a => a.Superficie)
                .HasColumnName("SUPERFICIE");

            Property(a => a.TrazaDepreciable)
                .HasColumnName("TRAZA_DEPRECIABLE");

            Property(a => a.Puntaje)
                .HasColumnName("PUNTAJE");

            Property(a => a.UsuarioAlta)
                .IsRequired()
                .HasColumnName("ID_USU_ALTA");

            Property(a => a.FechaAlta)
                .IsRequired()
                .HasColumnName("FECHA_ALTA");

            Property(a => a.UsuarioModif)
                .IsRequired()
                .HasColumnName("ID_USU_MODIF");

            Property(a => a.FechaModif)
                .IsRequired()
                .HasColumnName("FECHA_MODIF");

            Property(a => a.UsuarioBaja)
                .HasColumnName("ID_USU_BAJA");

            Property(a => a.FechaBaja)
                .HasColumnName("FECHA_BAJA");

            Ignore(a => a.PuntajeSuperficie);

            HasRequired(a => a.Sor)
                .WithMany(a => a.Superficies)
                .HasForeignKey(a => a.IdSor);

            HasRequired(a => a.Aptitud)
                .WithMany()
                .HasForeignKey(a => a.IdAptitud);

            HasMany(a => a.Caracteristicas)
                .WithRequired()
                .HasForeignKey(x => x.IdSuperficie);
        }
    }
}
