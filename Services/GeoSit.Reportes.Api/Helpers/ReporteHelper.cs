using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using GeoSit.Data.BusinessEntities.Inmuebles;
using GeoSit.Data.BusinessEntities.ObrasPublicas;
using GeoSit.Data.BusinessEntities.ObrasParticulares;
using GeoSit.Reportes.Api.Reportes;
using GeoSit.Data.BusinessEntities.Documentos;
using GeoSit.Data.BusinessEntities.MesaEntradas;
using GeoSit.Reportes.Api.Models;
using GeoSit.Data.BusinessEntities.Personas;
using GeoSit.Data.BusinessEntities.DeclaracionesJuradas;
using System.Data;
using GeoSit.Data.BusinessEntities.Certificados;
using System.Globalization;
using GeoSit.Data.BusinessEntities.Seguridad;
using GeoSit.Data.BusinessEntities.Temporal;
using GeoSit.Data.BusinessEntities.MesaEntradas.DTO;

namespace GeoSit.Reportes.Api.Helpers
{
    public static class ReporteHelper
    {
        public static byte[] GenerarReporte(XtraReport reporte, Parcela parcela, VALValuacion valValuacion, ParcelaSuperficies parcelaSuperficies, Dictionary<string, double> parcelaRuralSuperficies, string usuarioImpresion)
        {
            MESubReporteHeader3 subReporteHeader3 = new MESubReporteHeader3();
            MESubReporteFooter subReporteFooter = new MESubReporteFooter(usuarioImpresion);
            var unidad = parcela.UnidadesTributarias.Where(x => x.TipoUnidadTributariaID == 1 || x.TipoUnidadTributariaID == 2).Select(a => a.CodigoProvincial).FirstOrDefault();

            SetLogo2(subReporteHeader3, "imgLogo");
            subReporteHeader3.FindControl("txtTitulo", true).Text = "Certificación Catastral";
            subReporteHeader3.FindControl("lblPartida", true).Visible = true;
            //subReporteHeader3.FindControl("txtPartida", true).Text = unidad.ToString();
            subReporteHeader3.FindControl("txtPartida", true).Text = (unidad ?? "0").ToString();
            subReporteHeader3.FindControl("txtPartida", true).Visible = true;

            //Nomenclatura
            XRLabel txtDep = (XRLabel)subReporteHeader3.FindControl("txtDep", true);
            XRLabel txtCirc = (XRLabel)subReporteHeader3.FindControl("txtCirc", true);
            XRLabel txtSec = (XRLabel)subReporteHeader3.FindControl("txtSec", true);
            XRLabel txtCha = (XRLabel)subReporteHeader3.FindControl("txtCha", true);
            XRLabel txtQui = (XRLabel)subReporteHeader3.FindControl("txtQui", true);
            XRLabel txtFra = (XRLabel)subReporteHeader3.FindControl("txtFra", true);
            XRLabel txtMan = (XRLabel)subReporteHeader3.FindControl("txtMan", true);
            XRLabel txtPar = (XRLabel)subReporteHeader3.FindControl("txtPar", true);
            string nomenclatura = parcela.Nomenclaturas.FirstOrDefault().Nombre.ToString();

            if (!string.IsNullOrEmpty(nomenclatura))
            {
                string departamento = nomenclatura.Substring(0, 2);
                txtDep.Text = departamento;

                string cicunscripcion = nomenclatura.Substring(2, 3);
                txtCirc.Text = cicunscripcion;

                string seccion = nomenclatura.Substring(5, 2).ToUpper();
                txtSec.Text = seccion;

                string chacra = nomenclatura.Substring(7, 4);
                txtCha.Text = chacra;

                string quinta = nomenclatura.Substring(11, 4);
                txtQui.Text = quinta;

                string fraccion = nomenclatura.Substring(15, 4);
                txtFra.Text = fraccion;

                string manzana = nomenclatura.Substring(19, 4);
                txtMan.Text = manzana;

                string parcelaNom = nomenclatura.Substring(23, 5).ToUpper();
                txtPar.Text = parcelaNom;
            }
           
            (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
            (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader3;

            XRLabel lblCantUt = (XRLabel)reporte.FindControl("lblCantUt", true);
            var CantUt = parcela.UnidadesTributarias.Count().ToString();

            //Afecta PH
            XRLabel lblAfectaPh = (XRLabel)reporte.FindControl("lblAfectaPh", true);
            if (parcela.UnidadesTributarias.Where(x => x.TipoUnidadTributariaID == 3).Count() >= 1)
            {
                lblAfectaPh.Text = "SI";
            }
            else
            {
                lblAfectaPh.Text = "NO";
            }

            //Apartado Valuacion partida afectada a PH
            DetailReportBand DetailReportValuaciones = (DetailReportBand)reporte.FindControl("DetailReportValuaciones", true);
            if (parcela.ClaseParcelaID == 5)
            {
                DetailReportValuaciones.Visible = false;
            }
            else
            {
                DetailReportValuaciones.Visible = true;
            }

            //Cantidad Unidades
            if (parcela.UnidadesTributarias.Count() > 1)
            {
                lblCantUt.Text = $"({CantUt})";
            }
            else
            {
                lblCantUt.Text = "";
            }

            //Valuacion
            XRLabel lblValorTierra = (XRLabel)reporte.FindControl("lblValorTierra", true);
            XRLabel lblFecha = (XRLabel)reporte.FindControl("lblFecha", true);
            XRLabel lblValorTotal = (XRLabel)reporte.FindControl("lblValorTotal", true);

            if ((valValuacion?.IdValuacion ?? 0) > 0)
            {
                lblValorTierra.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", valValuacion.ValorTierra);
                lblValorTotal.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", valValuacion.ValorTotal);
                lblFecha.Text = valValuacion.FechaDesde.ToShortDateString();
            }
            else
            {
                lblValorTierra.Text = " - ";
                lblValorTotal.Text = " - ";
                lblFecha.Text = " - ";
            }

            //Superficies
            /*XRLabel lblTierra = (XRLabel)reporte.FindControl("lblTierra", true);
            XRLabel lblMejora = (XRLabel)reporte.FindControl("lblMejora", true);
            if (parcela.TipoParcelaID == 2 || parcela.TipoParcelaID == 3)
            {
                lblTierra.Text = "Las medidas están expresadas en ha";
            }
            else
            {
                lblTierra.Text = "Las medidas están expresadas en m²";
            }*/

            //Superficie Tierra
            //Registrada
            /*XRLabel lblSupCatastro = (XRLabel)reporte.FindControl("lblSupCatastro", true);
            XRLabel lblSupTitulo = (XRLabel)reporte.FindControl("lblSupTitulo", true);
            XRLabel lblSupMensura = (XRLabel)reporte.FindControl("lblSupMensura", true);
            var SupCatastro = parcelaSuperficies.AtributosParcela.Catastro;
            var SupTitulo = parcelaSuperficies.AtributosParcela.Titulo;
            var SupMensura = parcelaSuperficies.AtributosParcela.Mensura;
            lblSupCatastro.Text = String.Format(new CultureInfo("es-AR"), "{0:N2}", SupCatastro);
            lblSupTitulo.Text = String.Format(new CultureInfo("es-AR"), "{0:N2}", SupTitulo);
            lblSupMensura.Text = String.Format(new CultureInfo("es-AR"), "{0:N2}", SupMensura);*/

            //Superficie Rural
            /*
            XRTable xrTable11 = (XRTable)reporte.FindControl("xrTable11", true);

            if (xrTable11 != null)
            {
                foreach (var kvp in parcelaRuralSuperficies)
                {
                    var supRural = kvp.Value.ToString("0.0000");

                    XRTableRow dataRow = new XRTableRow();
                    XRTableCell cellKey = new XRTableCell()
                    {
                        Text = kvp.Key,
                        ForeColor = Color.FromArgb(119, 121, 118),
                    };
                    XRTableCell cellValue = new XRTableCell()
                    {
                        Text = supRural,
                        ForeColor = Color.FromArgb(119, 121, 118),
                    };

                    // Establecer el estilo de fuente para desactivar el negrita
                    cellKey.Font = new Font("Calibri", 12F, FontStyle.Regular);
                    cellValue.Font = new Font("Calibri", 12F, FontStyle.Regular);
                    cellValue.TextAlignment = TextAlignment.MiddleRight;

                    dataRow.Cells.Add(cellKey);
                    dataRow.Cells.Add(cellValue);
                    xrTable11.Rows.Add(dataRow);
                }
            }
            */

            // Registrada
            CargarSuperficie(
                (XRLabel)reporte.FindControl("lblHas", true),
                (XRLabel)reporte.FindControl("lblAs", true),
                (XRLabel)reporte.FindControl("lblMetro", true),
                (XRLabel)reporte.FindControl("lblDm", true),
                (XRLabel)reporte.FindControl("lblCm", true),
                parcela.Superficie,
                parcela.TipoParcelaID
            );

            // Relevada
            CargarSuperficie(
                (XRLabel)reporte.FindControl("lblHasGraf", true),
                (XRLabel)reporte.FindControl("lblAsGraf", true),
                (XRLabel)reporte.FindControl("lblMetroGraf", true),
                (XRLabel)reporte.FindControl("lblDmGraf", true),
                (XRLabel)reporte.FindControl("lblCmGraf", true),
                parcela.SuperficieGrafica,
                parcela.TipoParcelaID, 
                true
            );


            /* SUPERFICIES DE MEJORAS
            //Superficies Construcciones
            //Registrada
            XRLabel lblCubiertaReg = (XRLabel)reporte.FindControl("lblCubiertaReg", true);
            XRLabel lblNegocioReg = (XRLabel)reporte.FindControl("lblNegocioReg", true);
            XRLabel lblSemicubiertaReg = (XRLabel)reporte.FindControl("lblSemicubiertaReg", true);
            XRLabel lblTotalConsReg = (XRLabel)reporte.FindControl("lblTotalConsReg", true);
            XRLabel lblPileta = (XRLabel)reporte.FindControl("lblPileta", true);
            XRLabel lblPavimento = (XRLabel)reporte.FindControl("lblPavimento", true);
            XRLabel lblTotalOtraMejoraReg = (XRLabel)reporte.FindControl("lblTotalOtraMejoraReg", true);
            XRLabel lblSupTotalConsReg = (XRLabel)reporte.FindControl("lblSupTotalConsReg", true);
            if (parcelaSuperficies != null && parcelaSuperficies.RelevamientoParcela != null)
            {
                var SupCubiertaReg = parcelaSuperficies.DGCMejorasConstrucciones.Cubierta;
                var SupNegocioReg = parcelaSuperficies.DGCMejorasConstrucciones.Negocio;
                var SupSemicubiertaReg = parcelaSuperficies.DGCMejorasConstrucciones.Semicubierta;
                var TotalConsReg = parcelaSuperficies.DGCMejorasConstrucciones.Total;
                var SupPileta = parcelaSuperficies.DGCMejorasOtras.Piscina;
                var SupPavimento = parcelaSuperficies.DGCMejorasOtras.Pavimento;
                var TotalOtrasReg = parcelaSuperficies.DGCMejorasOtras.Total;

                lblPileta.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupPileta);
                lblPavimento.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupPavimento);
                lblTotalOtraMejoraReg.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", TotalOtrasReg);
                lblCubiertaReg.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupCubiertaReg);
                lblNegocioReg.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupNegocioReg);
                lblSemicubiertaReg.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupSemicubiertaReg);
                lblTotalConsReg.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", TotalConsReg);
                lblSupTotalConsReg.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", (TotalConsReg + TotalOtrasReg));
            }
            else
            {
                lblCubiertaReg.Text = "0.00";
                lblNegocioReg.Text = "0.00";
                lblSemicubiertaReg.Text = "0.00";
                lblTotalConsReg.Text = "0.00";
                lblPileta.Text = "0.00";
                lblPavimento.Text = "0.00";
                lblTotalOtraMejoraReg.Text = "0.00";
                lblSupTotalConsReg.Text = "0.00";
            }

            //Relevada
            XRLabel lblCubiertaRel = (XRLabel)reporte.FindControl("lblCubiertaRel", true);
            XRLabel lblGalponRel = (XRLabel)reporte.FindControl("lblGalponRel", true);
            XRLabel lblSemicubiertaRel = (XRLabel)reporte.FindControl("lblSemicubiertaRel", true);
            XRLabel lblTotalConsRel = (XRLabel)reporte.FindControl("lblTotalConsRel", true);
            XRLabel lblPiscina = (XRLabel)reporte.FindControl("lblPiscina", true);
            XRLabel lblDeportiva = (XRLabel)reporte.FindControl("lblDeportiva", true);
            XRLabel lblEnconstruccion = (XRLabel)reporte.FindControl("lblEnconstruccion", true);
            XRLabel lblPrecaria = (XRLabel)reporte.FindControl("lblPrecaria", true);
            XRLabel lblTotalOtraMejoraRel = (XRLabel)reporte.FindControl("lblTotalOtraMejoraRel", true);
            XRLabel lblSupTotalConsRel = (XRLabel)reporte.FindControl("lblSupTotalConsRel", true);

            if (parcelaSuperficies != null && parcelaSuperficies.RelevamientoParcela != null)
            {
                var SupCubiertaRel = parcelaSuperficies.RelevamientoMejorasConstrucciones.Cubierta;
                var SupGalponRel = parcelaSuperficies.RelevamientoMejorasConstrucciones.Galpon;
                var SupSemicubiertaRel = parcelaSuperficies.RelevamientoMejorasConstrucciones.Semicubierta;
                var TotalConsRel = parcelaSuperficies.RelevamientoMejorasConstrucciones.Total;
                var SupPiscina = parcelaSuperficies.RelevamientoMejorasOtras.Piscina;
                var SupDeportiva = parcelaSuperficies.RelevamientoMejorasOtras.Deportiva;
                var SupEnconstruccion = parcelaSuperficies.RelevamientoMejorasOtras.Construccion;
                var SupPrecaria = parcelaSuperficies.RelevamientoMejorasOtras.Precaria;
                var TotalOtrasRel = parcelaSuperficies.RelevamientoMejorasOtras.Total;

                lblCubiertaRel.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupCubiertaRel);
                lblGalponRel.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupGalponRel);
                lblSemicubiertaRel.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupSemicubiertaRel);
                lblTotalConsRel.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", TotalConsRel);
                lblPiscina.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupPiscina);
                lblDeportiva.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupDeportiva);
                lblEnconstruccion.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupEnconstruccion);
                lblPrecaria.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupPrecaria);
                lblTotalOtraMejoraRel.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", TotalOtrasRel);
                lblSupTotalConsRel.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", (TotalConsRel + TotalOtrasRel));
            }
            else
            {
                lblCubiertaRel.Text = "0.00";
                lblGalponRel.Text = "0.00";
                lblSemicubiertaRel.Text = "0.00";
                lblTotalConsRel.Text = "0.00";
                lblPiscina.Text = "0.00";
                lblDeportiva.Text = "0.00";
                lblEnconstruccion.Text = "0.00";
                lblPrecaria.Text = "0.00";
                lblTotalOtraMejoraRel.Text = "0.00";
                lblSupTotalConsRel.Text = "0.00";
            }
            */
            XRTableCell tblUnidad = (XRTableCell)reporte.FindControl("tblUnidad", true);
            if (parcela.ClaseParcelaID == 4)
            {
                tblUnidad.Text = "PH ESPECIAL";
            }


            return GenerarReporte(reporte, parcela, usuarioImpresion);

        }

        public static void CargarSuperficie(XRLabel lblHas, XRLabel lblAs, XRLabel lblMet, XRLabel lblDm, XRLabel lblCm, decimal? superficie, long tipoParcelaID, bool esSupGrafica = false)
        {
            if (superficie == null)
            {
                lblHas.Text = lblAs.Text = lblMet.Text = lblDm.Text = lblCm.Text = "-";
                return;
            }
            decimal superficieAjustada = (tipoParcelaID == 2 || tipoParcelaID == 3) && esSupGrafica ? superficie.Value * 10_000 : superficie.Value;
            var (hectareas, areas, metros, decimetros, centimetros) = SuperficieModel.DescomponerSuperficie(superficieAjustada);
            lblHas.Text = hectareas.ToString();
            lblAs.Text = areas.ToString();
            lblMet.Text = metros.ToString();
            lblDm.Text = decimetros.ToString();
            lblCm.Text = centimetros.ToString();
        }

        public static XRTableRow CrearFilaSuperficie(string clave, double supBase)
        {
            var superficieModel = SuperficieModel.DescomponerSuperficie((decimal)supBase);
            XRTableRow dataRow = new XRTableRow();
            dataRow.Cells.Add(CrearCeldaSup(clave));
            dataRow.Cells.Add(CrearCeldaSup(superficieModel.Hectarea.ToString()));
            dataRow.Cells.Add(CrearCeldaSup(superficieModel.Area.ToString()));
            dataRow.Cells.Add(CrearCeldaSup(superficieModel.M2.ToString()));
            dataRow.Cells.Add(CrearCeldaSup(superficieModel.Dm2.ToString()));
            dataRow.Cells.Add(CrearCeldaSup(superficieModel.Cm2.ToString()));

            return dataRow;
        }

        public static XRTableCell CrearCeldaSup(string texto)
        {
            return new XRTableCell()
            {
                Text = texto,
                ForeColor = Color.FromArgb(119, 121, 118),
                Font = new Font("Calibri", 12F, FontStyle.Regular),
                TextAlignment = TextAlignment.MiddleRight,
            };
        }

        public static byte[] GenerarReporte(XtraReport reporte, Parcela parcela, VALValuacion valValuacion, string usuarioImpresion)
        {
            MESubReporteHeader3 subReporteHeader3 = new MESubReporteHeader3();
            MESubReporteFooter subReporteFooter = new MESubReporteFooter(usuarioImpresion);
            var unidad = parcela.UnidadesTributarias.Select(a => a.CodigoProvincial).FirstOrDefault();
            string unidadFuncional = parcela.UnidadesTributarias.Select(a => a.UnidadFuncional).FirstOrDefault();

            SetLogo2(subReporteHeader3, "imgLogo");
            subReporteHeader3.FindControl("txtTitulo", true).Text = "Certificado Catastral";
            subReporteHeader3.FindControl("lblPartida", true).Visible = true;
            subReporteHeader3.FindControl("txtPartida", true).Text = unidad.ToString();
            subReporteHeader3.FindControl("txtPartida", true).Visible = true;

            subReporteHeader3.FindControl("lblUf", true).Visible = true;
            subReporteHeader3.FindControl("txtUf", true).Visible = true;
            subReporteHeader3.FindControl("txtUf", true).Text = (!string.IsNullOrEmpty(unidadFuncional) ? unidadFuncional : "N/A");

            //Nomenclatura
            XRLabel txtDep = (XRLabel)subReporteHeader3.FindControl("txtDep", true);
            XRLabel txtCirc = (XRLabel)subReporteHeader3.FindControl("txtCirc", true);
            XRLabel txtSec = (XRLabel)subReporteHeader3.FindControl("txtSec", true);
            XRLabel txtCha = (XRLabel)subReporteHeader3.FindControl("txtCha", true);
            XRLabel txtQui = (XRLabel)subReporteHeader3.FindControl("txtQui", true);
            XRLabel txtFra = (XRLabel)subReporteHeader3.FindControl("txtFra", true);
            XRLabel txtMan = (XRLabel)subReporteHeader3.FindControl("txtMan", true);
            XRLabel txtPar = (XRLabel)subReporteHeader3.FindControl("txtPar", true);
            string nomenclatura = parcela.Nomenclaturas.FirstOrDefault().Nombre.ToString();

            if (!string.IsNullOrEmpty(nomenclatura))
            {
                string departamento = nomenclatura.Substring(0, 2);
                txtDep.Text = departamento;

                string cicunscripcion = nomenclatura.Substring(2, 3);
                txtCirc.Text = cicunscripcion;

                string seccion = nomenclatura.Substring(5, 2).ToUpper();
                txtSec.Text = seccion;

                string chacra = nomenclatura.Substring(7, 4);
                txtCha.Text = chacra;

                string quinta = nomenclatura.Substring(11, 4);
                txtQui.Text = quinta;

                string fraccion = nomenclatura.Substring(15, 4);
                txtFra.Text = fraccion;

                string manzana = nomenclatura.Substring(19, 4);
                txtMan.Text = manzana;

                string parcelaNom = nomenclatura.Substring(23, 5).ToUpper();
                txtPar.Text = parcelaNom;
            }

            (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
            (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader3;

            //Valuacion
            XRLabel lblValorTierra = (XRLabel)reporte.FindControl("lblValorTierra", true);
            XRLabel lblValorTotal = (XRLabel)reporte.FindControl("lblValorTotal", true);
            XRLabel lblFechaVigencia = (XRLabel)reporte.FindControl("lblFechaVigencia", true);

            if ((valValuacion?.IdValuacion ?? 0) > 0)
            {
                lblValorTierra.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", valValuacion.ValorTierra);
                lblValorTotal.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", valValuacion.ValorTotal);
                lblFechaVigencia.Text = valValuacion.FechaDesde.ToShortDateString();
            }
            else
            {
                lblValorTierra.Text = " - ";
                lblValorTotal.Text = " - ";
                lblFechaVigencia.Text = " - ";
            }

            /*XRLabel lblTitular = (XRLabel)reporte.FindControl("lblTitular", true);
            var claseParcela = parcela.ClaseParcelaID;

            if (parcela.Dominios.Any())
            {
                if (claseParcela == 2)
                {
                    lblTitular.Text = "Poseedores";
                }
                else
                {
                    lblTitular.Text = "Titulares";
                }
            }*/

            XRLabel lblUnidad = (XRLabel)reporte.FindControl("lblUnidad", true);
            XRLabel lblTipoUnidad = (XRLabel)reporte.FindControl("lblTipoUnidad", true);
            if (parcela.ClaseParcelaID == 6)
            {
                lblUnidad.Text = "Unidad Parcelaria";
                lblTipoUnidad.Text = "UP de CI";
            }
            else if (parcela.ClaseParcelaID == 5)
            {
                lblTipoUnidad.Text = "UF de PH";
            }
            else
            {
                lblTipoUnidad.Text = "COMUN";
            }

            return GenerarReporte(reporte, parcela, usuarioImpresion);
        }

        public static byte[] GenerarReporteCambioTitularidad(XtraReport reporte, Parcela parcela, string usuarioImpresion, Dictionary<DominioUT, string> titularesDominio)
        {
            MESubReporteHeader3 subReporteHeader3 = new MESubReporteHeader3();
            MESubReporteFooter subReporteFooter = new MESubReporteFooter(usuarioImpresion);
            var unidad = parcela.UnidadesTributarias.Select(a => a.CodigoProvincial).FirstOrDefault();
            string uf = parcela.UnidadesTributarias.Select(a => a.UnidadFuncional).FirstOrDefault();

            SetLogo2(subReporteHeader3, "imgLogo");
            subReporteHeader3.FindControl("txtTitulo", true).Text = "Constancia Cambio de Titularidad";
            subReporteHeader3.FindControl("lblPartida", true).Visible = true;
            subReporteHeader3.FindControl("txtPartida", true).Text = unidad.ToString();
            subReporteHeader3.FindControl("txtPartida", true).Visible = true;
            subReporteHeader3.FindControl("lblUf", true).Visible = true;
            subReporteHeader3.FindControl("txtUf", true).Visible = true;
            subReporteHeader3.FindControl("txtUf", true).Text = (!string.IsNullOrEmpty(uf) ? uf : "N/A");

            //Nomenclatura - Header
            XRLabel txtDepH = (XRLabel)subReporteHeader3.FindControl("txtDep", true);
            XRLabel txtCircH = (XRLabel)subReporteHeader3.FindControl("txtCirc", true);
            XRLabel txtSecH = (XRLabel)subReporteHeader3.FindControl("txtSec", true);
            XRLabel txtChaH = (XRLabel)subReporteHeader3.FindControl("txtCha", true);
            XRLabel txtQuiH = (XRLabel)subReporteHeader3.FindControl("txtQui", true);
            XRLabel txtFraH = (XRLabel)subReporteHeader3.FindControl("txtFra", true);
            XRLabel txtManH = (XRLabel)subReporteHeader3.FindControl("txtMan", true);
            XRLabel txtParH = (XRLabel)subReporteHeader3.FindControl("txtPar", true);

            //Nomenclatura - Reporte
            XRLabel txtDep = (XRLabel)reporte.FindControl("txtDep", true);
            XRLabel txtCirc = (XRLabel)reporte.FindControl("txtCirc", true);
            XRLabel txtSec = (XRLabel)reporte.FindControl("txtSec", true);
            XRLabel txtCha = (XRLabel)reporte.FindControl("txtCha", true);
            XRLabel txtQui = (XRLabel)reporte.FindControl("txtQui", true);
            XRLabel txtFra = (XRLabel)reporte.FindControl("txtFra", true);
            XRLabel txtMan = (XRLabel)reporte.FindControl("txtMan", true);
            XRLabel txtPar = (XRLabel)reporte.FindControl("txtPar", true);
            string nomenclatura = parcela.Nomenclaturas.FirstOrDefault().Nombre.ToString();
            if (!string.IsNullOrEmpty(nomenclatura))
            {
                string departamento = nomenclatura.Substring(0, 2);
                txtDep.Text = departamento;
                txtDepH.Text = departamento;

                string cicunscripcion = nomenclatura.Substring(2, 3);
                txtCirc.Text = cicunscripcion;
                txtCircH.Text = cicunscripcion;

                string seccion = nomenclatura.Substring(5, 2).ToUpper();
                txtSec.Text = seccion;
                txtSecH.Text = seccion;

                string chacra = nomenclatura.Substring(7, 4);
                txtCha.Text = chacra;
                txtChaH.Text = chacra;

                string quinta = nomenclatura.Substring(11, 4);
                txtQui.Text = quinta;
                txtQuiH.Text = quinta;

                string fraccion = nomenclatura.Substring(15, 4);
                txtFra.Text = fraccion;
                txtFraH.Text = fraccion;

                string manzana = nomenclatura.Substring(19, 4);
                txtMan.Text = manzana;
                txtManH.Text = manzana;

                string parcelaNom = nomenclatura.Substring(23, 5).ToUpper();
                txtPar.Text = parcelaNom;
                txtParH.Text = parcelaNom;
            }

            XRLabel txtFecha = (XRLabel)reporte.FindControl("txtFecha", true);
            DateTime fechaActual = DateTime.Now;
            txtFecha.Text = $"Resistencia, Chaco {fechaActual.Day} de {fechaActual.ToString("MMMM", new System.Globalization.CultureInfo("es-ES"))} de {fechaActual.Year}";

            XRLabel txtUF = (XRLabel)reporte.FindControl("txtUF", true);
            txtUF.Text = string.IsNullOrEmpty(uf) ? "-" : uf;

            XRTable tblDominios = (XRTable)reporte.FindControl("tblDominios", true);
            CargarTablaDominios(tblDominios, titularesDominio);

            (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
            (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader3;

            return GenerarReporte(reporte, parcela, usuarioImpresion);
        }

        public static byte[] GenerarReporteMedidasLinderos(XtraReport reporte, Parcela parcela, string usuario, string departamento)
        {
            MESubReporteHeader3 subReporteHeader3 = new MESubReporteHeader3();
            MESubReporteFooter subReporteFooter = new MESubReporteFooter(usuario);
            var partida = parcela.UnidadesTributarias.Select(a => a.CodigoProvincial).FirstOrDefault();
            string unidadFuncional = parcela.UnidadesTributarias.Select(a => a.UnidadFuncional).FirstOrDefault();

            SetLogo2(subReporteHeader3, "imgLogo");
            subReporteHeader3.FindControl("txtTitulo", true).Text = "Descripción de inmuebles - Medidas y Linderos";
            subReporteHeader3.FindControl("lblPartida", true).Visible = true;
            subReporteHeader3.FindControl("txtPartida", true).Text = partida.ToString();
            subReporteHeader3.FindControl("txtPartida", true).Visible = true;

            //Departamento
            XRLabel txtDep = (XRLabel)reporte.FindControl("txtDep", true);
            txtDep.Text = departamento;

            //Nomenclatura - Header
            XRLabel txtDepH = (XRLabel)subReporteHeader3.FindControl("txtDep", true);
            XRLabel txtCircH = (XRLabel)subReporteHeader3.FindControl("txtCirc", true);
            XRLabel txtSecH = (XRLabel)subReporteHeader3.FindControl("txtSec", true);
            XRLabel txtChaH = (XRLabel)subReporteHeader3.FindControl("txtCha", true);
            XRLabel txtQuiH = (XRLabel)subReporteHeader3.FindControl("txtQui", true);
            XRLabel txtFraH = (XRLabel)subReporteHeader3.FindControl("txtFra", true);
            XRLabel txtManH = (XRLabel)subReporteHeader3.FindControl("txtMan", true);
            XRLabel txtParH = (XRLabel)subReporteHeader3.FindControl("txtPar", true);

            //Nomenclatura - Reporte
            XRLabel txtCirc = (XRLabel)reporte.FindControl("txtCirc", true);
            XRLabel txtSec = (XRLabel)reporte.FindControl("txtSec", true);
            XRLabel txtCha = (XRLabel)reporte.FindControl("txtCha", true);
            XRLabel txtQui = (XRLabel)reporte.FindControl("txtQui", true);
            XRLabel txtFra = (XRLabel)reporte.FindControl("txtFra", true);
            XRLabel txtMan = (XRLabel)reporte.FindControl("txtMan", true);
            XRLabel txtPar = (XRLabel)reporte.FindControl("txtPar", true);
            string nomenclatura = parcela.Nomenclaturas.FirstOrDefault().Nombre.ToString();
            if (!string.IsNullOrEmpty(nomenclatura))
            {
                string departamentoH = nomenclatura.Substring(0, 2);
                txtDepH.Text = departamentoH;

                string cicunscripcion = nomenclatura.Substring(2, 3);
                txtCirc.Text = cicunscripcion;
                txtCircH.Text = cicunscripcion;

                string seccion = nomenclatura.Substring(5, 2).ToUpper();
                txtSec.Text = seccion;
                txtSecH.Text = seccion;

                string chacra = nomenclatura.Substring(7, 4);
                txtCha.Text = chacra;
                txtChaH.Text = chacra;

                string quinta = nomenclatura.Substring(11, 4);
                txtQui.Text = quinta;
                txtQuiH.Text = quinta;

                string fraccion = nomenclatura.Substring(15, 4);
                txtFra.Text = fraccion;
                txtFraH.Text = fraccion;

                string manzana = nomenclatura.Substring(19, 4);
                txtMan.Text = manzana;
                txtManH.Text = manzana;

                string parcelaNom = nomenclatura.Substring(23, 5).ToUpper();
                txtPar.Text = parcelaNom;
                txtParH.Text = parcelaNom;
            }

            //Fecha
            XRLabel txtFecha = (XRLabel)reporte.FindControl("txtFecha", true);
            DateTime fechaActual = DateTime.Now;
            txtFecha.Text = $"Resistencia, Chaco {fechaActual.Day} de {fechaActual.ToString("MMMM", new System.Globalization.CultureInfo("es-ES"))} de {fechaActual.Year}";

            //Planos de Mensuras
            XRTable tblMensuras = (XRTable)reporte.FindControl("tblMensuras", true);
            XRTableRow cabecera = tblMensuras.Rows[0];
            foreach (ParcelaMensura pm in parcela.ParcelaMensuras)
            {
                XRTableRow nuevaFila = new XRTableRow();
                nuevaFila.Cells.Add(CrearCelda(pm.Mensura.Descripcion ?? "-", cabecera.Cells[0].WidthF, Color.FromArgb(246, 244, 252), Color.FromArgb(119, 121, 118)));
                string fechaAprobacion = pm.Mensura.FechaAprobacion.HasValue ? pm.Mensura.FechaAprobacion.Value.ToString("dd/MM/yyyy") : "-";
                nuevaFila.Cells.Add(CrearCelda(fechaAprobacion, cabecera.Cells[1].WidthF, Color.FromArgb(246, 244, 252), Color.FromArgb(119, 121, 118)));
                tblMensuras.Rows.Add(nuevaFila);
            }

            XRTableCell cellConfeccionado = (XRTableCell)reporte.FindControl("xrTableCellConfeccionado", true);
            cellConfeccionado.Text = "CONFECCIONADO" + Environment.NewLine + usuario;

            (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
            (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader3;

            return GenerarReporte(reporte, parcela, usuario);
        }

        #region Constancia Nomenclatura Catastral
        public static byte[] GenerarConstanciaNomenclaturaCatastral(XtraReport reporte, Parcela parcela, string usuario, Dictionary<long, ICollection<DominioUT>> dominios)
        {
            MESubReporteHeader3 subReporteHeader3 = new MESubReporteHeader3();
            MESubReporteFooter subReporteFooter = new MESubReporteFooter(usuario);
            var unidad = parcela.UnidadesTributarias.Select(a => a.CodigoProvincial).FirstOrDefault();

            SetLogo2(subReporteHeader3, "imgLogo");
            subReporteHeader3.FindControl("txtTitulo", true).Text = "Constancia de Nomenclatura Catastral";
            subReporteHeader3.FindControl("lblPartida", true).Visible = true;
            subReporteHeader3.FindControl("txtPartida", true).Text = unidad.ToString();
            subReporteHeader3.FindControl("txtPartida", true).Visible = true;

            //Nomenclatura - Header
            XRLabel txtDepH = (XRLabel)subReporteHeader3.FindControl("txtDep", true);
            XRLabel txtCircH = (XRLabel)subReporteHeader3.FindControl("txtCirc", true);
            XRLabel txtSecH = (XRLabel)subReporteHeader3.FindControl("txtSec", true);
            XRLabel txtChaH = (XRLabel)subReporteHeader3.FindControl("txtCha", true);
            XRLabel txtQuiH = (XRLabel)subReporteHeader3.FindControl("txtQui", true);
            XRLabel txtFraH = (XRLabel)subReporteHeader3.FindControl("txtFra", true);
            XRLabel txtManH = (XRLabel)subReporteHeader3.FindControl("txtMan", true);
            XRLabel txtParH = (XRLabel)subReporteHeader3.FindControl("txtPar", true);

            string nomenclatura = parcela.Nomenclaturas.FirstOrDefault().Nombre.ToString();
            if (!string.IsNullOrEmpty(nomenclatura))
            {
                string departamento = nomenclatura.Substring(0, 2);
                txtDepH.Text = departamento;

                string cicunscripcion = nomenclatura.Substring(2, 3);
                txtCircH.Text = cicunscripcion;

                string seccion = nomenclatura.Substring(5, 2).ToUpper();
                txtSecH.Text = seccion;

                string chacra = nomenclatura.Substring(7, 4);
                txtChaH.Text = chacra;

                string quinta = nomenclatura.Substring(11, 4);
                txtQuiH.Text = quinta;

                string fraccion = nomenclatura.Substring(15, 4);
                txtFraH.Text = fraccion;

                string manzana = nomenclatura.Substring(19, 4);
                txtManH.Text = manzana;

                string parcelaNom = nomenclatura.Substring(23, 5).ToUpper();
                txtParH.Text = parcelaNom;
            }
          
            XRTable tblDominios = (XRTable)reporte.FindControl("tblUTDominios", true);
            if (tblDominios.Rows.Count > 0)
            {
                tblDominios.Rows.RemoveAt(0);
            }

            foreach (var ut in parcela.UnidadesTributarias)
            {
                AgregarCabeceraUT(tblDominios);
                AgregarValoresUnidadTributaria(tblDominios, ut);
                AgregarDominios(tblDominios, dominios[ut.UnidadTributariaId]);
                AgregarWhiteRow(tblDominios);
            }

            (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
            (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader3;

            return GenerarReporte(reporte, parcela, usuario);
        }

        private static void AgregarCabeceraUT(XRTable tblDominios)
        {
            XRTableRow headerRow = new XRTableRow();
            headerRow.Cells.AddRange(CrearHeaderCells(new[] { "#ID_UT", "PARTIDA", "TIPO_UT", "UF" }, System.Drawing.Color.FromArgb(67, 75, 155)));
            tblDominios.Rows.Add(headerRow);
        }

        private static void AgregarValoresUnidadTributaria(XRTable tblDominios, UnidadTributaria ut)
        {
            XRTableRow valueRow = new XRTableRow();
            valueRow.Cells.AddRange(new[]
            {
                CreraValueCell(ut.UnidadTributariaId.ToString()),
                CreraValueCell(ut.CodigoProvincial),
                CreraValueCell(ut.TipoUnidadTributaria?.Descripcion),
                CreraValueCell(string.IsNullOrEmpty(ut.UnidadFuncional) ? "-" : ut.UnidadFuncional)
            });
            tblDominios.Rows.Add(valueRow);
        }

        private static void AgregarDominios(XRTable tblDominios, ICollection<DominioUT> dominios)
        {
            XRTableRow dominioHeaderRow = new XRTableRow();
            dominioHeaderRow.Cells.AddRange(CrearHeaderCells(new[] { "FOLIO REAL MATRÍCULA", "PROPIETARIOS" }, System.Drawing.Color.FromArgb(89, 98, 194), tblDominios.WidthF / 2));
            tblDominios.Rows.Add(dominioHeaderRow);

            foreach (var dom in dominios)
            {
                XRTableRow dominioRow = new XRTableRow();
                string tipoInscripcionDescripcion = dom.TipoInscripcion ?? string.Empty;
                string textoFinal = string.IsNullOrEmpty(tipoInscripcionDescripcion)
                    ? dom.Inscripcion
                    : $"{tipoInscripcionDescripcion} ({dom.Inscripcion})";

                dominioRow.Cells.Add(CreraValueCell(textoFinal, tblDominios.WidthF / 2));
                dominioRow.Cells.Add(CreraValueCell(string.IsNullOrEmpty(dom.Titulares?.FirstOrDefault()?.NombreCompleto) ? "-" :
                    string.Join(", ", dom.Titulares.Select(t => t.NombreCompleto)), tblDominios.WidthF / 2));
                tblDominios.Rows.Add(dominioRow);
            }
        }

        private static void AgregarWhiteRow(XRTable tblDominios)
        {
            XRTableRow blankRow = new XRTableRow();
            blankRow.Cells.AddRange(new[]
            {
                CrearWhiteCell(),
                CrearWhiteCell(),
                CrearWhiteCell(),
                CrearWhiteCell()
            });
            tblDominios.Rows.Add(blankRow);
        }

        private static XRTableCell[] CrearHeaderCells(string[] texts, System.Drawing.Color backColor, float? width = null)
        {
            return texts.Select(text => new XRTableCell
            {
                Text = text,
                BackColor = backColor,
                Font = new System.Drawing.Font("Calibri", 12, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic),
                WidthF = width ?? 0
            }).ToArray();
        }

        private static XRTableCell CreraValueCell(string text, float? width = null)
        {
            return new XRTableCell
            {
                Text = text,
                BackColor = System.Drawing.Color.FromArgb(84, 89, 157),
                WidthF = width ?? 0
            };
        }

        private static XRTableCell CrearWhiteCell()
        {
            return new XRTableCell
            {
                Text = ".",
                Borders = DevExpress.XtraPrinting.BorderSide.None,
                BackColor = System.Drawing.Color.White
            };
        }
        #endregion

        public static byte[] GenerarReporte(XtraReport reporte, Parcela parcela, string usuarioImpresion)
        {
            try
            {
                reporte.DataSource = new ArrayList() { { parcela } };
                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"GenerarReporte", ex);
                throw;
            }
        }

        internal static byte[] GenerarReporte(InformeUnidadTributariaBaja reporte, Parcela parcela, VALValuacion valValuacion, Usuarios usuarioBaja, string usuarioImpresion)
        {
            try
            {
                MESubReporteHeader3 subReporteHeader3 = new MESubReporteHeader3();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter(usuarioImpresion);
                var unidad = parcela.UnidadesTributarias.Select(a => a.CodigoProvincial).FirstOrDefault();
                string unidadFuncional = parcela.UnidadesTributarias.Select(a => a.UnidadFuncional).FirstOrDefault();

                SetLogo2(subReporteHeader3, "imgLogo");
                subReporteHeader3.FindControl("txtTitulo", true).Text = "Reporte Unidad Tributaria de Baja";
                subReporteHeader3.FindControl("lblPartida", true).Visible = true;
                subReporteHeader3.FindControl("txtPartida", true).Text = unidad.ToString();
                subReporteHeader3.FindControl("txtPartida", true).Visible = true;
                subReporteHeader3.FindControl("lblUf", true).Visible = true;
                subReporteHeader3.FindControl("txtUf", true).Visible = true;
                subReporteHeader3.FindControl("txtUf", true).Text = (!string.IsNullOrEmpty(unidadFuncional) ? unidadFuncional : "N/A");
                subReporteHeader3.FindControl("lblFechaBaja", true).Text = "Fecha Baja: " + parcela.UnidadesTributarias.Select(a => a.FechaBaja).FirstOrDefault();
                subReporteHeader3.FindControl("lblFechaBaja", true).Visible = true;

                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader3;

                //Nomenclatura
                XRLabel txtDep = (XRLabel)subReporteHeader3.FindControl("txtDep", true);
                XRLabel txtCirc = (XRLabel)subReporteHeader3.FindControl("txtCirc", true);
                XRLabel txtSec = (XRLabel)subReporteHeader3.FindControl("txtSec", true);
                XRLabel txtCha = (XRLabel)subReporteHeader3.FindControl("txtCha", true);
                XRLabel txtQui = (XRLabel)subReporteHeader3.FindControl("txtQui", true);
                XRLabel txtFra = (XRLabel)subReporteHeader3.FindControl("txtFra", true);
                XRLabel txtMan = (XRLabel)subReporteHeader3.FindControl("txtMan", true);
                XRLabel txtPar = (XRLabel)subReporteHeader3.FindControl("txtPar", true);
                string nomenclatura = parcela.Nomenclaturas.FirstOrDefault().Nombre.ToString();
                if (!string.IsNullOrEmpty(nomenclatura))
                {
                    string departamento = nomenclatura.Substring(0, 2);
                    txtDep.Text = departamento;

                    string cicunscripcion = nomenclatura.Substring(2, 3);
                    txtCirc.Text = cicunscripcion;

                    string seccion = nomenclatura.Substring(5, 2).ToUpper();
                    txtSec.Text = seccion;

                    string chacra = nomenclatura.Substring(7, 4);
                    txtCha.Text = chacra;

                    string quinta = nomenclatura.Substring(11, 4);
                    txtQui.Text = quinta;

                    string fraccion = nomenclatura.Substring(15, 4);
                    txtFra.Text = fraccion;

                    string manzana = nomenclatura.Substring(19, 4);
                    txtMan.Text = manzana;

                    string parcelaNom = nomenclatura.Substring(23, 5).ToUpper();
                    txtPar.Text = parcelaNom;
                }

                //Valuacion
                XRLabel lblValorTierra = (XRLabel)reporte.FindControl("lblValorTierra", true);
                XRLabel lblValorTotal = (XRLabel)reporte.FindControl("lblValorTotal", true);
                XRLabel lblFechaVigencia = (XRLabel)reporte.FindControl("lblFechaVigencia", true);

                if ((valValuacion?.IdValuacion ?? 0) > 0)
                {
                    lblValorTierra.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", valValuacion.ValorTierra);
                    lblValorTotal.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", valValuacion.ValorTotal);
                    lblFechaVigencia.Text = valValuacion.FechaDesde.ToShortDateString();
                }
                else
                {
                    lblValorTierra.Text = " - ";
                    lblValorTotal.Text = " - ";
                    lblFechaVigencia.Text = " - ";
                }

                XRLabel lblUnidad = (XRLabel)reporte.FindControl("lblUnidad", true);
                if (parcela.ClaseParcelaID == 6)
                {
                    lblUnidad.Text = "Unidad Parcelaria";
                }

                //UsuarioBaja
                XRLabel lblUsuarioBaja = (XRLabel)reporte.FindControl("lblUsuarioBaja", true);
                if (usuarioBaja.Id_Usuario != 0)
                {
                    lblUsuarioBaja.Text = usuarioBaja.Apellido.ToUpper().Contains("migracion")
                                       ? usuarioBaja.Login
                                       : usuarioBaja.NombreApellidoCompleto;
                }
                else
                {
                    lblUsuarioBaja.Text = "SIN DATOS";
                }

                reporte.DataSource = new ArrayList() { { parcela } };
                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"GenerarReporte", ex);
                throw;
            }
        }

        internal static byte[] GenerarReporte(InformePersona reporte, Persona persona, string usuario)
        {
            try
            {

                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter(usuario);
                var unidad = "";

                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteHeader2.FindControl("txtTitulo", true).Text = "Informe Persona";
                subReporteHeader2.FindControl("lblPartida", true).Visible = false;
                subReporteHeader2.FindControl("txtPartida", true).Text = unidad.ToString();
                subReporteHeader2.FindControl("txtPartida", true).Visible = false;
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                reporte.DataSource = new Persona[] { persona };

                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"GenerarInforme(persona:{persona.PersonaId})", ex);
                throw;
            }
        }

        internal static byte[] GenerarReporte(InformeParcelarioVIR reporte, Parcela parcela, VALValuacion valValuacion, VIRValuacion valValuacionVir, ParcelaSuperficies parcelaSuperficies, Zonificacion zoni, string usuarioImpresion)
        {
            try
            {
                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter();
                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteFooter.FindControl("txtUsuario", true).Text = usuarioImpresion;
                subReporteFooter.FindControl("txtFechaEmision", true).Text = DateTime.Now.ToShortDateString();
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                XRLabel lblCantUt = (XRLabel)reporte.FindControl("lblCantUt", true);
                var CantUt = parcela.UnidadesTributarias.Count().ToString();

                //Afecta PH
                XRLabel lblAfectaPh = (XRLabel)reporte.FindControl("lblAfectaPh", true);
                if (parcela.UnidadesTributarias.Where(x => x.TipoUnidadTributariaID == 3).Count() >= 1)
                {
                    lblAfectaPh.Text = "SI";
                }
                else
                {
                    lblAfectaPh.Text = "NO";
                }

                //Cantidad Unidades
                if (parcela.UnidadesTributarias.Count() > 1)
                {
                    lblCantUt.Text = $"({CantUt})";
                }
                else
                {
                    lblCantUt.Text = "";
                }

                //Partida
                var unidad = parcela.UnidadesTributarias.Select(a => a.CodigoProvincial).FirstOrDefault();
                XRLabel lblPartida2 = (XRLabel)reporte.FindControl("lblPartida2", true);
                XRLabel lblPartida = (XRLabel)reporte.FindControl("lblPartida", true);
                lblPartida2.Text = unidad.ToString();
                lblPartida.Text = unidad.ToString();

                //Unidades y Superficie Tierra DGC 
                XRLabel lblSupTierraDGC = (XRLabel)reporte.FindControl("lblSupTierraDGC", true);
                var superficieTierra = parcela.Superficie;

                if (parcela.TipoParcelaID == 0 || parcela.TipoParcelaID == 1)
                {
                    lblSupTierraDGC.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", parcela.Superficie) + " m²";
                }
                else
                {
                    lblSupTierraDGC.Text = parcela.Superficie.ToString("0.0000") + " ha";
                }

                //Unidades y Superficie Tierra VIR
                XRLabel lblUnidadesTierraVIR = (XRLabel)reporte.FindControl("lblUnidadesTierraVIR", true);

                if (valValuacionVir != null)
                {
                    var superficieTierraVIR = valValuacionVir.SuperficieTierra;
                    var unidadSuperficieTierraVIR = valValuacionVir.UnidadMedidaSupTierra;

                    lblUnidadesTierraVIR.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", superficieTierraVIR) + " " + unidadSuperficieTierraVIR;
                }
                //Unidades y Superficie Mejoras VIR
                XRLabel lblUnidadesMejorasVIR = (XRLabel)reporte.FindControl("lblUnidadesMejorasVIR", true);

                if (valValuacionVir != null)
                {
                    var superficieMejorasVIR = valValuacionVir.SuperficieMejora;
                    var unidadSuperficieMejorasVIR = valValuacionVir.UnidadMedidaSupMejora;

                    lblUnidadesMejorasVIR.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", superficieMejorasVIR) + " " + unidadSuperficieMejorasVIR;
                }
                //Tipo - Destino VIR
                XRLabel lblTipoDestinoTierraVIR = (XRLabel)reporte.FindControl("lblTipoDestinoTierraVIR", true);

                if (valValuacionVir != null)
                {
                    lblTipoDestinoTierraVIR.Text = $"Tierra - {valValuacionVir.ValuacionTipo.ToUpper()}";
                }
                //Valuacion VIR
                XRLabel lblTierraVIR = (XRLabel)reporte.FindControl("lblTierraVIR", true);
                XRLabel lblFechaVIR = (XRLabel)reporte.FindControl("lblFechaVIR", true);
                XRLabel lblMejorasVIR = (XRLabel)reporte.FindControl("lblMejorasVIR", true);
                XRLabel lblValorTotalVIR = (XRLabel)reporte.FindControl("lblValorTotalVIR", true);
                XRLabel lblTipoDestinoMejorasVIR = (XRLabel)reporte.FindControl("lblTipoDestinoMejorasVIR", true);

                if (valValuacionVir != null)
                {
                    lblTierraVIR.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", valValuacionVir.ValorTierra);
                    lblMejorasVIR.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", valValuacionVir.ValorMejoras);
                    lblValorTotalVIR.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", valValuacionVir.ValorTotal);
                    lblFechaVIR.Text = valValuacionVir.VigenciaDesde.Value.ToShortDateString();
                    lblTipoDestinoMejorasVIR.Text = valValuacionVir.TipoMejoraUso;
                }
                else
                {
                    lblTierraVIR.Text = " - ";
                    lblMejorasVIR.Text = " - ";
                    lblValorTotalVIR.Text = " - ";
                    lblFechaVIR.Text = " - ";
                    lblTipoDestinoMejorasVIR.Text = " - ";
                }

                //Valuacion DCG
                XRLabel lblValorTierra = (XRLabel)reporte.FindControl("lblValorTierra", true);
                XRLabel lblFecha = (XRLabel)reporte.FindControl("lblFecha", true);
                XRLabel lblValorMejoras = (XRLabel)reporte.FindControl("lblValorMejoras", true);
                XRLabel lblValorTotal = (XRLabel)reporte.FindControl("lblValorTotal", true);

                if (valValuacion.IdValuacion > 0)
                {
                    var ValorTierra = valValuacion.ValorTierra;
                    var ValorMejoras = valValuacion.ValorMejoras;
                    var ValorTotal = valValuacion.ValorTotal;
                    var Fecha = valValuacion.FechaDesde;
                    lblValorTierra.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", ValorTierra);
                    lblValorMejoras.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", ValorMejoras);
                    lblValorTotal.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", ValorTotal);
                    lblFecha.Text = Fecha.ToShortDateString();
                }
                else
                {
                    lblValorTierra.Text = " - ";
                    lblValorMejoras.Text = " - ";
                    lblValorTotal.Text = " - ";
                    lblFecha.Text = " - ";
                }

                //Superficies
                XRLabel lblTierra = (XRLabel)reporte.FindControl("lblTierra", true);
                XRLabel lblMejora = (XRLabel)reporte.FindControl("lblMejora", true);
                if (parcela.TipoParcelaID == 2 || parcela.TipoParcelaID == 3)
                {
                    lblTierra.Text = "Las medidas están expresadas en ha";
                }
                else
                {
                    lblTierra.Text = "Las medidas están expresadas en m²";
                }

                //Relevada
                XRLabel lblSupRelevada = (XRLabel)reporte.FindControl("lblSupRelevada", true);
                var SupRelevada = parcelaSuperficies.RelevamientoParcela.Relevada;
                lblSupRelevada.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupRelevada);

                //Registrada
                XRLabel lblSupRegistrada = (XRLabel)reporte.FindControl("lblSupCatastro", true);
                var SupRegistrada = parcela.Superficie;
                if (parcela.TipoParcelaID == 1)
                {
                    lblSupRegistrada.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupRegistrada);
                }
                else
                {
                    lblSupRegistrada.Text = SupRegistrada.ToString("0.0000");
                }

                //Superficies Construcciones
                //Registrada
                XRLabel lblCubiertaReg = (XRLabel)reporte.FindControl("lblCubiertaReg", true);
                XRLabel lblNegocioReg = (XRLabel)reporte.FindControl("lblNegocioReg", true);
                XRLabel lblSemicubiertaReg = (XRLabel)reporte.FindControl("lblSemicubiertaReg", true);
                XRLabel lblTotalConsReg = (XRLabel)reporte.FindControl("lblTotalConsReg", true);
                var SupCubiertaReg = parcelaSuperficies.DGCMejorasConstrucciones.Cubierta;
                var SupNegocioReg = parcelaSuperficies.DGCMejorasConstrucciones.Negocio;
                var SupSemicubiertaReg = parcelaSuperficies.DGCMejorasConstrucciones.Semicubierta;
                var TotalConsReg = parcelaSuperficies.DGCMejorasConstrucciones.Total;
                lblCubiertaReg.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupCubiertaReg);
                lblNegocioReg.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupNegocioReg);
                lblSemicubiertaReg.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupSemicubiertaReg);
                lblTotalConsReg.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", TotalConsReg);

                //Relevada
                XRLabel lblCubiertaRel = (XRLabel)reporte.FindControl("lblCubiertaRel", true);
                XRLabel lblGalponRel = (XRLabel)reporte.FindControl("lblGalponRel", true);
                XRLabel lblSemicubiertaRel = (XRLabel)reporte.FindControl("lblSemicubiertaRel", true);
                XRLabel lblTotalConsRel = (XRLabel)reporte.FindControl("lblTotalConsRel", true);
                var SupCubiertaRel = parcelaSuperficies.RelevamientoMejorasConstrucciones.Cubierta;
                var SupGalponRel = parcelaSuperficies.RelevamientoMejorasConstrucciones.Galpon;
                var SupSemicubiertaRel = parcelaSuperficies.RelevamientoMejorasConstrucciones.Semicubierta;
                var TotalConsRel = parcelaSuperficies.RelevamientoMejorasConstrucciones.Total;
                lblCubiertaRel.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupCubiertaRel);
                lblGalponRel.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupGalponRel);
                lblSemicubiertaRel.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupSemicubiertaRel);
                lblTotalConsRel.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", TotalConsRel);

                //Superficies Construcciones Otras Mejoras
                //Registrada
                XRLabel lblPileta = (XRLabel)reporte.FindControl("lblPileta", true);
                XRLabel lblPavimento = (XRLabel)reporte.FindControl("lblPavimento", true);
                XRLabel lblTotalOtraMejoraReg = (XRLabel)reporte.FindControl("lblTotalOtraMejoraReg", true);
                var SupPileta = parcelaSuperficies.DGCMejorasOtras.Piscina;
                var SupPavimento = parcelaSuperficies.DGCMejorasOtras.Pavimento;
                var TotalOtrasReg = parcelaSuperficies.DGCMejorasOtras.Total;
                lblPileta.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupPileta);
                lblPavimento.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupPavimento);
                lblTotalOtraMejoraReg.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", TotalOtrasReg);

                //Relevada
                XRLabel lblPiscina = (XRLabel)reporte.FindControl("lblPiscina", true);
                XRLabel lblDeportiva = (XRLabel)reporte.FindControl("lblDeportiva", true);
                XRLabel lblEnconstruccion = (XRLabel)reporte.FindControl("lblEnconstruccion", true);
                XRLabel lblPrecaria = (XRLabel)reporte.FindControl("lblPrecaria", true);
                XRLabel lblTotalOtraMejoraRel = (XRLabel)reporte.FindControl("lblTotalOtraMejoraRel", true);
                var SupPiscina = parcelaSuperficies.RelevamientoMejorasOtras.Piscina;
                var SupDeportiva = parcelaSuperficies.RelevamientoMejorasOtras.Deportiva;
                var SupEnconstruccion = parcelaSuperficies.RelevamientoMejorasOtras.Construccion;
                var SupPrecaria = parcelaSuperficies.RelevamientoMejorasOtras.Precaria;
                var TotalOtrasRel = parcelaSuperficies.RelevamientoMejorasOtras.Total;
                lblPiscina.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupPiscina);
                lblDeportiva.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupDeportiva);
                lblEnconstruccion.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupEnconstruccion);
                lblPrecaria.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupPrecaria);
                lblTotalOtraMejoraRel.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", TotalOtrasRel);

                //Superficie Total Construccion
                XRLabel lblSupTotalConsReg = (XRLabel)reporte.FindControl("lblSupTotalConsReg", true);
                XRLabel lblSupTotalConsRel = (XRLabel)reporte.FindControl("lblSupTotalConsRel", true);
                XRLabel lblSupMejorasDGC = (XRLabel)reporte.FindControl("lblSupMejorasDGC", true);

                lblSupTotalConsReg.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", (TotalConsReg + TotalOtrasReg));
                lblSupMejorasDGC.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", (TotalConsReg + TotalOtrasReg)) + " m²";
                lblSupTotalConsRel.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", (TotalConsRel + TotalOtrasRel));



                //Zonificación Zona
                XRTableCell CellZona = (XRTableCell)reporte.FindControl("CellZona", true);
                if (zoni != null)
                {
                    CellZona.Text = $"{zoni.CodigoZona}-{zoni.NombreZona}";
                }
                else
                {
                    CellZona.Text = $"-";
                }

                reporte.DataSource = new Parcela[] { parcela };

                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"GenerarInforme(parcela:{parcela.ParcelaID})", ex);
                throw;
            }
        }

        internal static byte[] GenerarReporte(InformeParcelarioBaja reporte, Parcela parcela, VALValuacion valValuacion, Usuarios usuarioBaja, string usuarioImpresion)
        {
            try
            {
                MESubReporteHeader3 subReporteHeader3 = new MESubReporteHeader3();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter(usuarioImpresion);
                var unidad = parcela.UnidadesTributarias.Where(x => x.TipoUnidadTributariaID == 1 || x.TipoUnidadTributariaID == 2).Select(a => a.CodigoProvincial).FirstOrDefault();

                SetLogo2(subReporteHeader3, "imgLogo");
                subReporteHeader3.FindControl("txtTitulo", true).Text = "Reporte Parcelario de Baja";
                subReporteHeader3.FindControl("lblPartida", true).Visible = true;
                //subReporteHeader2.FindControl("txtPartida", true).Text = unidad.ToString();
                subReporteHeader3.FindControl("txtPartida", true).Text = (unidad ?? "0").ToString();
                subReporteHeader3.FindControl("txtPartida", true).Visible = true;
                subReporteHeader3.FindControl("lblFechaBaja", true).Text = "Fecha Baja: " + parcela.FechaBaja;
                subReporteHeader3.FindControl("lblFechaBaja", true).Visible = true;

                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader3;

                XRLabel lblCantUt = (XRLabel)reporte.FindControl("lblCantUt", true);
                var CantUt = parcela.UnidadesTributarias.Count().ToString();

                //Afecta PH
                XRLabel lblAfectaPh = (XRLabel)reporte.FindControl("lblAfectaPh", true);
                if (parcela.UnidadesTributarias.Where(x => x.TipoUnidadTributariaID == 3).Count() >= 1)
                {
                    lblAfectaPh.Text = "SI";
                }
                else
                {
                    lblAfectaPh.Text = "NO";
                }

                //Cantidad Unidades
                if (parcela.UnidadesTributarias.Count() > 1)
                {
                    lblCantUt.Text = $"({CantUt})";
                }
                else
                {
                    lblCantUt.Text = "";
                }

                //Valuacion
                XRLabel lblValorTierra = (XRLabel)reporte.FindControl("lblValorTierra", true);
                XRLabel lblFecha = (XRLabel)reporte.FindControl("lblFecha", true);
                XRLabel lblValorTotal = (XRLabel)reporte.FindControl("lblValorTotal", true);

                if ((valValuacion?.IdValuacion ?? 0) > 0)
                {
                    lblValorTierra.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", valValuacion.ValorTierra);
                    lblValorTotal.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", valValuacion.ValorTotal);
                    lblFecha.Text = valValuacion.FechaDesde.ToShortDateString();
                }
                else
                {
                    lblValorTierra.Text = " - ";
                    lblValorTotal.Text = " - ";
                    lblFecha.Text = " - ";
                }

                //Nomenclatura - Header
                XRLabel txtDepH = (XRLabel)subReporteHeader3.FindControl("txtDep", true);
                XRLabel txtCircH = (XRLabel)subReporteHeader3.FindControl("txtCirc", true);
                XRLabel txtSecH = (XRLabel)subReporteHeader3.FindControl("txtSec", true);
                XRLabel txtChaH = (XRLabel)subReporteHeader3.FindControl("txtCha", true);
                XRLabel txtQuiH = (XRLabel)subReporteHeader3.FindControl("txtQui", true);
                XRLabel txtFraH = (XRLabel)subReporteHeader3.FindControl("txtFra", true);
                XRLabel txtManH = (XRLabel)subReporteHeader3.FindControl("txtMan", true);
                XRLabel txtParH = (XRLabel)subReporteHeader3.FindControl("txtPar", true);

                //Nomenclatura - Reporte
                XRLabel txtDep = (XRLabel)reporte.FindControl("txtDep", true);
                XRLabel txtCirc = (XRLabel)reporte.FindControl("txtCirc", true);
                XRLabel txtSec = (XRLabel)reporte.FindControl("txtSec", true);
                XRLabel txtCha = (XRLabel)reporte.FindControl("txtCha", true);
                XRLabel txtQui = (XRLabel)reporte.FindControl("txtQui", true);
                XRLabel txtFra = (XRLabel)reporte.FindControl("txtFra", true);
                XRLabel txtMan = (XRLabel)reporte.FindControl("txtMan", true);
                XRLabel txtPar = (XRLabel)reporte.FindControl("txtPar", true);
                string nomenclatura = parcela.Nomenclaturas.FirstOrDefault().Nombre.ToString();
                if (!string.IsNullOrEmpty(nomenclatura))
                {
                    string departamento = nomenclatura.Substring(0, 2);
                    txtDep.Text = departamento;
                    txtDepH.Text = departamento;

                    string cicunscripcion = nomenclatura.Substring(2, 3);
                    txtCirc.Text = cicunscripcion;
                    txtCircH.Text = cicunscripcion;

                    string seccion = nomenclatura.Substring(5, 2).ToUpper();
                    txtSec.Text = seccion;
                    txtSecH.Text = seccion;

                    string chacra = nomenclatura.Substring(7, 4);
                    txtCha.Text = chacra;
                    txtChaH.Text = chacra;

                    string quinta = nomenclatura.Substring(11, 4);
                    txtQui.Text = quinta;
                    txtQuiH.Text = quinta;

                    string fraccion = nomenclatura.Substring(15, 4);
                    txtFra.Text = fraccion;
                    txtFraH.Text = fraccion;

                    string manzana = nomenclatura.Substring(19, 4);
                    txtMan.Text = manzana;
                    txtManH.Text = manzana;

                    string parcelaNom = nomenclatura.Substring(23, 5).ToUpper();
                    txtPar.Text = parcelaNom;
                    txtParH.Text = parcelaNom;
                }

                //Superficie Rural
                /*
                XRTable xrTable11 = (XRTable)reporte.FindControl("xrTable11", true);

                if (xrTable11 != null)
                {
                    foreach (var kvp in parcelaRuralSuperficies)
                    {
                        var supRural = kvp.Value.ToString("0.0000");

                        XRTableRow dataRow = new XRTableRow();
                        XRTableCell cellKey = new XRTableCell()
                        {
                            Text = kvp.Key,
                            ForeColor = Color.FromArgb(119, 121, 118),
                        };
                        XRTableCell cellValue = new XRTableCell()
                        {
                            Text = supRural,
                            ForeColor = Color.FromArgb(119, 121, 118),
                        };

                        // Establecer el estilo de fuente para desactivar el negrita
                        cellKey.Font = new Font("Calibri", 12F, FontStyle.Regular);
                        cellValue.Font = new Font("Calibri", 12F, FontStyle.Regular);
                        cellValue.TextAlignment = TextAlignment.MiddleRight;

                        dataRow.Cells.Add(cellKey);
                        dataRow.Cells.Add(cellValue);
                        xrTable11.Rows.Add(dataRow);
                    }
                }
                */

                // Registrada
                CargarSuperficie(
                    (XRLabel)reporte.FindControl("lblHas", true),
                    (XRLabel)reporte.FindControl("lblAs", true),
                    (XRLabel)reporte.FindControl("lblMetro", true),
                    (XRLabel)reporte.FindControl("lblDm", true),
                    (XRLabel)reporte.FindControl("lblCm", true),
                    parcela.Superficie,
                    parcela.TipoParcelaID
                );

                // Relevada
                CargarSuperficie(
                    (XRLabel)reporte.FindControl("lblHasGraf", true),
                    (XRLabel)reporte.FindControl("lblAsGraf", true),
                    (XRLabel)reporte.FindControl("lblMetroGraf", true),
                    (XRLabel)reporte.FindControl("lblDmGraf", true),
                    (XRLabel)reporte.FindControl("lblCmGraf", true),
                    parcela.SuperficieGrafica,
                    parcela.TipoParcelaID,
                    true
                );

                /* SUPERFICIES DE MEJORAS
                //Superficies Construcciones
                //Registrada
                XRLabel lblCubiertaReg = (XRLabel)reporte.FindControl("lblCubiertaReg", true);
                XRLabel lblNegocioReg = (XRLabel)reporte.FindControl("lblNegocioReg", true);
                XRLabel lblSemicubiertaReg = (XRLabel)reporte.FindControl("lblSemicubiertaReg", true);
                XRLabel lblTotalConsReg = (XRLabel)reporte.FindControl("lblTotalConsReg", true);
                XRLabel lblPileta = (XRLabel)reporte.FindControl("lblPileta", true);
                XRLabel lblPavimento = (XRLabel)reporte.FindControl("lblPavimento", true);
                XRLabel lblTotalOtraMejoraReg = (XRLabel)reporte.FindControl("lblTotalOtraMejoraReg", true);
                XRLabel lblSupTotalConsReg = (XRLabel)reporte.FindControl("lblSupTotalConsReg", true);
                if (parcelaSuperficies != null && parcelaSuperficies.RelevamientoParcela != null)
                {
                    var SupCubiertaReg = parcelaSuperficies.DGCMejorasConstrucciones.Cubierta;
                    var SupNegocioReg = parcelaSuperficies.DGCMejorasConstrucciones.Negocio;
                    var SupSemicubiertaReg = parcelaSuperficies.DGCMejorasConstrucciones.Semicubierta;
                    var TotalConsReg = parcelaSuperficies.DGCMejorasConstrucciones.Total;
                    var SupPileta = parcelaSuperficies.DGCMejorasOtras.Piscina;
                    var SupPavimento = parcelaSuperficies.DGCMejorasOtras.Pavimento;
                    var TotalOtrasReg = parcelaSuperficies.DGCMejorasOtras.Total;

                    lblPileta.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupPileta);
                    lblPavimento.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupPavimento);
                    lblTotalOtraMejoraReg.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", TotalOtrasReg);
                    lblCubiertaReg.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupCubiertaReg);
                    lblNegocioReg.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupNegocioReg);
                    lblSemicubiertaReg.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupSemicubiertaReg);
                    lblTotalConsReg.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", TotalConsReg);
                    lblSupTotalConsReg.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", (TotalConsReg + TotalOtrasReg));
                }
                else
                {
                    lblCubiertaReg.Text = "0.00";
                    lblNegocioReg.Text = "0.00";
                    lblSemicubiertaReg.Text = "0.00";
                    lblTotalConsReg.Text = "0.00";
                    lblPileta.Text = "0.00";
                    lblPavimento.Text = "0.00";
                    lblTotalOtraMejoraReg.Text = "0.00";
                    lblSupTotalConsReg.Text = "0.00";
                }

                //Relevada
                XRLabel lblCubiertaRel = (XRLabel)reporte.FindControl("lblCubiertaRel", true);
                XRLabel lblGalponRel = (XRLabel)reporte.FindControl("lblGalponRel", true);
                XRLabel lblSemicubiertaRel = (XRLabel)reporte.FindControl("lblSemicubiertaRel", true);
                XRLabel lblTotalConsRel = (XRLabel)reporte.FindControl("lblTotalConsRel", true);
                XRLabel lblPiscina = (XRLabel)reporte.FindControl("lblPiscina", true);
                XRLabel lblDeportiva = (XRLabel)reporte.FindControl("lblDeportiva", true);
                XRLabel lblEnconstruccion = (XRLabel)reporte.FindControl("lblEnconstruccion", true);
                XRLabel lblPrecaria = (XRLabel)reporte.FindControl("lblPrecaria", true);
                XRLabel lblTotalOtraMejoraRel = (XRLabel)reporte.FindControl("lblTotalOtraMejoraRel", true);
                XRLabel lblSupTotalConsRel = (XRLabel)reporte.FindControl("lblSupTotalConsRel", true);

                if (parcelaSuperficies != null && parcelaSuperficies.RelevamientoParcela != null)
                {
                    var SupCubiertaRel = parcelaSuperficies.RelevamientoMejorasConstrucciones.Cubierta;
                    var SupGalponRel = parcelaSuperficies.RelevamientoMejorasConstrucciones.Galpon;
                    var SupSemicubiertaRel = parcelaSuperficies.RelevamientoMejorasConstrucciones.Semicubierta;
                    var TotalConsRel = parcelaSuperficies.RelevamientoMejorasConstrucciones.Total;
                    var SupPiscina = parcelaSuperficies.RelevamientoMejorasOtras.Piscina;
                    var SupDeportiva = parcelaSuperficies.RelevamientoMejorasOtras.Deportiva;
                    var SupEnconstruccion = parcelaSuperficies.RelevamientoMejorasOtras.Construccion;
                    var SupPrecaria = parcelaSuperficies.RelevamientoMejorasOtras.Precaria;
                    var TotalOtrasRel = parcelaSuperficies.RelevamientoMejorasOtras.Total;

                    lblCubiertaRel.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupCubiertaRel);
                    lblGalponRel.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupGalponRel);
                    lblSemicubiertaRel.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupSemicubiertaRel);
                    lblTotalConsRel.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", TotalConsRel);
                    lblPiscina.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupPiscina);
                    lblDeportiva.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupDeportiva);
                    lblEnconstruccion.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupEnconstruccion);
                    lblPrecaria.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupPrecaria);
                    lblTotalOtraMejoraRel.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", TotalOtrasRel);
                    lblSupTotalConsRel.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", (TotalConsRel + TotalOtrasRel));
                }
                else
                {
                    lblCubiertaRel.Text = "0.00";
                    lblGalponRel.Text = "0.00";
                    lblSemicubiertaRel.Text = "0.00";
                    lblTotalConsRel.Text = "0.00";
                    lblPiscina.Text = "0.00";
                    lblDeportiva.Text = "0.00";
                    lblEnconstruccion.Text = "0.00";
                    lblPrecaria.Text = "0.00";
                    lblTotalOtraMejoraRel.Text = "0.00";
                    lblSupTotalConsRel.Text = "0.00";
                }
                */

                XRTableCell tblUnidad = (XRTableCell)reporte.FindControl("tblUnidad", true);
                if (parcela.ClaseParcelaID == 4)
                {
                    tblUnidad.Text = "PH ESPECIAL";
                }

                //UsuarioBaja
                XRLabel lblUsuarioBaja = (XRLabel)reporte.FindControl("lblUsuarioBaja", true);
                if(usuarioBaja.Id_Usuario != 0)
                {
                    lblUsuarioBaja.Text = usuarioBaja.Apellido.ToUpper().Contains("migracion")
                                       ? usuarioBaja.Login
                                       : usuarioBaja.NombreApellidoCompleto;
                }
                else
                {
                    lblUsuarioBaja.Text = "SIN DATOS";
                }
               

                reporte.DataSource = new Parcela[] { parcela };

                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"GenerarInforme(parcela:{parcela.ParcelaID})", ex);
                throw;
            }
        }

        internal static byte[] GenerarReporte(InformeAdjudicacion reporte, MensuraTemporal mens, List<UnidadTributariaTramiteTemporal> ut, string nTramite, string usuarioImpresion)
        {
            try
            {
                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter();
                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteFooter.FindControl("txtUsuario", true).Text = usuarioImpresion;
                subReporteFooter.FindControl("txtFechaEmision", true).Text = DateTime.Now.ToShortDateString();
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                XRLabel lblMensuraRegistrada = (XRLabel)reporte.FindControl("lblMensuraRegistrada", true);
                lblMensuraRegistrada.Text = Convert.ToString(mens.Descripcion);

                reporte.SetNumeroTramiteOrHide(nTramite, "numTramite", "subBandTramite");

                reporte.DataSource = ut;

                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"GenerarInforme", ex);
                throw;
            }
        }

        internal static byte[] GenerarReporte(InformeAdjudicacionPorcen reporte, MensuraTemporal mens, List<UnidadTributariaTramiteTemporal> ut, string nTramite, string usuarioImpresion)
        {
            try
            {
                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter();
                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteFooter.FindControl("txtUsuario", true).Text = usuarioImpresion;
                subReporteFooter.FindControl("txtFechaEmision", true).Text = DateTime.Now.ToShortDateString();
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                XRLabel lblMensuraRegistrada = (XRLabel)reporte.FindControl("lblMensuraRegistrada", true);
                lblMensuraRegistrada.Text = Convert.ToString(mens.Descripcion);

                reporte.SetNumeroTramiteOrHide(nTramite, "numTramite", "subBandTramite");

                reporte.DataSource = ut;

                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"GenerarInforme", ex);
                throw;
            }
        }//Código de DGCyC corrientes

        internal static byte[] GenerarReporte(InformeBienesRegistrados reporte, List<DominioTitular> domTitular, string nTramite, string usuarioImpresion)
        {
            try
            {
                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter(usuarioImpresion);
                var unidad ="";

                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteHeader2.FindControl("txtTitulo", true).Text = "Informe de Bienes Registrados";
                subReporteHeader2.FindControl("lblPartida", true).Visible = false;
                subReporteHeader2.FindControl("txtPartida", true).Text = unidad.ToString();
                subReporteHeader2.FindControl("txtPartida", true).Visible = false;
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                //reporte.SetNumeroTramiteOrHide(nTramite, "numTramite", "subBandTramite");

                reporte.DataSource = domTitular;

                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"GenerarInformeBienesRegistrados", ex);
                throw;
            }
        }

        internal static byte[] GenerarReporte(InformePlanoAprobado reporte, Mensura mensura, string usuario)
        {
            try
            {
                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter(usuario);
                var unidad = "";

                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteHeader2.FindControl("txtTitulo", true).Text = "Certificado de Plano Aprobado" + Environment.NewLine + mensura.TipoMensura.Descripcion + " "+ mensura.Descripcion;
                subReporteHeader2.FindControl("lblPartida", true).Visible = false;
                subReporteHeader2.FindControl("txtPartida", true).Text = unidad.ToString();
                subReporteHeader2.FindControl("txtPartida", true).Visible = false;
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                XRLabel lblDigitalizado = (XRLabel)reporte.FindControl("lblDigitalizado", true);
                XRLabel lblNombreProfesional = (XRLabel)reporte.FindControl("lblNombreProfesional", true);

                if (mensura.MensurasDocumentos.Any())
                {
                   lblDigitalizado.Text = "SI"; 
                }
                else
                {
                    lblDigitalizado.Text = "NO";
                }

                lblNombreProfesional.Text = usuario;

                reporte.DataSource = new Mensura[] { mensura };

                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"GenerarInformePlanoAprobado", ex);
                throw;
            }
        }

        internal static byte[] GenerarReporte(InformeSituacionPartidaInmobiliaria reporte, Parcela parcela, List<ParcelaOperacion> parcelasOrigen, List<ParcelaOperacion> parcelasDestino, string nTramite, string usuarioImpresion)
        {
            try
            {
                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter();
                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteFooter.FindControl("txtUsuario", true).Text = usuarioImpresion;
                subReporteFooter.FindControl("txtFechaEmision", true).Text = DateTime.Now.ToShortDateString();
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                var idorigen = parcelasOrigen.Select(a => a.ParcelaOrigen.ParcelaID);
                var iddestino = parcelasDestino.Select(b => b.ParcelaDestino.ParcelaID);
                var idparcela = parcela.ParcelaID;

                SubReporteParcelasOrigen subReporteParcelasOrigen = new SubReporteParcelasOrigen();
                subReporteParcelasOrigen.DataSource = parcelasOrigen;
                (reporte.FindControl("subReporteParcelasOrigen", true) as XRSubreport).ReportSource = subReporteParcelasOrigen;


                SubReporteParcelasDestino subReporteParcelasDestino = new SubReporteParcelasDestino();
                subReporteParcelasDestino.DataSource = parcelasDestino;
                (reporte.FindControl("subReporteParcelasDestino", true) as XRSubreport).ReportSource = subReporteParcelasDestino;

                reporte.SetNumeroTramiteOrHide(nTramite, "numTramite", "subBandTramite");

                reporte.DataSource = new Parcela[] { parcela };

                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"InformeSituacionPartidaInmobiliaria", ex);
                throw;
            }
        }

        internal static byte[] GenerarReporte(InformePropiedad reporte, UnidadTributaria ut, ParcelaSuperficies parcelaSuperficies, string usuarioImpresion, string numMensura, string vigenciaMensura, string nTramite)
        {
            try
            {
                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter();
                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteFooter.FindControl("txtUsuario", true).Text = usuarioImpresion;
                subReporteFooter.FindControl("txtFechaEmision", true).Text = DateTime.Now.ToShortDateString();
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                //Valor
                if (ut.UTValuaciones != null)
                {
                    XRLabel lblValorTierra = (XRLabel)reporte.FindControl("lblValorTierra", true);
                    XRLabel lblValorMejoras = (XRLabel)reporte.FindControl("lblValorMejoras", true);
                    XRLabel lblValorTotal = (XRLabel)reporte.FindControl("lblValorTotal", true);
                    lblValorTierra.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", ut.UTValuaciones.ValorTierra);
                    lblValorMejoras.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", ut.UTValuaciones.ValorMejoras);
                    lblValorTotal.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", ut.UTValuaciones.ValorTotal);
                }

                reporte.SetNumeroTramiteOrHide(nTramite, "numTramite", "subBandTramite");

                XRLabel lblSupCatastro = (XRLabel)reporte.FindControl("lblSupCatastro", true);
                if (ut.Parcela.TipoParcelaID == 1)
                {
                    //lblSupCatastro.Text = String.Format("{0:N2}", ut.Parcela.Superficie) + " m²";
                    if (ut.TipoUnidadTributariaID == 3)
                    {
                        lblSupCatastro.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", ut.Superficie?.ToString()) + " m²";
                    }
                    else
                    {
                        lblSupCatastro.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", ut.Parcela.Superficie) + " m²";
                    }
                }
                else
                {
                    //lblSupCatastro.Text = String.Format("{0:N2}", ut.Parcela.Superficie) + " ha";
                    if (ut.TipoUnidadTributariaID == 3)
                    {
                        lblSupCatastro.Text = ut.Superficie?.ToString("0.0000") + " ha";
                    }
                    else
                    {
                        lblSupCatastro.Text = ut.Parcela.Superficie.ToString("0.0000") + " ha";
                    }

                }

                var lblCi = reporte.FindControl("lblCi", true);
                if (!ut.Parcela.Dominios.Any(d => ut.Dominios.Any(utd => utd.DominioID == d.DominioID)))
                {
                    lblCi.Text = "Unidad Parcelaria sin inscripción registrada, se informa Dominio y Titular de inmueble Origen";
                }
                else
                {
                    lblCi.Visible = false;
                }

                //Superficies
                /*XRLabel lblSupCatastro = (XRLabel)reporte.FindControl("lblSupCatastro", true);
                XRLabel lblSupTitulo = (XRLabel)reporte.FindControl("lblSupTitulo", true);
                XRLabel lblSupMensura = (XRLabel)reporte.FindControl("lblSupMensura", true);
                var SupCatastro = parcelaSuperficies.AtributosParcela.Catastro;
                var SupTitulo = parcelaSuperficies.AtributosParcela.Titulo;
                var SupMensura = parcelaSuperficies.AtributosParcela.Mensura;
                lblSupCatastro.Text = String.Format(new CultureInfo("es-AR"), "{0:N2}", SupCatastro);
                lblSupTitulo.Text = String.Format(new CultureInfo("es-AR"), "{0:N2}", SupTitulo);
                lblSupMensura.Text = String.Format(new CultureInfo("es-AR"), "{0:N2}", SupMensura);*/

                reporte.DataSource = new UnidadTributaria[] { ut };

                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"GenerarInforme(Propiedad:{ut.UnidadTributariaId})", ex);
                throw;
            }
        }

        internal static byte[] GenerarReporte(CertificadoValuatorio reporte, UnidadTributaria ut, string usuarioImpresion, string nTramite)
        {
            try
            {

                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter();
                var unidad = ut.CodigoProvincial;

                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteHeader2.FindControl("txtTitulo", true).Text = "Certificado de Valuación Fiscal";
                subReporteHeader2.FindControl("lblPartida", true).Visible = true;
                subReporteHeader2.FindControl("txtPartida", true).Text = unidad.ToString();
                subReporteHeader2.FindControl("txtPartida", true).Visible = true;
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                //Metros Hectareas

                XRLabel lblMetrosHectareas = (XRLabel)reporte.FindControl("lblMetrosHectareas", true);
                if (ut.Parcela.TipoParcelaID == 1 || ut.Parcela.TipoParcelaID == 0)
                {
                    lblMetrosHectareas.Text = "mts. cuadrados se halla valuado al día de la fecha, de acuerdo al ";
                }
                else
                {
                    lblMetrosHectareas.Text = "hectáreas se halla valuado al día de la fecha, de acuerdo al ";
                }

                if (ut.UTValuaciones != null)
                {
                    XRTableCell xrValorMejoras = (XRTableCell)reporte.FindControl("xrValorMejoras", true);
                    xrValorMejoras.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", ut.UTValuaciones.ValorMejoras);

                    XRTableCell xrValorTierra = (XRTableCell)reporte.FindControl("xrValorTierra", true);
                    xrValorTierra.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", ut.UTValuaciones.ValorTierra);

                    XRTableCell xrValorTotal = (XRTableCell)reporte.FindControl("xrValorTotal", true);
                    xrValorTotal.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", ut.UTValuaciones.ValorTotal);
                }

                XRLabel lblTitular = (XRLabel)reporte.FindControl("lblTitular", true);
                XRLabel lblTituloDominios = (XRLabel)reporte.FindControl("lblTituloDominios", true);
                var claseParcela = ut.Parcela.ClaseParcelaID;

                /*if (ut.Parcela.Dominios.Any())
                {
                    if (claseParcela == 2)
                    {
                        lblTitular.Text = "Poseedor";
                        lblTituloDominios.Visible = false;
                    }
                    else
                    {
                        lblTitular.Text = "Titular";
                        lblTituloDominios.Text = " Inscripto en el Registro de Propiedad de acuerdo al siguiente detalle:";
                    }
                }*/


                /*if (nTramite != null)
                {
                    XRLabel labelNumTramite = (XRLabel)reporte.FindControl("numTramite", true);
                    labelNumTramite.Text = Convert.ToString(nTramite);
                }
                else
                {
                    SubBand subBandTramite = (SubBand)reporte.FindControl("subBandTramite", true);
                    subBandTramite.Visible = false;
                }*/

                XRLabel labelSuperficie = (XRLabel)reporte.FindControl("xrLabel35", true);

                if (ut.TipoUnidadTributariaID == 3)
                {
                    labelSuperficie.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", ut.Superficie?.ToString());
                }
                else if ((ut.TipoUnidadTributariaID == 2 || ut.TipoUnidadTributariaID == 1) && (ut.Parcela.TipoParcelaID == 2 || ut.Parcela.TipoParcelaID == 3))
                {
                    labelSuperficie.Text = (ut.Parcela.Superficie / 10_000).ToString("0.0000");
                }
                else if ((ut.TipoUnidadTributariaID == 2 || ut.TipoUnidadTributariaID == 1) && ut.Parcela.TipoParcelaID == 1)
                {
                    labelSuperficie.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", ut.Parcela.Superficie);
                }

                /*if (ut.Designacion != null)
                {

                    XRLabel labelDesignacion = (XRLabel)reporte.FindControl("xrLabel3", true);

                    var mensaje = new List<string>();

                    if (!string.IsNullOrEmpty(ut.Designacion.Localidad))
                    {
                        mensaje.Add($"Localidad: {ut.Designacion.Localidad}");
                    }
                    if (!string.IsNullOrEmpty(ut.Designacion.Paraje))
                    {
                        mensaje.Add($"Paraje: {ut.Designacion.Paraje}");
                    }
                    if (!string.IsNullOrEmpty(ut.Designacion.Calle))
                    {
                        mensaje.Add($"Calle: {ut.Designacion.Calle}");
                    }
                    if (!string.IsNullOrEmpty(ut.Designacion.Numero))
                    {
                        mensaje.Add($"Número: {ut.Designacion.Numero}");
                    }
                    if (!string.IsNullOrEmpty(ut.Designacion.Seccion))
                    {
                        mensaje.Add($"Sección: {ut.Designacion.Seccion}");
                    }
                    if (!string.IsNullOrEmpty(ut.Designacion.Chacra))
                    {
                        mensaje.Add($"Chacra: {ut.Designacion.Chacra}");
                    }
                    if (!string.IsNullOrEmpty(ut.Designacion.Quinta))
                    {
                        mensaje.Add($"Quinta: {ut.Designacion.Quinta}");
                    }
                    if (!string.IsNullOrEmpty(ut.Designacion.Fraccion))
                    {
                        mensaje.Add($"Fracción: {ut.Designacion.Fraccion}");
                    }
                    if (!string.IsNullOrEmpty(ut.Designacion.Manzana))
                    {
                        mensaje.Add($"Manzana: {ut.Designacion.Manzana}");
                    }
                    if (!string.IsNullOrEmpty(ut.Designacion.Lote))
                    {
                        mensaje.Add($"Parcela: {ut.Designacion.Lote}");
                    }
                    if (ut.TipoUnidadTributariaID == 3)
                    {
                        if (!string.IsNullOrEmpty(ut.Piso))
                        {
                            mensaje.Add($"Piso: {ut.Piso}");
                        }
                        if (!string.IsNullOrEmpty(ut.Unidad))
                        {
                            mensaje.Add($"Unidad: {ut.Unidad}");
                        }
                    }

                    labelDesignacion.Text = string.Join(new string(' ', 5), mensaje);
                }*/

                if (ut.UTValuaciones != null)
                {
                    string resultado = MonedaHelper.Convertir(Math.Round(ut.UTValuaciones.ValorTotal, 2).ToString(), true, "SON PESOS");

                    XRLabel labelTotal = (XRLabel)reporte.FindControl("xrLabel7", true);
                    labelTotal.Text = resultado.ToString();

                    reporte.DataSource = new UnidadTributaria[] { ut };
                }

                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"GenerarInforme(Valuatorio:{ut.UnidadTributariaId})", ex);
                throw;
            }
        }

        internal static byte[] GenerarReporte(InformeHistoricoCambios reporte, List<Auditoria> auditorias, string rotulo, string identificador, string usuarioImpresion, string nTramite)
        {
            var rotuloIdentificador = $"{identificador}";
            var rotuloTitulo = $"{rotulo}";

            try
            {
                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter();
                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteFooter.FindControl("txtUsuario", true).Text = usuarioImpresion;
                subReporteFooter.FindControl("txtFechaEmision", true).Text = DateTime.Now.ToShortDateString();
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                //Rótulo e Identificador
                XRLabel lblRotulo = (XRLabel)reporte.FindControl("lblRotulo", true);
                XRLabel lblRotuloTitulo = (XRLabel)reporte.FindControl("lblRotuloTitulo", true);
                lblRotulo.Text = rotuloIdentificador;
                lblRotuloTitulo.Text = rotuloTitulo;

                reporte.SetNumeroTramiteOrHide(nTramite, "numTramite", "subBandTramite");

                ((DetailReportBand)reporte.FindControl("DetailReportAuditoria", true)).DataSource = auditorias;

                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"GenerarInforme(HistoricoCambios:{rotuloIdentificador})", ex);
                throw;
            }
        }

        internal static byte[] GenerarReporte(InformeHistoricoValuaciones reporte, UnidadTributaria ut, string usuarioImpresion, string nTramite, string nExpediente)
        {
            try
            {
                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter();
                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteFooter.FindControl("txtUsuario", true).Text = usuarioImpresion;
                subReporteFooter.FindControl("txtFechaEmision", true).Text = DateTime.Now.ToShortDateString();
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                reporte.SetNumeroTramiteOrHide(nTramite, "numTramite", "subBandTramite");

                XRLabel lblValorMejoras = (XRLabel)reporte.FindControl("lblValorMejoras", true);
                lblValorMejoras.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", ut.UTValuaciones.ValorMejoras);

                XRLabel lblValorTierra = (XRLabel)reporte.FindControl("lblValorTierra", true);
                lblValorTierra.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", ut.UTValuaciones.ValorTierra);

                XRLabel lblValorTotal = (XRLabel)reporte.FindControl("lblValorTotal", true);
                lblValorTotal.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", ut.UTValuaciones.ValorTotal);

                XRLabel labelExpediente = (XRLabel)reporte.FindControl("xrLabel6", true);
                labelExpediente.Text = nExpediente;


                reporte.SetDecretos(reporte, "lblUltimosDecretos", ut.UTValuaciones);

                reporte.DataSource = new UnidadTributaria[] { ut };

                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"GenerarInforme(HistoricoValuaciones:{ut.UnidadTributariaId})", ex);
                throw;
            }
        }

        internal static byte[] GenerarReporte(InformeValuacionRural reporte, UnidadTributaria ut, string usuarioImpresion, string nTramite)
        {
            throw new NotImplementedException("Arreglar este quilombo");
            //try
            //{

            //    MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
            //    MESubReporteFooter subReporteFooter = new MESubReporteFooter();
            //    SetLogo2(subReporteHeader2, "imgLogo");
            //    subReporteFooter.FindControl("txtUsuario", true).Text = usuarioImpresion;
            //    subReporteFooter.FindControl("txtFechaEmision", true).Text = DateTime.Now.ToShortDateString();
            //    (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
            //    (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;


            //    //Nro de Tramite
            //    if (nTramite != null)
            //    {
            //        XRLabel labelNumTramite = (XRLabel)reporte.FindControl("numTramite", true);
            //        labelNumTramite.Text = Convert.ToString(nTramite);
            //    }
            //    else
            //    {
            //        SubBand subBandTramite = (SubBand)reporte.FindControl("subBandTramite", true);
            //        subBandTramite.Visible = false;
            //    }

            //    //Decretos
            //    XRLabel lblDecretos = (XRLabel)reporte.FindControl("lblDecretos", false);
            //    lblDecretos.Text = ProcesarDecretos(ut.UTValuaciones.ValuacionDecretos);

            //    XRLabel lblValorTierra = (XRLabel)reporte.FindControl("lblValorTierra", true);
            //    lblValorTierra.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", ut.UTValuaciones.ValorTierra);

            //    //Grilla 
            //    XRLabel lblSinInfo = (XRLabel)reporte.FindControl("xrLabel10", true);
            //    XRTable lblTablaSinInfo = (XRTable)reporte.FindControl("xrTable", true);
            //    //Superficie Total
            //    XRLabel lblSupTotal = (XRLabel)reporte.FindControl("lblSupTotal", true);
            //    XRLabel lblSupTotallabel = (XRLabel)reporte.FindControl("xrLabel6", true);
            //    var sor = ut.DeclaracionJ.Sor;

            //    var caracteristicasPorAptitud = from sup in ut.DeclaracionJ.Sor.Single().Superficies
            //                                    where sup.Superficie > 0
            //                                    orderby sup.Aptitud.Numero
            //                                    join sorcar in ut.DeclaracionJ.Sor.Single().SorCar on sup.Aptitud.IdAptitud equals sorcar.IdSorCar into lj
            //                                    from car in lj.DefaultIfEmpty()
            //                                    group car?.Caracteristica by sup into grp
            //                                    select new { aptitud = grp.Key.Aptitud, superficie = grp.Key.Superficie, caracteristicas = grp.Where(d => d != null).ToList() };

            //    var tiposPresentesEnSor = caracteristicasPorAptitud
            //                                    .SelectMany(elem => elem.caracteristicas.Select(car => car.TipoCaracteristica))
            //                                    .OrderBy(car => car.IdSorTipoCaracteristica)
            //                                    .Distinct();

            //    if (sor.Any() && caracteristicasPorAptitud.Any())
            //    {
            //        var listaCaracteristicas = new List<DDJJSorTipoCaracteristica>()
            //            {
            //                new DDJJSorTipoCaracteristica() { IdSorTipoCaracteristica = 0, Descripcion = "APTITUDES" },
            //                new DDJJSorTipoCaracteristica() { IdSorTipoCaracteristica = 9999, Descripcion = "SUPERFICIE" }
            //            };
            //        listaCaracteristicas.InsertRange(1, tiposPresentesEnSor);
            //        var columasIndices = listaCaracteristicas.Select((car, idx) => new { id = car.IdSorTipoCaracteristica, idx })
            //                                                 .ToDictionary(car => car.id, car => car.idx);
            //        var tabla = new DataTable("caracteristicas");
            //        tabla.Columns.AddRange(listaCaracteristicas.Select(car => new DataColumn(car.Descripcion, typeof(string))).ToArray());

            //        foreach (var grupo in caracteristicasPorAptitud)
            //        {
            //            var valores = new string[tabla.Columns.Count];
            //            valores[0] = grupo.aptitud.Numero + " - " + grupo.aptitud.Descripcion;
            //            foreach (var car in grupo.caracteristicas)
            //            {
            //                valores[columasIndices[car.IdSorTipoCaracteristica]] = car.Puntaje + " - " + car.Descripcion;
            //            }
            //            valores[valores.Length - 1] = string.Format(new CultureInfo("es-AR"), "{0:N4}", grupo.superficie.GetValueOrDefault());
            //            tabla.Rows.Add(valores);
            //        }
            //        reporte.setcaracteristicas(tabla);

            //        lblSinInfo.Visible = false;
            //        lblSupTotal.Text = string.Format(new CultureInfo("es-AR"), "{0:N4}", caracteristicasPorAptitud.Sum(g => g.superficie.GetValueOrDefault()));
            //    }
            //    else
            //    {
            //        lblSinInfo.Visible = true;
            //        lblTablaSinInfo.CanShrink = true;
            //        lblSupTotal.Visible = false;
            //        lblSupTotallabel.Visible = false;
            //    }
            //    //Distancias

            //    var otrasCarSor = ut.DeclaracionJ.Version.OtrasCarsSor.ToList();
            //    var distanciaCamino = ut.DeclaracionJ.Sor.Select(a => a.DistanciaCamino).FirstOrDefault();
            //    var distanciaembarque = ut.DeclaracionJ.Sor.Select(a => a.DistanciaEmbarque).FirstOrDefault();
            //    var distancialocalidad = ut.DeclaracionJ.Sor.Select(a => a.DistanciaLocalidad).FirstOrDefault();

            //    string mensaje1 = "";
            //    string mensaje2 = "";
            //    string mensaje3 = "";

            //    XRLabel labelCaracteristica1 = (XRLabel)reporte.FindControl("lblCar1", true);
            //    XRLabel labelCaracteristica2 = (XRLabel)reporte.FindControl("lblCar2", true);
            //    XRLabel labelCaracteristica3 = (XRLabel)reporte.FindControl("lblCar3", true);

            //    foreach (var vcar in otrasCarSor)
            //    {
            //        if (vcar.IdVersion == ut.DeclaracionJ.IdVersion && (!vcar.FechaBaja.HasValue || vcar.FechaBaja >= ut.DeclaracionJ.Sor.Select(a => a.FechaModif).FirstOrDefault()))
            //        {

            //            if (vcar.IdDDJJSorOtrasCar == 1)
            //            {
            //                if (distanciaembarque != null)
            //                {
            //                    mensaje1 += distanciaembarque.ToString() + "Km";

            //                }
            //                else
            //                {
            //                    mensaje1 += "     -     ";
            //                }
            //            }
            //            labelCaracteristica1.Text = mensaje1;

            //            if (vcar.IdDDJJSorOtrasCar == 2)
            //            {
            //                var idcamino = ut.DeclaracionJ.Sor.Select(a => a.IdCamino).FirstOrDefault();

            //                if (distanciaCamino != null && idcamino != null)

            //                {
            //                    var camino = ut.DeclaracionJ.Sor.Select(a => a.Via.Nombre).FirstOrDefault();
            //                    mensaje2 += distanciaCamino.ToString() + "Km" + "   " + camino.ToString();
            //                }
            //                else if (distanciaCamino != null && idcamino == null)
            //                {
            //                    mensaje2 += distanciaCamino.ToString() + "Km";
            //                }
            //                else if (distanciaCamino == null && idcamino != null)
            //                {
            //                    var camino = ut.DeclaracionJ.Sor.Select(a => a.Via.Nombre).FirstOrDefault();
            //                    mensaje2 += "   -   " + "     " + camino.ToString();
            //                }
            //                else
            //                {
            //                    mensaje2 += "   -    ";
            //                }

            //            }


            //            labelCaracteristica2.Text = mensaje2;

            //            if (vcar.IdDDJJSorOtrasCar == 5)
            //            {
            //                var idLocalidad = ut.DeclaracionJ.Sor.Select(a => a.IdLocalidad).FirstOrDefault();

            //                if (distancialocalidad != null && idLocalidad != null)
            //                {
            //                    var localidad = ut.DeclaracionJ.Sor.Select(a => a.Objeto.Nombre).FirstOrDefault();
            //                    mensaje3 += distancialocalidad.ToString() + "Km" + "    " + localidad.ToString();
            //                }
            //                else if (distancialocalidad != null && idLocalidad == null)
            //                {
            //                    mensaje3 += distancialocalidad.ToString() + "Km";
            //                }
            //                else if (distancialocalidad == null && idLocalidad != null)
            //                {
            //                    var localidad = ut.DeclaracionJ.Sor.Select(a => a.Objeto.Nombre).FirstOrDefault();
            //                    mensaje3 += "   -   " + "     " + localidad.ToString();
            //                }
            //                else
            //                {
            //                    mensaje3 += "   -    ";
            //                }
            //            }
            //            labelCaracteristica3.Text = mensaje3;
            //        }

            //    }

            //    reporte.DataSource = new UnidadTributaria[] { ut };

            //    return ExportToPDF(reporte);
            //}
            //catch (Exception ex)
            //{
            //    WebApiApplication.GetLogger().LogError($"GenerarInforme(ValuacionRural:{ut.UnidadTributariaId})", ex);
            //    throw;
            //}
        }

        internal static byte[] GenerarReporte(InformeDDJJSoR reporte, UnidadTributaria ut, string usuarioImpresion, string nTramite)
        {
            throw new NotImplementedException("Arreglar este quilombo");

            //try
            //{
            //    MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
            //    MESubReporteFooter subReporteFooter = new MESubReporteFooter();
            //    SetLogo2(subReporteHeader2, "imgLogo");
            //    subReporteFooter.FindControl("txtUsuario", true).Text = usuarioImpresion;
            //    subReporteFooter.FindControl("txtFechaEmision", true).Text = DateTime.Now.ToShortDateString();
            //    (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
            //    (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

            //    if (nTramite != null)
            //    {
            //        XRLabel labelNumTramite = (XRLabel)reporte.FindControl("numTramite", true);
            //        labelNumTramite.Text = Convert.ToString(nTramite);
            //    }
            //    else
            //    {
            //        SubBand subBandTramite = (SubBand)reporte.FindControl("subBandTramite", true);
            //        subBandTramite.Visible = false;
            //    }


            //    var Mensura = ut.DeclaracionJ.Sor.SingleOrDefault()?.Mensuras;

            //    if (Mensura != null)
            //    {
            //        XRLabel labelMensura = (XRLabel)reporte.FindControl("xrLabel4", true);
            //        labelMensura.Text = $"{ Mensura.Numero }-{ Mensura.Letra }";

            //    }

            //    if (ut.DeclaracionJ.Designacion.Any())
            //    {

            //        XRLabel labelDesignacion = (XRLabel)reporte.FindControl("xrLabel33", true);

            //        var mensaje = "";

            //        var desig = ut.DeclaracionJ.Designacion.Select(a => a.IdDesignacion).FirstOrDefault();

            //        if (desig != 0)
            //        {
            //            var Localidad = ut.DeclaracionJ.Designacion.Select(a => a.Localidad).FirstOrDefault();
            //            if (Localidad != null)
            //            {

            //                mensaje += "Localidad: " + Localidad + "     ";

            //            }
            //            var Paraje = ut.DeclaracionJ.Designacion.Select(a => a.Paraje).FirstOrDefault();
            //            if (Paraje != null)
            //            {
            //                mensaje += "Paraje: " + Paraje + "     ";

            //            }
            //            var Calle = ut.DeclaracionJ.Designacion.Select(a => a.Calle).FirstOrDefault();
            //            if (Calle != null)
            //            {
            //                mensaje += "Calle: " + Calle + "     ";

            //            }
            //            var Numero = ut.DeclaracionJ.Designacion.Select(a => a.Numero).FirstOrDefault();
            //            if (Numero != null)
            //            {
            //                mensaje += "Número: " + Numero + "     ";

            //            }
            //            var Seccion = ut.DeclaracionJ.Designacion.Select(a => a.Seccion).FirstOrDefault();
            //            if (Seccion != null)
            //            {
            //                mensaje += "Sección: " + Seccion + "     ";

            //            }
            //            var Chacra = ut.DeclaracionJ.Designacion.Select(a => a.Chacra).FirstOrDefault();
            //            if (Chacra != null)
            //            {
            //                mensaje += "Chacra: " + Chacra + "     ";

            //            }
            //            var Quinta = ut.DeclaracionJ.Designacion.Select(a => a.Quinta).FirstOrDefault();
            //            if (Quinta != null)
            //            {
            //                mensaje += "Quinta: " + Quinta + "     ";

            //            }
            //            var Fraccion = ut.DeclaracionJ.Designacion.Select(a => a.Fraccion).FirstOrDefault();
            //            if (Fraccion != null)
            //            {
            //                mensaje += "Fracción: " + Fraccion + "     ";

            //            }
            //            var Manzana = ut.DeclaracionJ.Designacion.Select(a => a.Manzana).FirstOrDefault();
            //            if (Manzana != null)
            //            {

            //                mensaje += "Manzana: " + Manzana + "     ";

            //            }
            //            var Lote = ut.DeclaracionJ.Designacion.Select(a => a.Lote).FirstOrDefault();
            //            if (Lote != null)
            //            {
            //                mensaje += "Parcela: " + Lote + "     ";

            //            }
            //        }

            //        labelDesignacion.Text = mensaje;

            //        //Grilla 
            //        XRLabel lblSinInfo = (XRLabel)reporte.FindControl("xrLabel10", true);
            //        XRTable lblTablaSinInfo = (XRTable)reporte.FindControl("xrTable", true);

            //        //Superficie Total
            //        XRLabel lblSupTotal = (XRLabel)reporte.FindControl("lblSupTotal", true);
            //        XRLabel lblSupTotallabel = (XRLabel)reporte.FindControl("xrLabel9", true);
            //        var sor = ut.DeclaracionJ.Sor;

            //        var caracteristicasPorAptitud = from sup in ut.DeclaracionJ.Sor.Single().Superficies
            //                                        where sup.Superficie > 0
            //                                        orderby sup.Aptitud.Numero
            //                                        join sorcar in ut.DeclaracionJ.Sor.Single().SorCar on sup.Aptitud.IdAptitud equals sorcar.Caracteristica.IdAptitud into lj
            //                                        from car in lj.DefaultIfEmpty()
            //                                        group car?.AptCar.SorCaracteristica by sup into grp
            //                                        select new { aptitud = grp.Key.Aptitud, superficie = grp.Key.Superficie, caracteristicas = grp.Where(d => d != null).ToList() };

            //        var tiposPresentesEnSor = caracteristicasPorAptitud
            //                                        .SelectMany(elem => elem.caracteristicas.Select(car => car.TipoCaracteristica))
            //                                        .OrderBy(car => car.IdSorTipoCaracteristica)
            //                                        .Distinct();

            //        if (sor.Any() && caracteristicasPorAptitud.Any())
            //        {
            //            var listaCaracteristicas = new List<DDJJSorTipoCaracteristica>()
            //            {
            //                new DDJJSorTipoCaracteristica() { IdSorTipoCaracteristica = 0, Descripcion = "APTITUDES" },
            //                new DDJJSorTipoCaracteristica() { IdSorTipoCaracteristica = 9999, Descripcion = "SUPERFICIE" }
            //            };
            //            listaCaracteristicas.InsertRange(1, tiposPresentesEnSor);
            //            var columasIndices = listaCaracteristicas.Select((car, idx) => new { id = car.IdSorTipoCaracteristica, idx })
            //                                                     .ToDictionary(car => car.id, car => car.idx);
            //            var tabla = new DataTable("caracteristicas");
            //            tabla.Columns.AddRange(listaCaracteristicas.Select(car => new DataColumn(car.Descripcion, typeof(string))).ToArray());

            //            foreach (var grupo in caracteristicasPorAptitud)
            //            {
            //                var valores = new string[tabla.Columns.Count];
            //                valores[0] = grupo.aptitud.Numero + " - " + grupo.aptitud.Descripcion;
            //                foreach (var car in grupo.caracteristicas)
            //                {
            //                    var caar = grupo.aptitud.Descripcion;
            //                    if (caar != null)
            //                    {
            //                        valores[columasIndices[car.IdSorTipoCaracteristica]] = car.Numero + " - " + car.Descripcion;
            //                    }
            //                    else
            //                    {
            //                        valores[columasIndices[car.IdSorTipoCaracteristica]] = "-";
            //                    }
            //                }
            //                valores[valores.Length - 1] = string.Format(new CultureInfo("es-AR"), "{0:N4}", grupo.superficie.GetValueOrDefault());
            //                tabla.Rows.Add(valores);
            //            }
            //            reporte.setcaracteristicas(tabla);

            //            lblSinInfo.Visible = false;
            //            lblSupTotal.Text = string.Format(new CultureInfo("es-AR"), "{0:N4}", caracteristicasPorAptitud.Sum(g => g.superficie.GetValueOrDefault()));
            //        }
            //        else
            //        {
            //            lblSinInfo.Visible = true;
            //            lblTablaSinInfo.CanShrink = true;
            //            lblSupTotal.Visible = false;
            //            lblSupTotallabel.Visible = false;
            //        }
            //        //Distancias

            //        var otrasCarSor = ut.DeclaracionJ.Version.OtrasCarsSor.ToList();
            //        var distanciaCamino = ut.DeclaracionJ.Sor.Select(a => a.DistanciaCamino).FirstOrDefault();
            //        var distanciaembarque = ut.DeclaracionJ.Sor.Select(a => a.DistanciaEmbarque).FirstOrDefault();
            //        var distancialocalidad = ut.DeclaracionJ.Sor.Select(a => a.DistanciaLocalidad).FirstOrDefault();

            //        string mensaje1 = "";
            //        string mensaje2 = "";
            //        string mensaje3 = "";

            //        XRLabel labelCaracteristica1 = (XRLabel)reporte.FindControl("lblCar1", true);
            //        XRLabel labelCaracteristica2 = (XRLabel)reporte.FindControl("lblCar2", true);
            //        XRLabel labelCaracteristica3 = (XRLabel)reporte.FindControl("lblCar3", true);

            //        foreach (var vcar in otrasCarSor)
            //        {
            //            if (vcar.IdVersion == ut.DeclaracionJ.IdVersion && (!vcar.FechaBaja.HasValue || vcar.FechaBaja >= ut.DeclaracionJ.Sor.Select(a => a.FechaModif).FirstOrDefault()))
            //            {

            //                if (vcar.IdDDJJSorOtrasCar == 1)
            //                {
            //                    if (distanciaembarque != null)
            //                    {
            //                        mensaje1 += distanciaembarque.ToString() + "Km";
            //                    }
            //                    else
            //                    {
            //                        mensaje1 += "     -     ";
            //                    }
            //                }
            //                labelCaracteristica1.Text = mensaje1;

            //                if (vcar.IdDDJJSorOtrasCar == 2)
            //                {
            //                    var idcamino = ut.DeclaracionJ.Sor.Select(a => a.IdCamino).FirstOrDefault();

            //                    if (distanciaCamino != null && idcamino != null)

            //                    {
            //                        var camino = ut.DeclaracionJ.Sor.Select(a => a.Via.Nombre).FirstOrDefault();
            //                        mensaje2 += distanciaCamino.ToString() + "Km" + "   " + camino.ToString();
            //                    }
            //                    else if (distanciaCamino != null && idcamino == null)
            //                    {
            //                        mensaje2 += distanciaCamino.ToString() + "Km";
            //                    }
            //                    else if (distanciaCamino == null && idcamino != null)
            //                    {
            //                        var camino = ut.DeclaracionJ.Sor.Select(a => a.Via.Nombre).FirstOrDefault();
            //                        mensaje2 += "   -   " + "     " + camino.ToString();
            //                    }
            //                    else
            //                    {
            //                        mensaje2 += "   -    ";
            //                    }

            //                }


            //                labelCaracteristica2.Text = mensaje2;

            //                if (vcar.IdDDJJSorOtrasCar == 5)
            //                {
            //                    var idLocalidad = ut.DeclaracionJ.Sor.Select(a => a.IdLocalidad).FirstOrDefault();

            //                    if (distancialocalidad != null && idLocalidad != null)
            //                    {
            //                        var localidad = ut.DeclaracionJ.Sor.Select(a => a.Objeto.Nombre).FirstOrDefault();
            //                        mensaje3 += distancialocalidad.ToString() + "Km" + "    " + localidad.ToString();
            //                    }
            //                    else if (distancialocalidad != null && idLocalidad == null)
            //                    {
            //                        mensaje3 += distancialocalidad.ToString() + "Km";
            //                    }
            //                    else if (distancialocalidad == null && idLocalidad != null)
            //                    {
            //                        var localidad = ut.DeclaracionJ.Sor.Select(a => a.Objeto.Nombre).FirstOrDefault();
            //                        mensaje3 += "   -   " + "     " + localidad.ToString();
            //                    }
            //                    else
            //                    {
            //                        mensaje3 += "   -    ";
            //                    }
            //                }
            //                labelCaracteristica3.Text = mensaje3;
            //            }
            //        }


            //    }
            //    reporte.DataSource = new UnidadTributaria[] { ut };


            //    return ExportToPDF(reporte);

            //}
            //catch (Exception ex)
            //{
            //    WebApiApplication.GetLogger().LogError($"GenerarInforme(ValuacionUrbana:{ut.UnidadTributariaId})", ex);
            //    throw;
            //}
        }

        internal static byte[] GenerarReporte(InformeHistoricoTitulares reporte, UnidadTributaria ut, string usuarioImpresion, string nTramite)
        {
            try
            {
                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter();
                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteFooter.FindControl("txtUsuario", true).Text = usuarioImpresion;
                subReporteFooter.FindControl("txtFechaEmision", true).Text = DateTime.Now.ToShortDateString();
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                reporte.SetNumeroTramiteOrHide(nTramite, "numTramite", "subBandTramite");

                reporte.DataSource = new UnidadTributaria[] { ut };

                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"GenerarInforme(HistoricoTitlares:{ut.UnidadTributariaId})", ex);
                throw;
            }
        }

        internal static byte[] GenerarReporte(InformeExpedienteObraDetallado reporte, ExpedienteObra expObra, string ph, string permisoProvisorio, string footer, string usuarioImpresion)
        {
            try
            {
                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter();
                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteFooter.FindControl("txtUsuario", true).Text = usuarioImpresion;
                subReporteFooter.FindControl("txtFechaEmision", true).Text = DateTime.Now.ToShortDateString();
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                XRLabel txtPh = (XRLabel)reporte.FindControl("ph", true);
                txtPh.Text = ph;

                XRLabel txtPermiso = (XRLabel)reporte.FindControl("permisoprovisorio", true);
                txtPermiso.Text = permisoProvisorio;

                /*XRLabel txtFooter = (XRLabel)reporte.FindControl("txtFooter", true);
                txtFooter.Text = footer;*/

                reporte.DataSource = new ExpedienteObra[] { expObra };

                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"GenerarInforme(HistoricoTitlares:{expObra.ExpedienteObraId})", ex);
                throw;
            }
        }

        internal static byte[] GenerarReporte(InformeCertificadoCatastral reporte, INMCertificadoCatastral certCat, long certificadosEmitidos, string usuarioImpresion, long? tramite)
        {
            try
            {
                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter();
                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteFooter.FindControl("txtUsuario", true).Text = usuarioImpresion;
                subReporteFooter.FindControl("txtFechaEmision", true).Text = DateTime.Now.ToShortDateString();
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                if (tramite != null)
                {
                    XRLabel labelNumTramite = (XRLabel)reporte.FindControl("numTramite", true);
                    labelNumTramite.Text = Convert.ToString(tramite);
                }
                else
                {
                    SubBand subBandTramite = (SubBand)reporte.FindControl("subBandTramite", true);
                    subBandTramite.Visible = false;
                }

                XRLabel txtPh = (XRLabel)reporte.FindControl("certificadosEmitidos", true);
                txtPh.Text = Convert.ToString(certificadosEmitidos);

                //Valor
                XRLabel lblValorTierra = (XRLabel)reporte.FindControl("lblValorTierra", true);
                XRLabel lblValorMejoras = (XRLabel)reporte.FindControl("lblValorMejoras", true);
                XRLabel lblValorTotal = (XRLabel)reporte.FindControl("lblValorTotal", true);
                lblValorTierra.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", certCat.UnidadTributaria.UTValuaciones.ValorTierra);
                lblValorMejoras.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", certCat.UnidadTributaria.UTValuaciones.ValorMejoras);
                lblValorTotal.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", certCat.UnidadTributaria.UTValuaciones.ValorTotal);

                //superficie
                if (certCat.UnidadTributaria.TipoUnidadTributariaID == 3)
                {
                    XRLabel superficie = (XRLabel)reporte.FindControl("superficie", true);
                    if (certCat.UnidadTributaria.Parcela.TipoParcelaID == 1)
                    {
                        superficie.Text = certCat.UnidadTributaria.Superficie?.ToString("0.00");
                    }
                    else
                    {
                        superficie.Text = certCat.UnidadTributaria.Superficie?.ToString("0.0000");
                    }

                }
                else
                {
                    XRLabel superficie = (XRLabel)reporte.FindControl("superficie", true);
                    if (certCat.UnidadTributaria.Parcela.TipoParcelaID == 1)
                    {
                        superficie.Text = certCat.UnidadTributaria.Parcela.Superficie.ToString("0.00");
                    }
                    else
                    {
                        superficie.Text = certCat.UnidadTributaria.Parcela.Superficie.ToString("0.0000");
                    }
                }

                //ocultar prescripciones
                DetailReportBand detailBandPrescripciones = (DetailReportBand)reporte.FindControl("DetailReportPrescripciones", true);
                detailBandPrescripciones.Visible = false;

                reporte.DataSource = new INMCertificadoCatastral[] { certCat };

                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"GenerarInforme(InformeCertificadoCatastral:{certCat.CertificadoCatastralId})", ex);
                throw;
            }
        }

        public static byte[] GenerarReporte(XtraReport reporte, Tramite tramite, List<TramiteUnidadTributaria> lstUTs, List<TramitePersona> lstPesonas, List<Documento> lstDocumentos, string titulo, string usuarioImpresion)
        {
            try
            {

                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter(usuarioImpresion);
                
                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteHeader2.FindControl("txtTitulo", true).Text = titulo;
                subReporteHeader2.FindControl("lblPartida", true).Visible = false;
                subReporteHeader2.FindControl("txtPartida", true).Text = "";
                subReporteHeader2.FindControl("txtPartida", true).Visible = false;
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                if (lstUTs.Count != 0)
                {
                    if (tramite.Imprime_UTS)
                    {
                        SubReporteUTTramite subReporteUts = new SubReporteUTTramite();
                        CrearReporte(subReporteUts, lstUTs);
                        XRSubreport subReporte = (XRSubreport)reporte.FindControl("reporteUTS", true);
                        subReporte.ReportSource = subReporteUts;
                    }
                }
                else
                {
                    SubReporteUTTramite subReporteUts = new SubReporteUTTramite();
                    CrearReporteUTsEmpty(subReporteUts);
                    XRSubreport subReporte = (XRSubreport)reporte.FindControl("reporteUTS", true);
                    subReporte.ReportSource = subReporteUts;
                }

                if (lstPesonas.Count != 0)
                {
                    if (tramite.Imprime_Per)
                    {
                        SubReportePETramite subReportePersonas = new SubReportePETramite();
                        CrearReporte(subReportePersonas, lstPesonas);
                        XRSubreport subReportePer = (XRSubreport)reporte.FindControl("ReportePersonaS", true);
                        subReportePer.ReportSource = subReportePersonas;
                    }
                }

                if (lstDocumentos.Count != 0)
                {
                    if (tramite.Imprime_Doc)
                    {
                        SubReporteDOTramite subReporteDocumentos = new SubReporteDOTramite();
                        CrearReporte(subReporteDocumentos, lstDocumentos);
                        XRSubreport subReporteDoc = (XRSubreport)reporte.FindControl("Documentos", true);
                        subReporteDoc.ReportSource = subReporteDocumentos;
                    }
                }


                if (tramite.Imprime_Final)
                {
                    SubReporteEFTramite subReporteEFs = new SubReporteEFTramite();
                    CrearReporte(subReporteEFs, tramite.Informe_Final);
                    XRSubreport subReporte = (XRSubreport)reporte.FindControl("reportePlanillaFinal", true);
                    subReporte.ReportSource = subReporteEFs;
                }

                ArrayList datos = new ArrayList();
                datos.Add(tramite);
                reporte.DataSource = datos;

                return ExportToPDF(reporte);

            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", ex.Message + "\nStack Trace\n" + ex.StackTrace, EventLogEntryType.Error);
                throw;
            }
        }

        public static byte[] GenerarReporte(XtraReport reporte, METramite tramite, string persona, string usuarioImpresion)
        {
            try
            {

                string fechaEmision = DateTime.Now.ToShortDateString();
                //SetLogo(reporte, "imgLogo");
                //var ctlfechaEmision = reporte.FindControl("txtFechaEmision", true);
                //ctlfechaEmision.Text = fechaEmision;
                //var ctlUsuario = reporte.FindControl("txtUsuario", true);
                //ctlUsuario.Text = usuarioApeYnom;

                //SetLogo(reporte, "imgLogo2");
                //var ctlfechaEmision2 = reporte.FindControl("txtFechaEmision2", true);
                //ctlfechaEmision2.Text = fechaEmision;
                //var ctlUsuario2 = reporte.FindControl("txtUsuario2", true);
                //ctlUsuario2.Text = usuarioApeYnom;

                // ReporteHelper.SetPiePagina(reporte);
                ArrayList datos = new ArrayList();
                datos.Add(tramite);
                //reporte.DataSource = datos;

                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter();
                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteFooter.FindControl("txtUsuario", true).Text = usuarioImpresion;
                subReporteFooter.FindControl("txtFechaEmision", true).Text = DateTime.Now.ToShortDateString();
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;
                (reporte.FindControl("xrSubreport1", true) as XRSubreport).ReportSource = subReporteHeader2;

                /*MESubReporteHeader subReporteHeader = new MESubReporteHeader();
                SetLogo(subReporteHeader, "imgLogo");
                var ctlfechaEmision = subReporteHeader.FindControl("txtFechaEmision", true);
                ctlfechaEmision.Text = fechaEmision;
                var ctlUsuario = subReporteHeader.FindControl("txtUsuario", true);
                ctlUsuario.Text = usuarioApeYnom;

                XRSubreport subReporteHeader1 = (XRSubreport)reporte.FindControl("subRepHeader1", true);
                subReporteHeader1.ReportSource = subReporteHeader;

                XRSubreport subReporteHeader2 = (XRSubreport)reporte.FindControl("subRepHeader2", true);
                subReporteHeader2.ReportSource = subReporteHeader;*/

                MECaratulaExpedienteSubRep caratulaExpedienteSubRep = new MECaratulaExpedienteSubRep();
                caratulaExpedienteSubRep.DataSource = datos;
                var ctlBarCode = caratulaExpedienteSubRep.FindControl("xrBarCode1", true);
                ctlBarCode.Text = tramite.Numero.Replace("-", string.Empty).Replace("/", string.Empty);

                XRSubreport subReporteDetail1 = (XRSubreport)reporte.FindControl("subRepDetail1", true);
                subReporteDetail1.ReportSource = caratulaExpedienteSubRep;

                XRSubreport subReporteDetail2 = (XRSubreport)reporte.FindControl("subRepDetail2", true);
                subReporteDetail2.ReportSource = caratulaExpedienteSubRep;

                var lblPropietarioPoseedor = caratulaExpedienteSubRep.FindControl("lblPropietarioPoseedor", true);
                if (persona != null)
                {
                    lblPropietarioPoseedor.Text = string.Format(persona.ToString());
                }

                return ExportToPDF(reporte);

            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", ex.Message + "\nStack Trace\n" + ex.StackTrace, EventLogEntryType.Error);
                throw;
            }
        }
        internal static byte[] GenerarCertificadoValuatorio(CertificadoValuatorio reporte, UnidadTributaria ut, string departamento, Dictionary<string, double> parcelaRuralSuperficies, string usuario)
        {
            try
            {
                MESubReporteHeader3 subReporteHeader3 = new MESubReporteHeader3();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter(usuario);
                var unidad = ut.CodigoProvincial;

                SetLogo2(subReporteHeader3, "imgLogo");
                subReporteHeader3.FindControl("txtTitulo", true).Text = "Certificado de Valuación Fiscal";
                subReporteHeader3.FindControl("lblPartida", true).Visible = true;
                subReporteHeader3.FindControl("txtPartida", true).Text = unidad.ToString();
                subReporteHeader3.FindControl("txtPartida", true).Visible = true;
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader3;

                //Nomenclatura
                XRLabel txtDep = (XRLabel)subReporteHeader3.FindControl("txtDep", true);
                XRLabel txtCirc = (XRLabel)subReporteHeader3.FindControl("txtCirc", true);
                XRLabel txtSec = (XRLabel)subReporteHeader3.FindControl("txtSec", true);
                XRLabel txtSecRep = (XRLabel)reporte.FindControl("txtSec", true);
                XRLabel txtCha = (XRLabel)subReporteHeader3.FindControl("txtCha", true);
                XRLabel txtQui = (XRLabel)subReporteHeader3.FindControl("txtQui", true);
                XRLabel txtFra = (XRLabel)subReporteHeader3.FindControl("txtFra", true);
                XRLabel txtMan = (XRLabel)subReporteHeader3.FindControl("txtMan", true);
                XRLabel txtManRep = (XRLabel)reporte.FindControl("txtMan", true);
                XRLabel txtPar = (XRLabel)subReporteHeader3.FindControl("txtPar", true);
                XRLabel txtParRep = (XRLabel)reporte.FindControl("txtPar", true);
                string nomenclatura = ut.Parcela.Nomenclaturas.FirstOrDefault().Nombre.ToString();
                if (!string.IsNullOrEmpty(nomenclatura))
                {
                    string departamentoH = nomenclatura.Substring(0, 2);
                    txtDep.Text = departamentoH;

                    string cicunscripcion = nomenclatura.Substring(2, 3);
                    txtCirc.Text = cicunscripcion;

                    string seccion = nomenclatura.Substring(5, 2).ToUpper();
                    txtSec.Text = seccion;
                    txtSecRep.Text = seccion;

                    string chacra = nomenclatura.Substring(7, 4);
                    txtCha.Text = chacra;

                    string quinta = nomenclatura.Substring(11, 4);
                    txtQui.Text = quinta;

                    string fraccion = nomenclatura.Substring(15, 4);
                    txtFra.Text = fraccion;

                    string manzana = nomenclatura.Substring(19, 4);
                    txtMan.Text = manzana;
                    txtManRep.Text = manzana;

                    string parcelaNom = nomenclatura.Substring(23, 5).ToUpper();
                    txtPar.Text = parcelaNom;
                    txtParRep.Text = parcelaNom;
                }

                //Departamento
                if (departamento != null) 
                {
                    XRLabel lbDepartamento = (XRLabel)reporte.FindControl("lbDepartamento", true);
                    lbDepartamento.Text = departamento; 
                }


                //Metros Hectareas
                XRLabel lblMetrosHectareas = (XRLabel)reporte.FindControl("lblMetrosHectareas", true);
                if (ut.Parcela.TipoParcelaID == 1 || ut.Parcela.TipoParcelaID == 0)
                {
                    lblMetrosHectareas.Text = "mts. cuadrados se halla valuado al día de la fecha, de acuerdo al ";
                }
                else
                {
                    lblMetrosHectareas.Text = "hectáreas se halla valuado al día de la fecha, de acuerdo al ";
                }

                
                //Valuacion
                XRLabel lblValorTierra = (XRLabel)reporte.FindControl("lblValorTierra", true);
                XRLabel lblFecha = (XRLabel)reporte.FindControl("lblFecha", true);
                XRLabel lblValorTotal = (XRLabel)reporte.FindControl("lblValorTotal", true);

                if ((ut.UTValuaciones?.IdValuacion ?? 0) > 0)
                {
                    lblValorTierra.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", ut.UTValuaciones.ValorTierra);
                    lblValorTotal.Text = string.Format(new CultureInfo("es-AR"), "{0:C}", ut.UTValuaciones.ValorTotal);
                    lblFecha.Text = ut.UTValuaciones.FechaDesde.ToShortDateString();
                }
                else
                {
                    lblValorTierra.Text = " - ";
                    lblValorTotal.Text = " - ";
                    lblFecha.Text = " - ";
                }

                XRTable xrTable11 = (XRTable)reporte.FindControl("xrTable11", true);
                if (xrTable11 != null)
                {
                    foreach (var kvp in parcelaRuralSuperficies)
                    {
                        double superficieM2 = (ut.Parcela.TipoParcelaID == 2 || ut.Parcela.TipoParcelaID == 3)
                            ? kvp.Value * 10_000
                            : kvp.Value;
                        xrTable11.Rows.Add(CrearFilaSuperficie(kvp.Key, superficieM2));
                    }
                }

                // Registrada
                CargarSuperficie(
                    (XRLabel)reporte.FindControl("lblHas", true),
                    (XRLabel)reporte.FindControl("lblAs", true),
                    (XRLabel)reporte.FindControl("lblMetro", true),
                    (XRLabel)reporte.FindControl("lblDm", true),
                    (XRLabel)reporte.FindControl("lblCm", true),
                    ut.Parcela.Superficie,
                    ut.Parcela.TipoParcelaID
                );

                // Relevada
                CargarSuperficie(
                    (XRLabel)reporte.FindControl("lblHasGraf", true),
                    (XRLabel)reporte.FindControl("lblAsGraf", true),
                    (XRLabel)reporte.FindControl("lblMetroGraf", true),
                    (XRLabel)reporte.FindControl("lblDmGraf", true),
                    (XRLabel)reporte.FindControl("lblCmGraf", true),
                    ut.Parcela.SuperficieGrafica,
                    ut.Parcela.TipoParcelaID
                );

                //Registrada
                /*
                XRLabel lblSupRegistrada = (XRLabel)reporte.FindControl("lblSupRegistrada", true);
                XRLabel lblTierra = (XRLabel)reporte.FindControl("lblTierra", true);
                var SupRegistrada = ut.Parcela.Superficie;
                if (ut.Parcela.TipoParcelaID == 1 || ut.Parcela.TipoParcelaID == 3)
                {
                    lblSupRegistrada.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", SupRegistrada) + " m2";
                    lblTierra.Text = "*Superficie en m2.";
                }
                else
                {
                    SupRegistrada = SupRegistrada / 10_000;
                    lblSupRegistrada.Text = SupRegistrada.ToString("0.0000") + " ha";
                    lblTierra.Text = "*Superficie en ha.";
                }

                //Relevada
                XRLabel lblAreaGrafico = (XRLabel)reporte.FindControl("lblAreaGrafico", true);
                decimal? AreaGrafico = null;
                string UnidadMedida = (ut.Parcela.TipoParcelaID == 1 || ut.Parcela.TipoParcelaID == 3) ? "m2" : "ha";

                if (ut.Parcela.SuperficieGrafica != null)
                {
                    AreaGrafico = ut.Parcela.SuperficieGrafica;
                }

                lblAreaGrafico.Text = AreaGrafico != null ? $"{AreaGrafico} {UnidadMedida}" : "-";
                */

                XRLabel lblTitular = (XRLabel)reporte.FindControl("lblTitular", true);
                XRLabel lblTituloDominios = (XRLabel)reporte.FindControl("lblTituloDominios", true);
                var claseParcela = ut.Parcela.ClaseParcelaID;

                XRLabel labelSuperficie = (XRLabel)reporte.FindControl("xrLabel35", true);

                if (ut.TipoUnidadTributariaID == 3)
                {
                    labelSuperficie.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", ut.Superficie?.ToString());
                }
                else if ((ut.TipoUnidadTributariaID == 2 || ut.TipoUnidadTributariaID == 1) && (ut.Parcela.TipoParcelaID == 2 || ut.Parcela.TipoParcelaID == 3))
                {
                    labelSuperficie.Text = (ut.Parcela.Superficie / 10_000).ToString("0.0000");
                }
                else if ((ut.TipoUnidadTributariaID == 2 || ut.TipoUnidadTributariaID == 1) && ut.Parcela.TipoParcelaID == 1)
                {
                    labelSuperficie.Text = string.Format(new CultureInfo("es-AR"), "{0:N2}", ut.Parcela.Superficie);
                }

                if (ut.UTValuaciones != null)
                {
                    string resultado = MonedaHelper.Convertir(Math.Round(ut.UTValuaciones.ValorTotal, 2).ToString(), true, "SON PESOS");

                    XRLabel labelTotal = (XRLabel)reporte.FindControl("xrLabel7", true);
                    labelTotal.Text = resultado.ToString();

                    reporte.DataSource = new UnidadTributaria[] { ut };
                }

                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"GenerarInforme(Valuatorio:{ut.UnidadTributariaId})", ex);
                throw;
            }
        }
        public static byte[] GenerarReporteInformeDetallado(XtraReport reporte, METramite tramite, string usuarioImpresion)
        {
            try
            {

                string fechaEmision = DateTime.Now.ToShortDateString();

                ArrayList datos = new ArrayList();
                datos.Add(tramite);
                reporte.DataSource = datos;

                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter();
                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteFooter.FindControl("txtUsuario", true).Text = usuarioImpresion;
                subReporteFooter.FindControl("txtFechaEmision", true).Text = DateTime.Now.ToShortDateString();
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                /*MESubReporteHeader subReporteHeader = new MESubReporteHeader();
                SetLogo(subReporteHeader, "imgLogo");
                var ctlfechaEmision = subReporteHeader.FindControl("txtFechaEmision", true);
                ctlfechaEmision.Text = fechaEmision;
                var ctlUsuario = subReporteHeader.FindControl("txtUsuario", true);
                ctlUsuario.Text = usuarioApeYnom;

                XRSubreport subReporteHeader1 = (XRSubreport)reporte.FindControl("subRepHeader1", true);
                subReporteHeader1.ReportSource = subReporteHeader;*/


                MEMovimientosSubRep movimientosSubRep = new MEMovimientosSubRep();
                movimientosSubRep.DataSource = tramite.Movimientos;

                XRSubreport subReporteMovimientos = (XRSubreport)reporte.FindControl("subRepMovimientos", true);
                subReporteMovimientos.ReportSource = movimientosSubRep;

                MEDocumentosSubRep documentosSubRep = new MEDocumentosSubRep();
                documentosSubRep.DataSource = tramite.TramiteDocumentos;

                XRSubreport subReporteDocumentos = (XRSubreport)reporte.FindControl("subRepDocumentos", true);
                subReporteDocumentos.ReportSource = documentosSubRep;

                MEDesglosesSubRep desglosesSubRep = new MEDesglosesSubRep();
                desglosesSubRep.DataSource = tramite.Desgloses;

                XRSubreport subRepDesgloses = (XRSubreport)reporte.FindControl("subRepDesgloses", true);
                subRepDesgloses.ReportSource = desglosesSubRep;

                // ReporteHelper.SetPiePagina(reporte);

                return ExportToPDF(reporte);

            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", ex.Message + "\nStack Trace\n" + ex.StackTrace, EventLogEntryType.Error);
                throw;
            }
        }

        public static byte[] GenerarReporteObservaciones(XtraReport reporte, METramite tramite, string usuarioImpresion)
        {
            try
            {
                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter();
                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteFooter.FindControl("txtUsuario", true).Text = usuarioImpresion;
                subReporteFooter.FindControl("txtFechaEmision", true).Text = DateTime.Now.ToShortDateString();
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                /*MESubReporteHeader subReporteHeader = new MESubReporteHeader();
                SetLogo(subReporteHeader, "imgLogo");
                subReporteHeader.FindControl("txtFechaEmision", true).Text = DateTime.Now.ToShortDateString();
                subReporteHeader.FindControl("txtUsuario", true).Text = usuarioApeYnom;
                (reporte.FindControl("subRepHeader1", true) as XRSubreport).ReportSource = subReporteHeader;*/

                string fechaEmision = DateTime.Now.ToShortDateString();

                ArrayList datos = new ArrayList();
                datos.Add(tramite);
                reporte.DataSource = datos;

                return ExportToPDF(reporte);

            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", ex.Message + "\nStack Trace\n" + ex.StackTrace, EventLogEntryType.Error);
                throw;
            }
        }

        public static byte[] GenerarReporteRemito(XtraReport reporte, MERemito remito, string usuarioImpresion)
        {
            try
            {
                string fechaEmision = DateTime.Now.ToShortDateString();

                //ArrayList datos = new ArrayList();
                //datos.Add(remito.Movimientos);
                //reporte.DataSource = datos;

                reporte.DataSource = remito.Movimientos;

                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter();
                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteFooter.FindControl("txtUsuario", true).Text = usuarioImpresion;
                subReporteFooter.FindControl("txtFechaEmision", true).Text = DateTime.Now.ToShortDateString();
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                /*MESubReporteHeader subReporteHeader = new MESubReporteHeader();
                SetLogo(subReporteHeader, "imgLogo");
                var ctlfechaEmision = subReporteHeader.FindControl("txtFechaEmision", true);
                ctlfechaEmision.Text = fechaEmision;
                var ctlUsuario = subReporteHeader.FindControl("txtUsuario", true);
                ctlUsuario.Text = usuarioApeYnom;

                XRSubreport subReporteHeader1 = (XRSubreport)reporte.FindControl("subRepHeader1", true);
                subReporteHeader1.ReportSource = subReporteHeader;*/

                var ctlRemitoNro = reporte.FindControl("lblRemitoNro", true);
                ctlRemitoNro.Text = remito.Numero;

                var ctlSectorEmisor = reporte.FindControl("lblSectorEmisor", true);
                ctlSectorEmisor.Text = remito.SectorOrigen.Nombre;

                var ctlSectorDestinatario = reporte.FindControl("lblSectorDestinatario", true);
                ctlSectorDestinatario.Text = remito.SectorDestino.Nombre;

                var ctlFecha = reporte.FindControl("lblFecha", true);
                ctlFecha.Text = remito.FechaAlta.ToString("dd/MM/yyyy HH:mm");

                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"ReporteHelper - GenerarReporteRemito({remito.IdRemito})", ex);
                throw;
            }
        }

        public static byte[] GenerarReporteGeneralTramites(XtraReport reporte, Dictionary<string, string> filtros, List<METramite> tramites, string usuarioImpresion)
        {
            try
            {
                //ArrayList datos = new ArrayList();
                //datos.Add(remito.Movimientos);
                //reporte.DataSource = datos;

                var lstListadoGralTramites = tramites.Select(t => new MEListadoGeneralTramitesModel
                {
                    FechaInicio = t.FechaInicio,
                    Numero = t.Numero,
                    TipoTramite = t.Tipo.Descripcion,
                    ObjetoTramite = t.Objeto.Descripcion,
                    Iniciador = (t.Profesional?.NombreApellidoCompleto ?? string.Empty),
                    Destinatario = t.Movimientos.SingleOrDefault(m => m.FechaAlta == t.Movimientos.Max(f => f.FechaAlta))?.SectorOrigen?.Nombre ?? string.Empty,
                    Estado = t.Estado.Descripcion
                }).ToList();

                reporte.DataSource = lstListadoGralTramites;

                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter();
                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteFooter.FindControl("txtUsuario", true).Text = usuarioImpresion;
                subReporteFooter.FindControl("txtFechaEmision", true).Text = DateTime.Now.ToShortDateString();
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                /*MESubReporteHeader subReporteHeader = new MESubReporteHeader();
                SetLogo(subReporteHeader, "imgLogo");
                var ctlfechaEmision = subReporteHeader.FindControl("txtFechaEmision", true);
                ctlfechaEmision.Text = fechaEmision;
                var ctlUsuario = subReporteHeader.FindControl("txtUsuario", true);
                ctlUsuario.Text = usuarioApeYnom;

                XRSubreport subReporteHeader1 = (XRSubreport)reporte.FindControl("subRepHeader1", true);
                subReporteHeader1.ReportSource = subReporteHeader;*/

                //var ctlTitFecha1 = reporte.FindControl("lblTitFecha1", true);
                //ctlTitFecha1.Text = "Fecha Ingreso (Desde - Hasta):";
                //var ctlFecha1 = reporte.FindControl("lblFecha1", true);
                //ctlFecha1.Text = filtros["fechaIngresoDesde"] + " - " + filtros["fechaIngresoHasta"];

                var ctlFiltros = reporte.FindControl("lblFiltros", true);
                string filtrosTexto = string.Empty;
                foreach (var valor in filtros)
                {
                    if (!string.IsNullOrEmpty(valor.Value) && !valor.Value.Contains("Tod") && !valor.Value.Contains("Seleccione"))
                    {
                        switch (valor.Key)
                        {
                            case "fechaIngresoDesde":
                                {
                                    filtrosTexto += "Fecha Ingreso Desde: " + valor.Value + Environment.NewLine;
                                }
                                break;
                            case "fechaIngresoHasta":
                                {
                                    filtrosTexto += "Fecha Ingreso Hasta: " + valor.Value + Environment.NewLine;
                                }
                                break;
                            case "fechaLibroDesde":
                                {
                                    filtrosTexto += "Fecha Libro Desde: " + valor.Value + Environment.NewLine;
                                }
                                break;
                            case "fechaLibroHasta":
                                {
                                    filtrosTexto += "Fecha Libro Hasta: " + valor.Value + Environment.NewLine;
                                }
                                break;
                            case "fechaVencDesde":
                                {
                                    filtrosTexto += "Fecha Vto. Desde: " + valor.Value + Environment.NewLine;
                                }
                                break;
                            case "fechaVencHasta":
                                {
                                    filtrosTexto += "Fecha Vto. Hasta: " + valor.Value + Environment.NewLine;
                                }
                                break;
                            case "jurisdiccionText":
                                {
                                    filtrosTexto += "Jurisdicción: " + valor.Value + Environment.NewLine;
                                }
                                break;
                            case "localidadText":
                                {
                                    filtrosTexto += "Localidad: " + valor.Value + Environment.NewLine;
                                }
                                break;
                            case "prioridadText":
                                {
                                    filtrosTexto += "Prioridad: " + valor.Value + Environment.NewLine;
                                }
                                break;
                            case "tipoTramiteText":
                                {
                                    filtrosTexto += "Tipo Trámite: " + valor.Value + Environment.NewLine;
                                }
                                break;
                            case "objetoTramiteText":
                                {
                                    filtrosTexto += "Objeto: " + valor.Value + Environment.NewLine;
                                }
                                break;
                            case "estadoText":
                                {
                                    filtrosTexto += "Estado: " + valor.Value + Environment.NewLine;
                                }
                                break;
                            case "iniciadorText":
                                {
                                    filtrosTexto += "Iniciador: " + valor.Value + Environment.NewLine;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
                ctlFiltros.Text = filtrosTexto;

                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"ReporteHelper - GenerarReporteGeneralTramites(filtro, tramites,{usuarioImpresion})", ex);
                throw;
            }
        }

        public static byte[] GenerarInformePendientesConfirmar(XtraReport reporte, List<METramite> tramites, string usuarioImpresion)
        {
            try
            {
                string fechaEmision = DateTime.Now.ToShortDateString();

                var lstListadoGralTramites = tramites.Select(t => new MEListadoGeneralTramitesModel
                {
                    FechaInicio = t.FechaInicio,
                    Numero = t.Numero,
                    TipoTramite = t.Tipo.Descripcion,
                    ObjetoTramite = t.Objeto.Descripcion,
                    Iniciador = (t.Profesional?.NombreApellidoCompleto ?? string.Empty),
                    Destinatario = t.Movimientos.Single(m => m.FechaAlta == t.Movimientos.Max(f => f.FechaAlta)).SectorOrigen.Nombre,
                    Estado = t.Estado.Descripcion
                }).ToList();

                reporte.DataSource = lstListadoGralTramites;

                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter();
                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteFooter.FindControl("txtUsuario", true).Text = usuarioImpresion;
                subReporteFooter.FindControl("txtFechaEmision", true).Text = DateTime.Now.ToShortDateString();
                (reporte.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (reporte.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                /*MESubReporteHeader subReporteHeader = new MESubReporteHeader();
                SetLogo(subReporteHeader, "imgLogo");
                var ctlfechaEmision = subReporteHeader.FindControl("txtFechaEmision", true);
                ctlfechaEmision.Text = fechaEmision;
                var ctlUsuario = subReporteHeader.FindControl("txtUsuario", true);
                ctlUsuario.Text = usuarioApeYnom;

                XRSubreport subReporteHeader1 = (XRSubreport)reporte.FindControl("subRepHeader1", true);
                subReporteHeader1.ReportSource = subReporteHeader;*/

                return ExportToPDF(reporte);
            }
            catch (Exception ex)
            {
                WebApiApplication.GetLogger().LogError($"ReporteHelper - GenerarReporteGeneralTramites(filtro, tramites,{usuarioImpresion})", ex);
                throw;
            }
        }

        public static void CrearReporte(SubReporteUTTramite reporte, List<TramiteUnidadTributaria> unidades)
        {
            try
            {
                /*Armo la tabla*/
                XRTable tablaActual = (XRTable)reporte.FindControl("xrTable1", true);
                CrearTablaTUs(tablaActual, unidades);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", ex.Message + "\nStack Trace\n" + ex.StackTrace, EventLogEntryType.Error);
                throw;
            }
        }

        public static void CrearReporteUTsEmpty(SubReporteUTTramite reporte)
        {
            try
            {
                /*Armo la tabla*/
                XRTable tablaActual = (XRTable)reporte.FindControl("xrTable1", true);
                CrearTablaTUsEmpty(tablaActual);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", ex.Message + "\nStack Trace\n" + ex.StackTrace, EventLogEntryType.Error);
                throw;
            }
        }
        public static void CrearReporte(SubReportePETramite reporte, List<TramitePersona> personas)
        {
            try
            {
                /*Armo la tabla*/
                XRTable tablaActual = (XRTable)reporte.FindControl("xrTable1", true);
                CrearTablaUTPseronas(tablaActual, personas);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", ex.Message + "\nStack Trace\n" + ex.StackTrace, EventLogEntryType.Error);
                throw;
            }
        }

        public static void CrearReporte(SubReporteDOTramite reporte, List<Documento> documentos)
        {
            try
            {
                /*Armo la tabla*/
                XRTable tablaActual = (XRTable)reporte.FindControl("xrTable1", true);
                CrearTablaDocumentos(tablaActual, documentos);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", ex.Message + "\nStack Trace\n" + ex.StackTrace, EventLogEntryType.Error);
                throw;
            }
        }

        public static void CrearReporte(SubReporteEFTramite reporte, string Planilla)
        {
            try
            {
                /*Armo la tabla*/
                XRLabel LabelActual = (XRLabel)reporte.FindControl("xrInformeFinal", true);
                LabelActual.Text = Planilla;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", ex.Message + "\nStack Trace\n" + ex.StackTrace, EventLogEntryType.Error);
                throw;
            }
        }

        private static void CrearTablaTUs(XRTable tabla, List<TramiteUnidadTributaria> unidades)
        {
            tabla.Width = 0;
            XRTableCell celda = null;
            tabla.Rows.Clear();

            //Agrega fila de encabezado
            tabla.SuspendLayout();
            //tabla.Borders = DevExpress.XtraPrinting.BorderSide.Top;
            XRTableRow filaEnc = new XRTableRow();

            celda = new XRTableCell();
            celda.Text = "Partida";
            celda.Font = new Font("Calibri", 12, FontStyle.Bold);
            celda.ForeColor = Color.FromArgb(8, 169, 217);
            celda.Visible = true;
            celda.TextAlignment = TextAlignment.MiddleLeft;
            celda.Width = 50;
            filaEnc.Cells.Add(celda);

            celda = new XRTableCell();
            celda.Text = "Nomenclatura";
            celda.Visible = true;
            celda.Width = 110;
            celda.Font = new Font("Calibri", 12, FontStyle.Bold);
            celda.ForeColor = Color.FromArgb(8, 169, 217);
            celda.TextAlignment = TextAlignment.MiddleLeft;
            filaEnc.Cells.Add(celda);


            tabla.Width += 140 + 90 + 15; //(celda.Width * 2);
            tabla.Rows.Add(filaEnc);

            tabla.PerformLayout();


            foreach (var item in unidades)
            {
                tabla.SuspendLayout();
                XRTableRow fila = new XRTableRow();

                celda = new XRTableCell();
                celda.Text = item.UnidadTributaria.CodigoProvincial;
                celda.Font = new Font("Calibri", 12, FontStyle.Bold);
                celda.ForeColor = Color.FromArgb(119, 121, 118);
                celda.Visible = true;
                celda.TextAlignment = TextAlignment.MiddleLeft;
                celda.Width = 50;
                fila.Cells.Add(celda);
                //tabla.Rows.FirstRow.Cells.Add(celda);

                celda = new XRTableCell();
                if (item.UnidadTributaria.Parcela.Nomenclaturas.Count() != 0)
                {
                    celda.Text = item.UnidadTributaria.Parcela.Nomenclaturas.First().Nombre;
                }
                else
                {
                    celda.Text = "-";
                }

                celda.Visible = true;
                celda.Width = 110;
                celda.Font = new Font("Calibri", 12, FontStyle.Bold);
                celda.ForeColor = Color.FromArgb(119, 121, 118);
                celda.TextAlignment = TextAlignment.MiddleLeft;
                //celda.Font = new System.Drawing.Font("verdana", 10, System.Drawing.FontStyle.Bold);
                fila.Cells.Add(celda);
                //tabla.Rows.FirstRow.Cells.Add(celda);

                //tabla.Width = tabla.Width + (90 + 70); //(celda.Width * 2);
                tabla.Rows.Add(fila);

                tabla.PerformLayout();

            }
            tabla.Width += 60 + 110; //(celda.Width * 2);
        }

        private static void CrearTablaUTPseronas(XRTable tabla, List<TramitePersona> personas)
        {
            tabla.Width = 0;
            XRTableCell celda = null;
            tabla.Rows.Clear();

            //Agrega fila de encabezado
            tabla.SuspendLayout();
            //tabla.Borders = DevExpress.XtraPrinting.BorderSide.Top;
            XRTableRow filaEnc = new XRTableRow();

            celda = new XRTableCell();
            celda.Text = "Documento Nro.";
            celda.Font = new Font("Calibri", 12, FontStyle.Bold);
            celda.ForeColor = Color.FromArgb(8, 169, 217);
            celda.Visible = true;
            celda.TextAlignment = TextAlignment.MiddleLeft;
            celda.Width = 50;
            filaEnc.Cells.Add(celda);

            celda = new XRTableCell();
            celda.Text = "Nombre";
            celda.Visible = true;
            celda.Width = 110;
            celda.Font = new Font("Calibri", 12, FontStyle.Bold);
            celda.ForeColor = Color.FromArgb(8, 169, 217);
            celda.TextAlignment = TextAlignment.MiddleLeft;
            filaEnc.Cells.Add(celda);

            tabla.Width = tabla.Width + (140 + 90 + 15); //(celda.Width * 2);
            tabla.Rows.Add(filaEnc);

            tabla.PerformLayout();

            foreach (var item in personas)
            {
                tabla.SuspendLayout();
                XRTableRow fila = new XRTableRow();

                celda = new XRTableCell();
                celda.Text = item.Persona.NroDocumento;
                celda.Font = new Font("Calibri", 12, FontStyle.Bold);
                celda.ForeColor = Color.FromArgb(119, 121, 118);
                celda.Visible = true;
                celda.TextAlignment = TextAlignment.MiddleLeft;
                celda.Width = 50;
                fila.Cells.Add(celda);

                celda = new XRTableCell();
                celda.Text = item.Persona.NombreCompleto;
                celda.Visible = true;
                celda.Width = 110;
                celda.Font = new Font("Calibri", 12, FontStyle.Bold);
                celda.ForeColor = Color.FromArgb(119, 121, 118);
                celda.TextAlignment = TextAlignment.MiddleLeft;
                fila.Cells.Add(celda);

                //tabla.Width = tabla.Width + (60 + 110); //(celda.Width * 2);
                tabla.Rows.Add(fila);

                tabla.PerformLayout();

            }
            tabla.Width = tabla.Width + (60 + 110); //(celda.Width * 2);
        }

        private static void CrearTablaDocumentos(XRTable tabla, List<Documento> documentos)
        {
            tabla.Width = 0;
            XRTableCell celda = null;
            tabla.Rows.Clear();

            //Agrega fila de encabezado
            tabla.SuspendLayout();
            //tabla.Borders = DevExpress.XtraPrinting.BorderSide.Top;
            XRTableRow filaEnc = new XRTableRow();

            celda = new XRTableCell();
            celda.Text = "Nombre";
            celda.Font = new Font("Calibri", 12, FontStyle.Bold);
            celda.ForeColor = Color.FromArgb(8, 169, 217);
            celda.Visible = true;
            celda.TextAlignment = TextAlignment.MiddleLeft;
            celda.Width = 100;
            filaEnc.Cells.Add(celda);

            celda = new XRTableCell();
            celda.Text = "Archivo";
            celda.Visible = true;
            celda.Width = 110;
            celda.Font = new Font("Calibri", 12, FontStyle.Bold);
            celda.ForeColor = Color.FromArgb(8, 169, 217);
            celda.TextAlignment = TextAlignment.MiddleLeft;
            filaEnc.Cells.Add(celda);

            tabla.Width = tabla.Width + (140 + 90 + 15); //(celda.Width * 2);
            tabla.Rows.Add(filaEnc);

            tabla.PerformLayout();

            foreach (var item in documentos)
            {
                tabla.SuspendLayout();
                XRTableRow fila = new XRTableRow();

                celda = new XRTableCell();
                celda.Text = item.descripcion;
                celda.Font = new Font("Calibri", 12, FontStyle.Bold);
                celda.ForeColor = Color.FromArgb(119, 121, 118);
                celda.Visible = true;
                celda.TextAlignment = TextAlignment.MiddleLeft;
                celda.Width = 100;
                fila.Cells.Add(celda);

                celda = new XRTableCell();
                celda.Text = item.nombre_archivo;
                celda.Visible = true;
                celda.Width = 300;
                celda.Font = new Font("Calibri", 12, FontStyle.Bold);
                celda.ForeColor = Color.FromArgb(119, 121, 118);
                celda.TextAlignment = TextAlignment.MiddleLeft;
                fila.Cells.Add(celda);

                //tabla.Width = tabla.Width + (60 + 110); //(celda.Width * 2);
                tabla.Rows.Add(fila);

                tabla.PerformLayout();

            }
            tabla.Width = tabla.Width + (60 + 110); //(celda.Width * 2);
        }

        private static void SetLogo(XtraReport reporte)
        {
            try
            {
                string imgURL = string.Format("{0}Content\\Imagenes\\{1}", AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["imagenLogo"]);
                XRPictureBox imgLogo = (XRPictureBox)reporte.FindControl("imgLogo", true);
                imgLogo.Image = new Bitmap(imgURL);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", ex.Message + "\nStack Trace\n" + ex.StackTrace, EventLogEntryType.Error);
                throw;
            }
        }

        private static void SetLogo2(XtraReport reporte, string controlName)
        {
            try
            {
                string imgURL = string.Format("{0}Content\\Imagenes\\{1}", AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["imagenLogo2"]);
                XRPictureBox imgLogo = (XRPictureBox)reporte.FindControl(controlName, true);
                imgLogo.Image = new Bitmap(imgURL);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", ex.Message + "\nStack Trace\n" + ex.StackTrace, EventLogEntryType.Error);
                throw;
            }
        }

        private static void SetLogo(XtraReport reporte, string controlName)
        {
            try
            {
                string imgURL = string.Format("{0}Content\\Imagenes\\{1}", AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppSettings["imagenLogo"]);
                XRPictureBox imgLogo = (XRPictureBox)reporte.FindControl(controlName, true);
                imgLogo.Image = new Bitmap(imgURL);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", ex.Message + "\nStack Trace\n" + ex.StackTrace, EventLogEntryType.Error);
                throw;
            }
        }

        private static void SetPiePagina(XtraReport reporte)
        {
            try
            {
                XRLabel lblMunicipio = (XRLabel)reporte.FindControl("lblMunicipio", true);
                lblMunicipio.Text = ConfigurationManager.AppSettings["descMunicipio"];
                XRLabel lblDireccion = (XRLabel)reporte.FindControl("lblDireccion", true);
                lblDireccion.Text = ConfigurationManager.AppSettings["descDireccion"];
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", ex.Message + "\nStack Trace\n" + ex.StackTrace, EventLogEntryType.Error);
                throw;
            }
        }

        public static byte[] ExportToPDF(XtraReport report)
        {
            try
            {
                byte[] pdf = null;
                using (var memStr = new MemoryStream())
                {
                    report.ExportToPdf(memStr);
                    pdf = memStr.GetBuffer();
                    memStr.Close();
                }
                return pdf;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", ex.Message + "\nStack Trace\n" + ex.StackTrace, EventLogEntryType.Error);
                throw;
            }
        }

        public static XtraReport SetLogoUsuario(XtraReport report, string usuarioImpresion)
        {
            try
            {
                MESubReporteHeader2 subReporteHeader2 = new MESubReporteHeader2();
                MESubReporteFooter subReporteFooter = new MESubReporteFooter();
                SetLogo2(subReporteHeader2, "imgLogo");
                subReporteFooter.FindControl("txtUsuario", true).Text = usuarioImpresion;
                subReporteFooter.FindControl("txtFechaEmision", true).Text = DateTime.Now.ToShortDateString();
                (report.FindControl("xrSubreport2", true) as XRSubreport).ReportSource = subReporteFooter;
                (report.FindControl("xrSubreport3", true) as XRSubreport).ReportSource = subReporteHeader2;

                return report;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("Application", ex.Message + "\nStack Trace\n" + ex.StackTrace, EventLogEntryType.Error);
                throw;
            }
        }

        private static void CrearTablaTUsEmpty(XRTable tabla)
        {
            tabla.Width = 0;
            XRTableCell celda = null;
            tabla.Rows.Clear();

            //Agrega fila de encabezado
            tabla.SuspendLayout();
            //tabla.Borders = DevExpress.XtraPrinting.BorderSide.Top;
            XRTableRow filaEnc = new XRTableRow();

            celda = new XRTableCell();
            celda.Text = "Sin unidades tributarias asociadas.";
            celda.Font = new Font("verdana", 9);
            celda.ForeColor = Color.Black;
            celda.Visible = true;
            celda.TextAlignment = TextAlignment.MiddleLeft;
            celda.Width = 50;
            filaEnc.Cells.Add(celda);

            tabla.Width += 140 + 90 + 15; //(celda.Width * 2);
            tabla.Rows.Add(filaEnc);

            tabla.PerformLayout();



            tabla.Width += 60 + 110; //(celda.Width * 2);
        }

        internal static string ProcesarDecretos(ICollection<VALValuacionDecreto> valuacionDecretos)
        {
            if (valuacionDecretos?.Any() ?? false)
            {
                return string.Join(", ", valuacionDecretos.Select(d => d.Decreto.NroDecreto));
            }
            else
            {
                return " - ";
            }
        }

        private static void CargarTablaDominios(XRTable tblDominios, Dictionary<DominioUT, string> titularesDominio)
        {
            XRTableRow cabecera = tblDominios.Rows[0];
            Color colorFondo = Color.FromArgb(246, 244, 252);
            Color colorTextoPrincipal = Color.FromArgb(119, 121, 118);
            Color colorTextoPropietarios = Color.FromArgb(77, 85, 178);
            foreach (var entrada in titularesDominio)
            {
                DominioUT dominio = entrada.Key;
                string titulares = entrada.Value;
                XRTableRow nuevaFila = CrearFilaDominio(dominio, titulares, cabecera, colorFondo, colorTextoPrincipal, colorTextoPropietarios);
                tblDominios.Rows.Add(nuevaFila);
            }  
        }

        private static XRTableRow CrearFilaDominio(DominioUT dominio, string titulares, XRTableRow cabecera, Color colorFondo, Color colorTextoPrincipal, Color colorTextoPropietarios)
        {
            XRTableRow nuevaFila = new XRTableRow();

            nuevaFila.Cells.Add(CrearCelda(dominio.TipoInscripcion ?? "-", cabecera.Cells[0].WidthF, colorFondo, colorTextoPrincipal));

            if (dominio.TipoInscripcionID == 3)
            {
                var partes = dominio.Inscripcion.Split('-');
                if (partes.Length == 4)
                {
                    nuevaFila.Cells.Add(CrearCelda("-", cabecera.Cells[1].WidthF, colorFondo, colorTextoPrincipal));
                    nuevaFila.Cells.Add(CrearCelda(partes[0], cabecera.Cells[2].WidthF, colorFondo, colorTextoPrincipal));
                    nuevaFila.Cells.Add(CrearCelda(partes[1], cabecera.Cells[3].WidthF, colorFondo, colorTextoPrincipal));
                    nuevaFila.Cells.Add(CrearCelda(partes[2], cabecera.Cells[4].WidthF, colorFondo, colorTextoPrincipal));
                    nuevaFila.Cells.Add(CrearCelda(partes[3], cabecera.Cells[5].WidthF, colorFondo, colorTextoPrincipal));
                }
                else
                {
                    AgregarCeldasPredeterminadas(nuevaFila, dominio.Inscripcion ?? "-", dominio.Fecha.Year.ToString(), cabecera, colorFondo, colorTextoPrincipal);
                }
            }
            else
            {
                AgregarCeldasPredeterminadas(nuevaFila, dominio.Inscripcion ?? "-", dominio.Fecha.Year.ToString(), cabecera, colorFondo, colorTextoPrincipal);
            }

            nuevaFila.Cells.Add(CrearCelda(titulares, cabecera.Cells[6].WidthF, colorFondo, colorTextoPropietarios));

            return nuevaFila;
        }

        private static XRTableCell CrearCelda(string texto, float width, Color backColor, Color foreColor)
        {
            return new XRTableCell
            {
                Text = texto,
                WidthF = width,
                BackColor = backColor,
                ForeColor = foreColor
            };
        }

        internal static byte[] GenerarInformeTramiteResumen(ResumenTramite resumen)
        {
            var reporte = new MEInformeResumen(resumen);
            SetLogo2(reporte.rptHeader.ReportSource, "imgLogo");
            
            return ExportToPDF(reporte);
        }

        internal static byte[] GenerarInformeHojaDeRuta(METramite meTramite, string usuario)
        {
            var reporte = new MEHojaDeRuta(meTramite, usuario);
            SetLogo2(reporte.rptHeader.ReportSource, "imgLogo");

            return ExportToPDF(reporte);
        }

        internal static byte[] GenerarInformeUsuarios(List<Usuarios> usersToExport, string filtro, string usuario)
        {
            var reporte = new InformeUsuarios(usersToExport, filtro, usuario);
            SetLogo2(reporte.rptHeader.ReportSource, "imgLogo");

            return ExportToPDF(reporte);
        }

        internal static byte[] GenerarInformeDetalleCorrida(List<VALValuacionTempDepto> valValuacionTmpDeptoList, string usuario)
        {
            var reporte = new InformeDetalleCorrida(valValuacionTmpDeptoList, usuario);
            SetLogo2(reporte.rptHeader.ReportSource, "imgLogo");

            return ExportToPDF(reporte);
        }

        private static void AgregarCeldasPredeterminadas(XRTableRow fila, string inscripcion, string anio, XRTableRow cabecera, Color backColor, Color foreColor)
        {
            fila.Cells.Add(CrearCelda(inscripcion, cabecera.Cells[1].WidthF, backColor, foreColor));
            fila.Cells.Add(CrearCelda("-", cabecera.Cells[2].WidthF, backColor, foreColor));
            fila.Cells.Add(CrearCelda("-", cabecera.Cells[3].WidthF, backColor, foreColor));
            fila.Cells.Add(CrearCelda("-", cabecera.Cells[4].WidthF, backColor, foreColor));
            fila.Cells.Add(CrearCelda(anio, cabecera.Cells[5].WidthF, backColor, foreColor));
        }
    }

    static class ReportXTMethods
    {
        public static void SetNumeroTramiteOrHide(this XtraReport reporte, string nTramite, string lblName, string bandName)
        {
            if (string.IsNullOrEmpty(nTramite))
            {
                SubBand subBandTramite = (SubBand)reporte.FindControl(bandName, true);
                subBandTramite.Visible = false;
            }
            else
            {
                XRLabel labelNumTramite = (XRLabel)reporte.FindControl(lblName, true);
                labelNumTramite.Text = Convert.ToString(nTramite);
            }
        }
    }
}