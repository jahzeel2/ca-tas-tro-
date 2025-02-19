using System.Drawing;
using DevExpress.XtraReports.UI;
using System.Data;

namespace GeoSit.Reportes.Api.Reportes
{
    public partial class InformeValuacionRural : XtraReport
    {
        public InformeValuacionRural()
        {
            InitializeComponent();
        }

        internal void setcaracteristicas(DataTable tabla)
        {
            var fila = new XRTableRow();
            foreach (DataColumn col in tabla.Columns)
            {
                fila.Cells.Add(new XRTableCell() { Text = col.ColumnName, TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter, Font = new Font("Calibri", 9F, FontStyle.Regular), ForeColor = Color.Black });
            }

            xrTable.Rows.Add(fila);

            xrTable.AdjustSize();
            foreach (DataRow row in tabla.Rows)
            {
                fila = new XRTableRow();
                foreach (DataColumn col in tabla.Columns)
                {
                    var alineamiento = DevExpress.XtraPrinting.TextAlignment.TopCenter;
                    if (col.Ordinal == tabla.Columns.Count - 1)
                    {
                        alineamiento = DevExpress.XtraPrinting.TextAlignment.TopCenter;
                    }
                    if (col.Ordinal == 0)
                    {
                        fila.Cells.Add(new XRTableCell() { Text = row[col.Ordinal].ToString(), TextAlignment = alineamiento, Font = new Font("Calibri", 9F, FontStyle.Regular), ForeColor = Color.Black });
                    }
                    else
                    {
                        var item = row[col.Ordinal].ToString();
                        if (string.IsNullOrEmpty(item))
                        {
                            fila.Cells.Add(new XRTableCell() { Text = "-", TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter, Font = new Font("Calibri", 9F, FontStyle.Bold) });
                        }
                        else
                        {
                            fila.Cells.Add(new XRTableCell() { Text = row[col.Ordinal].ToString(), TextAlignment = alineamiento, Font = new Font("Calibri", 9F, FontStyle.Bold) });
                        }
                    }

                }

                xrTable.Rows.Add(fila);


            }

        }

    }
}