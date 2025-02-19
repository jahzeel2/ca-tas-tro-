function ValuacionesController(container) {
    const idUnidadTributaria = Number($("#IdUnidadTributaria", container).val());
    const modalInfo = "#modal-info-valuaciones";
    $(document).ready(function () {
        $(container).modal("show");
    });
    $(window).resize(ajustarmodal);
    $(".valuaciones-content", container).niceScroll(getNiceScrollConfig());
    $("a[data-toggle='tab']", ".valuaciones-content").on("shown.bs.tab", ajustarScrollBars);

    function ajustarmodal() {
        var altura = $(window).height() - 120;
        $(".valuaciones-body").css({ "height": altura });
        $(".valuaciones-content").css({ "height": altura, "overflow": "hidden" });
        ajustarScrollBars();
    }
    function ajustarScrollBars() {
        $(".valuaciones-content").getNiceScroll().resize();
        $(".valuaciones-content").getNiceScroll().show();
    }

    function mostrarMensaje(mensajes, titulo, tipo) {
        $("#TituloInfoDeclaracionesJuradas", modalInfo).html(titulo);
        $("#DescripcionInfoDeclaracionesJuradas", modalInfo).html(mensajes);
        $("[role='alert']", modalInfo)
            .removeClass("alert-danger alert-success alert-info alert-warning")
            .addClass(tipo);
        $(".modal-footer", modalInfo).hide();
        $("#btnInfoOK", modalInfo).off("click");
        const clickPromise = new Promise(ok => {
            $("#btnInfoOK", modalInfo).on("click", ok);
        });
        return clickPromise;
    }
    function mostrarMensajeError(mensajes, titulo, error) {
        return mostrarMensaje(mensajes, titulo, (error || false ? "alert-danger" : "alert-warning"));
    }

    function initGrid(grid, options) {
        const debounce = (func) => setTimeout(func, 100);
        const gridOptions = {
            ...{
                destroy: true,
                dom: "<'row'<'col-sm-12'f>><'row remove-margin text-right'<'col-xs-11 remove-padding leyenda'><'col-xs-1 remove-padding switcher'<'row toggle-activos'>>>rt<'row'<'col-sm-12'p>>",
                processing: true,
                ordering: true,
                language: { url: `${BASE_URL}Scripts/dataTables.spanish.txt` },
                searchDelay: 500,
                order: [2, 'desc'],
            }, ...options
        };
        return $(`#${grid}`, container)
            .DataTable(gridOptions)
            .on('search.dt', debounce.bind(null, ajustarScrollBars));
    }
    function verFormulario(table) {
        const row = table.row(".selected").data();
        if (!row) return;
        showLoading();
        $.ajax({
            url: `${BASE_URL}Valuaciones/VerFormulario`,
            data: { id: row.IdValuacion },
            dataType: "html",
            type: "POST",
            success: function (data) {
                $("#contenido-formulario").html(data);
            },
            error: function (error) {
                hideLoading();
                mostrarMensajeError(error.responseText, "Recuperar Formulario", true);
            }
        });
    }
    function abrirInformeGenerado() {
        window.open(`${BASE_URL}Valuaciones/AbrirReporte`);
    }
    function imprimirFormulario(table) {
        const row = table.row(".selected").data();
        if (!row) return;
        showLoading();
        $.ajax({
            //url: `${BASE_URL}Valuaciones/GenerarInformeFormulario`,
            url: `${BASE_URL}Valuaciones/CertificadoValuatorio`,
            //data: { idDeclaracionJurada: row.IdDeclaracionJurada, tipoDDJJ: row.TipoDDJJ },
            data: { id: idUnidadTributaria },
            type: 'POST',
            success: abrirInformeGenerado,
            error: function () {
                mostrarMensajeError("No se puede generar el informe para el formulario.", "Imprimir Formulario", true);
            },
            complete: hideLoading
        });
    }
    function enableOrDisableButtonsFormularioVigente(row) {
        const controls = $("#btnVerFormulario, #btnImprimirFormulario", container);
        if (!!row && $(row).hasClass("selected")) {
            controls.removeClass("disabled");
        } else {
            controls.addClass("disabled");
        }
    }
    function enableOrDisableButtonsFormulariosHistoricos(row) {
        const controls = $("#btnVerFormularioHistorico, #btnImprimirFormularioHistorico", container);
        if (!!row && $(row).hasClass("selected")) {
            controls.removeClass("disabled");
        } else {
            controls.addClass("disabled");
        }
    }
    function enableOrDisableButtonsValuacionesHistoricas(row) {
        const controls = $("#btnVerValuacionHistorica", container);
        if (!!row && $(row).hasClass("selected")) {
            controls.removeClass("disabled");
        } else {
            controls.addClass("disabled");
        }
    }
    function dateTimeRenderer(data, type) {
        const value = parseJsonDate(data);
        if (type === "display" || type === "filter") {
            return FormatFechaHora(value, false);
        }
        return value;
    }
    function getResumenValuacion() {
        showLoading();
        $.ajax({
            url: `${BASE_URL}Valuaciones/ResumenVigente/${idUnidadTributaria}`,
            dataType: "json",
            type: "GET",
            success: function (resumen) {
                let valorTierra, valorTotal, vigencia;
                if (resumen.Vigente) {
                    const formatter = new Intl.NumberFormat('en-US', {
                        style: 'currency',
                        currency: 'USD',
                    });
                    vigencia = FormatFechaHora(resumen.FechaDesde);
                    valorTierra = formatter.format(resumen.ValorTierra);
                    valorTotal = formatter.format(resumen.ValorTotal);
                }

                $("#ValorTierra", container).val(valorTierra);
                $("#ValorFiscalTotal", container).val(valorTotal);
                $("#VigenciaValorFiscalTotal", container).val(vigencia);
            },
            error: function (error) {
                mostrarMensajeError(error.responseText, "Error al obtener los valores de la cabecera", true);
            },
            complete: hideLoading
        });
    }

    const formularioVigenteOpts = {
        ajax: `${BASE_URL}Valuaciones/GetFormularioVigente/${idUnidadTributaria}`,
        columns: [
            { title: "Tipo", data: "Tipo" },
            { title: "Vigencia Desde", data: "VigenciaDesde", render: { _: "display", sort: "timestamp" } },
            { title: "Origen", data: "Origen" }
        ],
        createdRow: function (row, data) {
            $('td:last-of-type', row).addClass((data["Origen"] || "npi").toLowerCase().replace("ó", "o"));
        },
        initComplete: function (options) {
            $(this).dataTable().api().columns.adjust();
            ajustarScrollBars();
            $(options.nTBody)
                .off("click", "tr")
                .on("click", "tr", function (evt) {
                    evt.preventDefault();
                    evt.stopImmediatePropagation();
                    $(this).toggleClass("selected");
                    enableOrDisableButtonsFormularioVigente(this);
                });
        }
    };
    const tableFormularioVigente = initGrid("grillaFormularioVigente", formularioVigenteOpts);

    const formulariosHistoricosOpts = {
        ajax: `${BASE_URL}Valuaciones/GetFormulariosHistoricos/${idUnidadTributaria}`,
        columns: [
            { title: "Tipo", data: "Tipo" },
            { title: "Vigencia Desde", data: "VigenciaDesde", render: { _: "display", sort: "timestamp" } },
            { title: "Vigencia Hasta", data: "VigenciaHasta", render: { _: "display", sort: "timestamp" } },
            { title: "Origen", data: "Origen" }
        ],
        createdRow: function (row, data) {
            $('td:last-of-type', row).addClass((data["Origen"] || "npi").toLowerCase().replace("ó", "o"));
        },
        initComplete: function (options) {
            $(this).dataTable().api().columns.adjust();
            ajustarScrollBars();
            $(options.nTBody)
                .off("click", "tr")
                .on("click", "tr", function (evt) {
                    evt.preventDefault();
                    evt.stopPropagation();
                    $(this).toggleClass("selected")
                        .siblings().removeClass("selected");
                    enableOrDisableButtonsFormulariosHistoricos(this);
                });
        }
    }
    const tableFormulariosHistoricos = initGrid("grillaFormulariosHistoricos", formulariosHistoricosOpts);

    const valuacionesHistoricasOpts = {
        ajax: `${BASE_URL}Valuaciones/GetValuacionesHistoricas/${idUnidadTributaria}`,
        order: [1, 'desc'],
        columns: [
            { title: "Desde", data: "FechaDesde", render: dateTimeRenderer },
            { title: "Hasta", data: "FechaHasta", render: dateTimeRenderer },
            { title: "Valor Tierra", data: "ValorTierra", render: $.fn.dataTable.render.number(",", ".", 2, "$"), "defaultContent": "-" },
            { title: "VFT", data: "ValorTotal", render: $.fn.dataTable.render.number(",", ".", 2, "$"), "defaultContent": "-" },
            { title: "Superficie", data: "Superficie", "defaultContent": "-" },
        ],
        initComplete: function (options) {
            $(this).dataTable().api().columns.adjust();
            $(options.nTBody)
                .off("click", "tr")
                .on("click", "tr", function (e) {
                    e.preventDefault();
                    e.stopPropagation();
                    $(this).toggleClass("selected")
                        .siblings().removeClass("selected");

                    enableOrDisableButtonsValuacionesHistoricas(this);
                });
        }
    }
    initGrid("grillaValuacionesHistoricas", valuacionesHistoricasOpts);

    getResumenValuacion();

    $("#btnAgregar", container).click(function () {
        showLoading();
        $("#contenido-formulario")
            .one("hidden.bs.modal", () => {
                const existeMantenedorExterno = $("#mantenedor-externo-container").length > 0;
                const procesarValuacionGenerada = () => {
                    delay(() => {
                        if (existeMantenedorExterno) {
                            cargarAdminValuaciones(`${BASE_URL}valuaciones/administrador/${idUnidadTributaria}`);
                        } else {
                            loadView(`${BASE_URL}valuaciones/administrador/${idUnidadTributaria}`);
                            $(container)
                                .one("hidden.bs.modal", () => $("container").empty())
                                .modal("hide");
                        }
                    }, 100);
                };
                $(document)
                    .off("valuacion-generada")
                    .one("valuacion-generada", procesarValuacionGenerada);
            })
            .load(`${BASE_URL}Valuaciones/NuevoFormulario`, { id: idUnidadTributaria });
    });

    function cargarAdminValuaciones(url, data) {
        closeLeftSideMenu();
        showLoading();
        if (url.indexOf("?") === -1) {
            url = url + "?" + new Date().getTime();
        }
        else {
            url = url + "&unique=" + new Date().getTime();
        }
        if (data) {
            $("#mantenedor-externo-container").load(url, data);
        } else {
            $("#mantenedor-externo-container").load(url);
        }
    }

    $("#btnImprimirFormulario", container).on("click", imprimirFormulario.bind(null, tableFormularioVigente));

    $("#btnImprimirFormularioHistorico", container).on("click", imprimirFormulario.bind(null, tableFormulariosHistoricos));

    $("#btnVerFormulario", container).on("click", verFormulario.bind(null, tableFormularioVigente));

    $("#btnVerFormularioHistorico", container).on("click", verFormulario.bind(null, tableFormulariosHistoricos));

    $("#btnImprimirHistoricoValuaciones", container).on("click", function () {
        showLoading();
        $.ajax({
            //url: `${BASE_URL}Valuaciones/GenerarInformeHistoricoValuaciones`,
            url: `${BASE_URL}Valuaciones/CertificadoValuatorio`,
            data: { id: idUnidadTributaria },
            type: "POST",
            success: abrirInformeGenerado,
            error: function (err) {
                mostrarMensajeError("Ha ocurido un error al generar el informe.", "Histórico de Valuaciones", true);
            },
            complete: hideLoading
        });
    });

    $(container).one("shown.bs.modal", () => {
        ajustarmodal();
        hideLoading();
    });
}
ValuacionesController("#modal-valuaciones");