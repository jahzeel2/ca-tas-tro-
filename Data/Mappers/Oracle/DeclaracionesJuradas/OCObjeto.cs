using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace GeoSit.Data.Mappers.Oracle
{
    public class OCObjetoMapper : EntityTypeConfiguration<OCObjeto>
    {
        public OCObjetoMapper()
        {
            ToTable("OC_OBJETO")
                .HasKey(a => a.FeatId);

            Property(a => a.FeatId)
                .IsRequired()
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity)
                .HasColumnName("FEATID");
            
            Property(a => a.Codigo)
                .HasColumnName("CODIGO");
            Property(a => a.Descripcion)
                .HasColumnName("DESCRIPCION");
            Property(a => a.Atributos)
                .HasColumnName("ATRIBUTOS");
            Property(a => a.Nombre)
                .HasColumnName("NOMBRE");                 

            Property(a => a.GeomTxt)
                .HasColumnName("GEOM_TXT");
            Property(a => a.IdSubtipoObjeto)
                .HasColumnName("ID_SUBTIPO_OBJETO")
                .IsRequired();
            Property(a => a.IdUsuarioAlta)
                .IsRequired()
                .HasColumnName("ID_USU_ALTA");
            Property(a => a.FechaAlta)
                .IsRequired()
                .HasColumnName("FECHA_ALTA");
            Property(a => a.IdUsuarioModif)
                .HasColumnName("ID_USU_MODIF");
            Property(a => a.FechaModif)
                .HasColumnName("FECHA_MODIF");
            Property(a => a.IdUsuarioBaja)
                .HasColumnName("ID_USU_BAJA");
            Property(a => a.FechaBaja)
                .HasColumnName("FECHA_BAJA");
            
            Ignore(a => a.Geometry);
                 

        }
    }
}
