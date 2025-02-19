﻿using GeoSit.Data.BusinessEntities.Inmuebles;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace GeoSit.Data.Mappers.Oracle
{
    public class ParcelaGraficaMapper : EntityTypeConfiguration<ParcelaGrafica>
    {
        public ParcelaGraficaMapper()
        {
            ToTable("INM_PARCELA_GRAFICA")
                .HasKey(x => x.FeatID);

            Property(p => p.FeatID)
                .HasColumnName("FEATID")
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None)
                .IsRequired();

            Property(p => p.ParcelaID)
                .HasColumnName("ID_PARCELA")
                .IsOptional();

            Property(p => p.FechaAlta)
                .HasColumnName("FECHA_ALTA")
                .IsOptional();

            Property(p => p.IdOrigen)
                .HasColumnName("ID_ORIGEN")
                .IsOptional();

            Property(p => p.Nomenclatura)
                .HasColumnName("NOMENCLATURA")
                .IsOptional();

            Property(p => p.FechaBaja)
                .HasColumnName("FECHA_BAJA")
                .IsOptional();

            Property(p => p.FechaModificacion)
                .HasColumnName("FECHA_MODIF")
                .IsOptional();

            Property(p => p.UsuarioAltaID)
                .HasColumnName("ID_USU_ALTA")
                .IsOptional();

            Property(p => p.UsuarioBajaID)
                .HasColumnName("ID_USU_BAJA")
                .IsOptional();

            Property(p => p.UsuarioModificacionID)
                .HasColumnName("ID_USU_MODIF")
                .IsOptional();
        }
    }
}
