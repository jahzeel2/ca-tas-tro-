using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System.Data.Entity.ModelConfiguration;

namespace GeoSit.Data.Mappers.Oracle.DeclaracionesJuradas
{
    public class DDJJSorGrupoCaracteristicaMapper : EntityTypeConfiguration<DDJJSorGrupoCaracteristica>
    {
        public DDJJSorGrupoCaracteristicaMapper()
        {
            ToTable("VAL_SOR_GRUPO_CARACTERISTICA")
                .HasKey(a => a.IdGrupoCaracteristica);

            Property(a => a.IdGrupoCaracteristica)
                .IsRequired()
                .HasColumnName("ID_SOR_GRUPO_CAR");

            Property(a => a.Descripcion)
                .IsRequired()
                .HasColumnName("DESCRIPCION");

            Property(a => a.Maestra)
                .IsRequired()
                .HasColumnName("MAESTRA");

            HasMany(x => x.Aptitudes)
                .WithMany(p => p.GruposCaracteristicas)
                .Map(x => x.ToTable("VAL_SOR_GRUPO_APTITUD")
                           .MapLeftKey("ID_SOR_GRUPO_CAR")
                           .MapRightKey("ID_APTITUD"));


        }
    }
}
