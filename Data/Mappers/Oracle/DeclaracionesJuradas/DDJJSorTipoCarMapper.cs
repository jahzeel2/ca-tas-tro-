using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System.Data.Entity.ModelConfiguration;

namespace GeoSit.Data.Mappers.Oracle.DeclaracionesJuradas
{
    public class DDJJSorTipoCarMapper : EntityTypeConfiguration<DDJJSorTipoCaracteristica>
    {
        public DDJJSorTipoCarMapper()
        {
            ToTable("VAL_SOR_TIPO_CAR")
                .HasKey(a => a.IdSorTipoCaracteristica);

            Property(a => a.IdSorTipoCaracteristica)
                .IsRequired()
                .HasColumnName("ID_SOR_TIPO_CAR");

            Property(a => a.Descripcion)
                .IsRequired()
                .HasColumnName("DESCRIPCION");

            Property(a => a.IdGrupoCaracteristica)
                .IsRequired()
                .HasColumnName("ID_SOR_GRUPO_CAR");

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


            HasRequired(x => x.GrupoCaracteristica)
                .WithMany(x => x.Tipos)
                .HasForeignKey(x => x.IdGrupoCaracteristica);
        }
    }
}
