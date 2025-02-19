namespace SGTEntities
{
    public class EParte
    {
        public string Eparte { get; set; }
        public string EparteNombre { get; set; }
        public int EparteOrden { get; set; }
        internal EParte() { }

        private EParte(string nombre, int numero, string eparte)
        {
            EparteNombre = nombre;
            EparteOrden = numero;
            Eparte = eparte;
        }

        public static EParte Create(string nombre, int numeroParte, string generatedText)
        {
            return new EParte(nombre, numeroParte, generatedText);
        }
    }
}
