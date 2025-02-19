using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System.Data.Entity.ModelConfiguration;

namespace GeoSit.Data.Mappers.Oracle.DeclaracionesJuradas
{
    public class DDJJSorCaracteristicasMapper : EntityTypeConfiguration<DDJJSorCaracteristicas>
    {
        public DDJJSorCaracteristicasMapper()
        {
            ToTable("VAL_SOR_CARACTERISTICAS")
                .HasKey(a => a.IdSorCaracteristica);

            Property(a => a.IdSorCaracteristica)
                .IsRequired()
                .HasColumnName("ID_SOR_CAR");

            Property(a => a.IdSorTipoCaracteristica)
                .IsRequired()
                .HasColumnName("ID_SOR_TIPO_CAR");

            Property(a => a.Puntaje)
                .IsRequired()
                .HasColumnName("PUNTAJE");

            Property(a => a.PuntajeDepreciable)
                .IsRequired()
                .HasColumnName("PUNTAJE_DEPRECIABLE");

            Property(a => a.Descripcion)
               .IsRequired()
               .HasColumnName("DESCRIPCION");         

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

            HasRequired(a => a.TipoCaracteristica)
                .WithMany(a=> a.Caracteristicas)
                .HasForeignKey(a => a.IdSorTipoCaracteristica);
        }
    }
}
