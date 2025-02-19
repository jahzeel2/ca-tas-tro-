using DevExpress.XtraReports.UI;
using GeoSit.Data.BusinessEntities.Temporal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

namespace GeoSit.Reportes.Api.Reportes
{
    public partial class InformeDetalleCorrida : DevExpress.XtraReports.UI.XtraReport
    {
        internal InformeDetalleCorrida(List<VALValuacionTempDepto> valValuacionTmpDeptoList, string usuario) : this()
        {
            DataSource = valValuacionTmpDeptoList;
            rptFooter.ReportSource = new MESubReporteFooter(usuario);
            rptHeader.ReportSource = new MESubReporteHeader2();
            rptHeader.ReportSource.FindControl("txtTitulo", true).Text = "Detalle de Corrida Temporal N°" + valValuacionTmpDeptoList.FirstOrDefault().Corrida;
            rptHeader.ReportSource.FindControl("txtTitulo", true).Font = new System.Drawing.Font("Calibri", 17, System.Drawing.FontStyle.Bold);
            rptHeader.ReportSource.FindControl("lblPartida", true).Visible = false;
            rptHeader.ReportSource.FindControl("txtPartida", true).Visible = false;
        } 

        public InformeDetalleCorrida()
        {
            InitializeComponent();
        }

    }
}
