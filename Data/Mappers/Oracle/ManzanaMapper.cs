using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using GeoSit.Data.BusinessEntities.ModuloPloteo;

namespace GeoSit.Data.Mappers.Oracle
{
    public class ManzanaMapper : EntityTypeConfiguration<Manzana>
    {
        public ManzanaMapper()
        {
            ToTable("CT_DIVISION")
                .HasKey(a => a.FeatId);
            Property(a => a.FeatId)
                .IsRequired()
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.None)
                .HasColumnName("FEATID");

            Property(a => a.Nomenclatura)
                .IsRequired()
                .HasColumnName("DIV_DESCRIPTOR");
            
            Ignore(a => a.Geom);
        }
    }
}
