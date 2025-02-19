namespace GeoSit.Data.BusinessEntities.DeclaracionesJuradas
{
    public class DatosComputo
    {
        private const int M2_TO_HECTAREA = 10_000;

        public short CodigoZonaEcologica { get; set; }
        public int AnioBase { get; set; }
        public decimal PuntajeValuado { get; set; }
        public decimal PorcentajeDepreciacion { get; set; }
        public decimal SuperficieValuadaHA { get; set; }
        public decimal SuperficieValuadaMT2 { get { return SuperficieValuadaHA * M2_TO_HECTAREA; } }
        public decimal ValorOptimo { get; set; }
        public decimal Valuacion { get; set; }
    }
}
