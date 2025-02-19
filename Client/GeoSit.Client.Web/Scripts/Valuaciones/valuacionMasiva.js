var corridaSeleccionada = null

$(document).ready(function () {
    init();
});

$(window).resize(ajustarmodal);

$('#generacionCertificadosModal').on('shown.bs.modal', function (e) {
    ajustarScrollBars();
    hideLoading();
});

async function init() {
    $(".certificados-content").niceScroll(getNiceScrollConfig());

    ajustarmodal();

    $('#tablaValuaciones tbody').on('click', 'tr', function () {
        $('#tablaValuaciones tr').not(this).attr('style', '');
        $(this).attr('style', 'background-color: #B0BED9 !important;');
        corridaSeleccionada = $(this).find('td').eq(0).text();
    });

    $('#btn-detalle-corrida').on('click', function (e) {
        e.preventDefault();
        verDetalleCorrida();
    });

    $('#btn-eliminar-corrida').on('click', async function (e) {
        e.preventDefault();
        await eliminarCorridaTemporal();
    });

    $('#btn-nueva-val-temp').on('click', async function (e) {
        e.preventDefault();
        await generarValuacionTemporal();
    });

    $('#btn-pasar-val-prod').on('click', async function (e) {
        e.preventDefault();
        await pasarValuacionTmpProduccion();
    });

    await getValuacionTempCorrida();
    seleccionarPrimerValuacion();

    $("#generacionCertificadosModal").modal("show");
}

function ajustarScrollBars() {
    temp = $(".certificados-body").height();
    var outerHeight = 20;
    $('#accordion-certificados .collapse').each(function () {
        outerHeight += $(this).outerHeight();
    });
    $('#accordion-certificados .panel-heading').each(function () {
        outerHeight += $(this).outerHeight();
    });
    temp = Math.min(outerHeight, temp);
    $('.certificados-content').css({ "max-height": temp + 'px' })
    $('#accordion-certificados').css({ "max-height": temp + 1 + 'px' })
    $(".certificados-content").getNiceScroll().resize();
    $(".certificados-content").getNiceScroll().show();
}

function ajustarmodal() {
    var viewportHeight = $(window).height(),
        headerFooter = 180,
        altura = viewportHeight - headerFooter;
    $(".certificados-body").css({ "height": altura });
    $(".certificados-content").css({ "height": altura, "overflow": "hidden" });
    ajustarScrollBars();
}

async function getValuacionTempCorrida() {
    try {
        showLoading(); 
        const response = await fetch(BASE_URL + 'Valuaciones/GetValTmpCorridas');
        if (!response.ok) {
            throw new Error('Error al obtener los datos.');
        }
        const data = await response.json();
        if (data && data.length > 0) {
            $('#tablaValuaciones tbody').empty();

            const rows = data.reduce((accum, row) => {
                return accum + `
                    <tr>
                        <td>${row.Corrida}</td>
                        <td>${row.FechaProc}</td>
                        <td>${row.CantidadParcProc}</td>
                        <td>${row.SupValuada}</td>
                        <td>${row.ValTotal}</td>
                        <td>${row.ValMax}</td>
                        <td>${row.ValMin}</td>
                        <td>${row.PromedioValParc}</td>
                    </tr>
                `;
            }, "");

            $('#tablaValuaciones tbody').append(rows);
        }
    } catch (error) {
        mostrarMensajeError(["Error: " + error], "Valuación Masiva - Error", true);
    } finally {
        hideLoading();
    }
}

function verDetalleCorrida() {
    showLoading();
    $.ajax({
        url: BASE_URL + 'Valuaciones/GetInformeDetalleCorrida',
        type: 'GET',
        data: { corrida: corridaSeleccionada },
        success: function () {
            window.open(BASE_URL + "Valuaciones/AbrirReporte/", "_blank");
        },
        error: function (xhr) {
            mostrarMensajeError(["Error: " + xhr.statusText], "Valuación Masiva - Error", true);
        },
        complete: function () {
            hideLoading();
        }
    });
}

function seleccionarPrimerValuacion() {
    var primerVal = $('#tablaValuaciones tbody tr:first');
    if (primerVal.length > 0) {
        primerVal.attr('style', 'background-color: #B0BED9 !important;');
        corridaSeleccionada = primerVal.find('td').eq(0).text();
    }
}

async function eliminarCorridaTemporal() {
    const result = await mostrarMensajeGeneral([
        "Se procederá con la baja de la corrida temporal seleccionada (N°" + corridaSeleccionada + ").",
        "Esto quiere decir que se darán de baja todas las valuaciones temporales que coincidan con la corrida seleccionada.",
        "",
        "¿Desea continuar?"
    ], "Valuación Masiva - Eliminar", true);

    if (!result) return;

    showLoading();
    try {
        const response = await $.ajax({
            url: BASE_URL + 'Valuaciones/EliminarCorridaTemporal',
            type: 'GET',
            data: { corrida: corridaSeleccionada }
        });

        if (response.success) {
            await getValuacionTempCorrida();
            seleccionarPrimerValuacion();
        }
        mostrarMensajeError([response.message], "Valuación Masiva - Eliminar", true);
    } catch (xhr) {
        mostrarMensajeError(["Error: " + xhr.statusText], "Valuación Masiva - Error", true);
    } finally {
        hideLoading();
    }
}

async function generarValuacionTemporal() {
    const result = await mostrarMensajeGeneral([
        "Este proceso puede requerir algunos minutos para completarse.",
        "",
        "¿Desea continuar?",
    ], "Valuación Masiva - Nueva valuación temporal", true);
    if (!result) return;

    showLoading();
    try {
        const response = await fetch(BASE_URL + 'Valuaciones/GenerarValuacionTemporal', {
            method: 'GET',
        });
        if (response.ok) {
            await getValuacionTempCorrida();
            seleccionarPrimerValuacion();
            mostrarMensajeGeneral(["El proceso ha finalizado correctamente."], "Valuación Masiva - Nueva valuación temporal");
        }
    } catch (error) {
        mostrarMensajeError([`Error: ${error.message}`], "Valuación Masiva - Error", true);
    } finally {
        hideLoading();
    }
}

async function pasarValuacionTmpProduccion() {
    const result = await mostrarMensajeGeneral([
        "Se procederá con el paso a producción de la corrida temporal N°" + corridaSeleccionada,
        "Este proceso puede requerir algunos minutos para completarse.",
        "",
        "¿Desea continuar?",
    ], "Valuación Masiva - Pasar valuación temporal a producción", true);
    if (!result) return;
    showLoading();
    $.ajax({
        url: BASE_URL + 'Valuaciones/PasarValuacionTmpProduccion',
        type: 'GET',
        data: { corrida: corridaSeleccionada },
        success: function (response, status, xhr) {
            if (xhr.status == 200) {
                mostrarMensajeGeneral(["El proceso ha finalizado correctamente."], "Valuación Masiva - Pasar valuación temporal a producción");
            } 
        },
        error: function (xhr) {
            mostrarMensajeError(["Error: " + xhr.statusText], "Valuación Masiva - Error", true);
        },
        complete: function () {
            hideLoading();
        }
    });
}

function mostrarMensaje(mensajes, titulo, tipo) {
    $('#TituloInfo', '#ModalInfo').html(titulo);
    $('#DescripcionInfo', '#ModalInfo').html(mensajes.join("<br />"));
    $("[role='alert']", "#ModalInfo").removeClass("alert-danger alert-success alert-info alert-warning").addClass(tipo);
    $(".modal-footer", "#ModalInfo").hide();
    if (tipo === "alert-info") {
        $(".modal-footer", "#ModalInfo").show();
    }
    $("#ModalInfo").modal('show');
}

function mostrarMensajeError(mensajes, titulo, error) {
    return mostrarMensaje(mensajes, titulo, (error || false ? "alert-danger" : "alert-warning"));
}

function mostrarMensajeGeneral(mensajes, titulo, confirmacion) {
    mostrarMensaje(mensajes, titulo, (confirmacion ? "alert-warning" : "alert-success"));
    if (confirmacion) {
        $(".modal-footer", "#ModalInfo").show();
        return new Promise(ok => {
            $("#ModalInfo").off("hidden.bs.modal").one("hidden.bs.modal", ok.bind(null, false))
            $("#btnInfoOK").on("click", ok.bind(null, true));
        });
    }
    return Promise.resolve(true);
}