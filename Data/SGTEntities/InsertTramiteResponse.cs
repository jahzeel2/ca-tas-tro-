namespace SGTEntities
{
    public class InsertTramiteResponse : TramiteResponse
    {
        public int TramiteId {  get; set; }
        public string TramiteIdentificacion {  get; set; }
        public int TramiteJurisdiccionId { get; set; }
        public int TramiteAnio { get; set; }
        public int TramiteNumero { get; set; }
        public string TramiteCodigo { get; set; }
    }
}
