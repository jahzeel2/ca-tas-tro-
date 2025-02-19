using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using DevExpress.XtraReports.UI;

namespace GeoSit.Reportes.Api.Reportes
{
    public partial class MESubReporteFooter : DevExpress.XtraReports.UI.XtraReport
    {
        public MESubReporteFooter(string operador = "-")
        {
            InitializeComponent();
            lblEmitido.Text = lblEmitido.Text = "Emitido: " + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt", new System.Globalization.CultureInfo("en-US")) + Environment.NewLine + "Operador: " + operador;
        }
    }
}
