const container = "#modal-window-ploteador";

$(document).ready(init);
$(window).resize(ajustarScrollBars);
function init() {
    ///////////////////// Scrollbars ////////////////////////
    $(".scrollable-panel.left").niceScroll(getNiceScrollConfig());
    $('.scrollable-panel.left .panel-heading').click(ajustarLeftScrollBar);

    ////////////////////////////////////////////////////////
    MarcarColeccionSeleccionada();

    $(container)
        .one('shown.bs.modal', () => {
            ajustarScrollBars();
            hideLoading();
        })
        .one('hidden.bs.modal', () => {
            $(document).off("planilla-cargada");
        }).modal("show");
}

function ajustarScrollBars() {
    ajustarLeftScrollBar();
    ajustarRightScrollBar();
}

function ajustarLeftScrollBar() {
    delay(() => {
        $(".scrollable-panel.left").getNiceScroll().resize();
        $(".scrollable-panel.left").getNiceScroll().show();
    }, 10);
}

function rightScrollBarInit() {
    $(".scrollable-panel.right").niceScroll(getNiceScrollConfig());
    $(".scrollable-panel.right input[type=radio]").click(ajustarRightScrollBar);
    $(".scrollable-panel.right .panel-heading").click(ajustarRightScrollBar);
    ajustarRightScrollBar();
}
function ajustarRightScrollBar() {
    delay(() => {
        $(".scrollable-panel.right").getNiceScroll().resize();
        $(".scrollable-panel.right").getNiceScroll().show();
    }, 10);
}

function cargarPlantilla(url) {
    showLoading();
    $(document)
        .off("plantilla-cargada")
        .one("plantilla-cargada", ({ controller }) => {
            const form = $("form", ".ploteo-content");
            const btnGenerar = $("#btnGenerar", container)
                .attr("disabled", true)
                .off("click")
                .on("click", async () => {
                    try {
                        showLoading();
                        if (await controller.generate()) {
                            IS_DOWNLOAD = true;
                            window.location = `${BASE_URL}Ploteo/DownloadZip`;
                        }
                    } catch (error) {
                        let msg = "Ha ocurrido un error al generar el ploteo.";
                        switch (error.status) {
                            case 409:
                                msg = "Error en plantilla. Uno o más campos requeridos no contienen entradas.";
                                break;
                            case 411:
                                msg = "No se encontraron todos los objetos a plotear";
                                break;
                            case 412:
                                msg = "No se pudo plotear ninguno de los objetos solicitados";
                                break;
                            case 417:
                                msg = "Supera el máximo permitido de ploteos.";
                                break;
                            default:
                                break;
                        }
                        $("#mensaje-error-ploteo").text(msg);
                        $("#ploteo-error-modal").modal('show');
                    } finally {
                        hideLoading();
                    }
                });
            controller.onMostrarError(({ error }) => {
                $("#mensaje-error-ploteo").text(error);
                $("#ploteo-error-modal").modal('show');
            });
            controller.onGenerarEnabled(() => btnGenerar.attr("disabled", false));
            controller.onGenerarDisabled(() => btnGenerar.attr("disabled", true));
            controller.init(form);
            rightScrollBarInit();
            ajustarRightScrollBar();
            hideLoading();
        });
    $("#configuracion-ploteo", container).load(url);
    //$("#configuracion-ploteo").load(url, function (_, status) {
    //    if (status === "success") {
    //        $("#btnGenerar").off("click");
    //        $("#btnGenerar").attr('disabled', true);

    //        const idFormulario = $("#configuracion-ploteo form").attr("id");
    //        cargarTextosVariables();
    //        buscarComponente();
    //        checkAllAgregar();
    //        checkAllEliminar();
    //        Agregar();
    //        borrarObjetos();
    //        CallGenerar();
    //        mostrarColeccionGral();
    //        VisualizacionGeneral();
    //        mostrarColeccion();
    //        mostrarListaManzanas();
    //        mostrarListaExpte();
    //        initCtrlsPluggins(idFormulario);
    //        ComposicionColeccionA4();
    //        ComposicionColeccionGeneral();
    //        ComposicionColeccionObra();
    //        if (idTipo !== 13) { //Corregir cambiando el id del componente
    //            ManzanaZonaResultado();
    //        } else if (idTipo === 13) {
    //            ObrasZonaResultado();
    //        }
    //        BuscarObjetosConTeclaEnter();
    //        rightScrollBarInit();
    //    }
    //    hideLoading();
    //});
}

function MarcarColeccionSeleccionada() {
    $('#grLisColecciones li').on('click', function () {
        $("#grLisColecciones li").each(function () {
            $(this).removeClass("selected-option-light-blue");
        });
        $(this).addClass("selected-option-light-blue");
    });
}
//COMPORTAMIENTO PLANCHETA A4//

var componentesSeleccionados = {};

function ManzanaZonaResultado() {
    $("#btnGenerar").attr('disabled', false);
    $('#radioObjetos').attr('checked', true);
    $('#ordManzana').prop('checked', true);

    //DESHABILITADAS
    //ordExpte
    $('#ordExpte').prop('checked', false);
    $("#ordExpte").attr("disabled", "disabled");

    //ordOrigen
    $('#ordOrigen').prop('checked', false);
    $("#ordOrigen").attr("disabled", "disabled");

    $("#grabarListado").removeAttr("disabled", "disabled");
    $("#grabarListado").prop("checked", false);

    //Revisar elementos seleccionados
    componentesSeleccionados = [];
    $.each($(".results [data-componente-ploteable='true']:checked"), function () {
        var el = $(this);
        var componenteSeleccionado = $(this).data("componente");
        $.each(ComponentesPloteables, function (i, componente) {
            if (componente.DocType == componenteSeleccionado) {
                componentesSeleccionados.push({
                    Id: el.data('name'),
                    Componente: componenteSeleccionado,
                    ComponenteDisplay: componente.Nombre
                });
            }
        });
    });

    var soloManzanas = true;
    $.each(componentesSeleccionados, function (i, val) {
        if (val.Componente !== "manzanas") {
            soloManzanas = false;
        }
    });

    $("#manzanasDupliacadas").prop("checked", !soloManzanas);
    if (soloManzanas)
        habilitarOpcionesSoloManzanas();
    else
        habilitarOpcionesMultiplesComponentes();

    dibujarBadges();

    //ONCHANGE
    $("#radioObjetos").on("change", function () {

        $("#btnGenerar").attr('disabled', false);
        $("#divColeccion").addClass("hide");
        $("#divManzanas").addClass("hide");
        $("#divExpte").addClass("hide");

        //ordenamiento
        var soloManzanas = true;
        $.each(componentesSeleccionados, function (i, val) {
            if (val.Componente !== "manzanas") {
                soloManzanas = false;
            }
        });
        $("#manzanasDupliacadas").prop("checked", !soloManzanas);
        if (soloManzanas)
            habilitarOpcionesSoloManzanas();
        else
            habilitarOpcionesMultiplesComponentes();

        $("#cotas").prop("checked", false);

        $(".file").fileinput("clear");
    });
}

function ObrasZonaResultado() {
    $('#radioObjetos').attr('checked', true);
    $("#btnGenerar").attr('disabled', false);

    var idComponentePrincipal = $("#idComponentePrincipal").val();
    //$.each($(".results [data-componente-ploteable='true']:checked"), function () {
    componentesSeleccionados = [];
    $.each($(".results :checked"), function () {
        var el = $(this);
        var componenteSeleccionado = $(this).data("componente");
        $.each(ComponentesPloteables, function (i, componente) {
            if (componente.DocType == componenteSeleccionado) {
                componentesSeleccionados.push({
                    Id: el.data('name'),
                    Componente: componenteSeleccionado,
                    ComponenteDisplay: componente.Nombre
                });
            }
        });

    });

    dibujarBadges(true);

    $("#radioObjetos").on("change", function () {
        $("#btnGenerar").attr('disabled', false);
        $("#divColeccion").addClass("hide");
        $("#divManzanas").addClass("hide");
        $("#divExpte").addClass("hide");

        //Coleccion
        $("#cboColeccionesObras").select2("val", 0);

        //ordenamiento
        var soloManzanas = true;
        $.each(componentesSeleccionados, function (i, val) {
            if (val.Componente !== "manzanas") {
                soloManzanas = false;
            }
        });
        $("#manzanasDupliacadas").prop("checked", !soloManzanas);
        if (soloManzanas)
            habilitarOpcionesSoloManzanas();
        else
            habilitarOpcionesMultiplesComponentes();

        $("#cotas").prop("checked", false);

        $(".file").fileinput("clear");
    });
}

function getBadge(componentes, esObjeto) {

    var badge = $('<span>').addClass('badge');

    var componentesString = [];
    var total = 0;
    $.each(componentes, function (componente, cant) {
        componentesString.push(cant + " " + componente);
        total += cant;
    });
    if (esObjeto)
        badge.text(total + " " + (total != 1 ? "Objetos" : "Objeto"));
    else
        badge.text(total + " " + (total != 1 ? "Componentes" : "Componente"));

    if (total > 0) {

        badge.tooltip({
            title: componentesString.join(", "),
            container: 'body'
        });

    }

    return badge
}

function dibujarBadges(esObjeto) {

    var container = $('#objetosSeleccionados');
    container.empty();

    var componentes = {};
    $.each(componentesSeleccionados, function (_, val) {
        componentes[val.ComponenteDisplay] = !componentes[val.ComponenteDisplay] ? componentes[val.ComponenteDisplay] : 0;
        componentes[val.ComponenteDisplay]++;
    });

    var badge = getBadge(componentes, esObjeto);

    container.append(badge);
}

function mostrarColeccion() {

    //COMPORTAMIENTO DEFAULT COLECCIONA4, NI BIEN SE CARGA PLANTILLAA4
    //DESHABILITAR BOTON GENERAR, SI NOY HAY COLECCION SELECCIONADA
    $("#btnGenerar").attr('disabled', true);

    //HABILITAR ORD MANZANA
    $("#lblManzana").removeClass("disabled");
    $('#ordManzana').removeAttr("disabled", "disabled");
    $('#ordManzana').prop('checked', true);

    //DESHABILITAR ORD EXP. Y ORD ORIGEN
    $("#lblExpte").addClass("disabled");
    $("#ordExpte").attr("disabled", "disabled");
    $('#ordExpte').prop('checked', false);

    $("#lblOrigen").addClass("disabled");
    $("#ordOrigen").attr("disabled", "disabled");
    $('#ordOrigen').prop('checked', false);

    //DESHABILITAR CHECKBOX MANZANA DUPLICADA
    $("#manzanasDupliacadas").prop("checked", false);
    $("#manzanasDupliacadas").attr("disabled", "disabled");

    $("#grabarListado").prop("checked", false);
    $("#grabarListado").removeAttr("disabled", "disabled");

    //ONCHANGE- COMPORTAMIENTO COLECCIONA4, AL VOLVER A LA OPCION COLECCION A4
    //DESDE ALGUNA DE LAS OTRAS TRES
    $("#radioColeccion").on("change", function () {

        //HABILITAR ORD MANZANA
        $("#lblManzana").removeClass("disabled");
        $('#ordManzana').removeAttr("disabled", "disabled");
        $('#ordManzana').prop('checked', true);

        $("#cboColeccionesA4").select2("val", 0)

        //VENGO DE OTRA OPCION
        $("#lblColeccionCompuesta").empty();
        //DESHABILITAR BOTON GENERAR, SI NO HAY COLECCION SELECCIONADA
        $("#btnGenerar").attr('disabled', true);

        //MOSTRAR COLECCION
        $("#divColeccion").removeClass("hide");

        //OCULAR LISTADO MANZANA Y LISTADO EXPEDIENTE
        $("#divManzanas").addClass("hide");
        $("#divExpte").addClass("hide");

        //DESHABILITAR ORD EXP. Y ORD ORIGEN
        $("#lblExpte").addClass("disabled");
        $("#ordExpte").attr("disabled", "disabled");
        $('#ordExpte').attr('checked', false);

        $("#lblOrigen").addClass("disabled");
        $("#ordOrigen").attr("disabled", "disabled");
        $('#ordOrigen').attr('checked', false);

        //DESHABILITAR CHECKBOX MANZANA DUPLICADA
        $("#manzanasDupliacadas").prop("checked", false);
        $("#manzanasDupliacadas").attr("disabled", "disabled");

        $("#cotas").prop("checked", false);
        $(".file").fileinput("clear");
        $("#grabarListado").prop("checked", false);
        $("#grabarListado").removeAttr("disabled", "disabled");

    });
}

function mostrarListaManzanas() {

    $("#cboColeccionesA4").select2("val", 0)
    //SI NO HAY ARCHIVO SELECCIONADO, DESHABILITAR BOTON GENERAR
    $("#btnGenerar").attr('disabled', true);
    //VERIFICAR SI YA ESTABA EL COMPORTAMIENTO DEL CUADRITO
    $("#manzanasDupliacadas").prop("checked", false);
    $("#manzanasDupliacadas").attr("disabled", "disabled");

    $("#grabarListado").prop("checked", false);
    $("#grabarListado").attr("disabled", "disabled");

    //ONCHANGE
    $("#radioListManzanas").on("change", function () {

        //$("#listManzanas").val("");
        //SI NO HAY ARCHIVO SELECCIONADO, DESHABILITAR BOTON GENERAR
        $("#btnGenerar").attr('disabled', true);

        $("#divManzanas").removeClass("hide");

        $("#divExpte").addClass("hide");
        $("#divColeccion").addClass("hide");

        //ordenamiento
        $("#manzanasDupliacadas").prop("checked", false);
        $("#manzanasDupliacadas").attr("disabled", "disabled");
        $("#lblOrigen").removeClass("disabled");
        $("#ordOrigen").removeAttr("disabled", "disabled");

        $("#lblExpte").addClass("disabled");
        $("#ordExpte").attr("disabled", "disabled");
        $('#ordExpte').prop('checked', false);

        $("#grabarListado").prop("checked", false);
        $("#grabarListado").attr("disabled", "disabled");
        $("#cotas").prop("checked", false);
        $(".file").fileinput("clear");

        $("#lblManzana").removeClass("disabled");
        $('#ordManzana').removeAttr("disabled", "disabled");
        $('#ordManzana').prop('checked', true);

    });

}

function mostrarListaExpte() {

    //LIMPIAR EL ARCHIVO SELECCIONADO, SI TENIA. VER SI HACE FALTA EN LA CARGA POR DEFAULT

    //SI NO HAY ARCHIVO SELECCIONADO, DESHABILITAR BOTON GENERAR
    $("#btnGenerar").attr('disabled', true);

    //HABILITAR CHECKBOX MANZANA DUPLICADA
    $("#manzanasDupliacadas").prop("checked", true);
    $("#manzanasDupliacadas").removeAttr("disabled", "disabled");

    //COMPORTAMIENTO DEFAULT ListaExpte, NI BIEN SE CARGA PLANTILLAA4
    //HABILITAR ORD MANZANA
    $("#lblManzana").removeClass("disabled");
    $('#ordManzana').removeAttr("disabled", "disabled");
    $('#ordManzana').attr('checked', true);

    //HABILITAR ORD EXP. Y ORD ORIGEN
    $("#lblExpte").removeClass("disabled");
    $('#ordExpte').removeAttr("disabled", "disabled");
    $('#ordExpte').attr('checked', true);

    $("#lblOrigen").removeClass("disabled");
    $('#ordOrigen').removeAttr("disabled", "disabled");
    $('#ordOrigen').attr('checked', true);

    $("#grabarListado").prop("checked", false);
    $("#grabarListado").attr("disabled", "disabled");

    //ONCHANGE- COMPORTAMIENTO COLECCIONA4, AL VOLVER A LA OPCION ListaExpte
    $("#radioListExpte").on("change", function () {

        //SI NO HAY ARCHIVO SELECCIONADO, DESHABILITAR BOTON GENERAR
        $("#btnGenerar").attr('disabled', true);

        $("#divExpte").removeClass("hide");

        $("#divManzanas").addClass("hide");
        $("#divColeccion").addClass("hide");

        //HABILITAR CHECKBOX MANZANA DUPLICADA
        $("#manzanasDupliacadas").prop("checked", true);
        $("#manzanasDupliacadas").removeAttr("disabled", "disabled");

        //COMPORTAMIENTO DEFAULT ListaExpte, NI BIEN SE CARGA PLANTILLAA4
        //HABILITAR ORD MANZANA
        $("#lblManzana").removeClass("disabled");
        $('#ordManzana').removeAttr("disabled", "disabled");
        $('#ordManzana').attr('checked', true);

        //HABILITAR ORD EXP. Y ORD ORIGEN
        $("#lblExpte").removeClass("disabled");
        $('#ordExpte').removeAttr("disabled", "disabled");
        $('#ordExpte').attr('checked', true);

        $("#lblOrigen").removeClass("disabled");
        $('#ordOrigen').removeAttr("disabled", "disabled");
        $('#ordOrigen').attr('checked', true);

        $("#grabarListado").prop("checked", false);
        $("#grabarListado").attr("disabled", "disabled");
        $("#cotas").prop("checked", false);

        //SI VENGO DE OTRA OPCION, LIMPIAR ARCHIVO SELECCIONADO, SI TENIA ALGUNO PREVIAMENTE
        $(".file").fileinput("clear");

    });

}

function GenerarListadoTxt(idPlA4, idColA4, idCompPpalA4, ordPorExpediente, ordPorManzana, graboList) {
    IS_DOWNLOAD = true;
    window.location = BASE_URL + "Ploteo/GenerarListadoTXTPlanchetaA4ByColeccion?id=" + idPlA4 + "&idColeccion=" + idColA4 + "&idComponentePrincipal=" + idCompPpalA4 + "&ordenarPorExpediente=" + ordPorExpediente + "&ordenarPorManzana=" + ordPorManzana + "&grabarListado=" + graboList;
}

function ComposicionColeccionA4() {
    $("[name='optradio'").change(function () { $("#lblColeccionCompuesta").empty(); });
    //DESHABILITA FUNCION TildarManzanasDuplicadas
    //DESTILDAR CHECKBOX MANZANADUPLICADA
    $("#manzanasDupliacadas").prop("checked", false);
    //DESHABILITAR CHECKBOX MANZANADUPLICADA
    $("#manzanasDupliacadas").attr("disabled", "disabled");

    //SETEAR CONTROLES DE ORDENAMIENTO PARA COLECCION PLANCHETA A4

    //HABILITAR ORD MANZANA
    $("#ordManzana").removeAttr("disabled", "disabled");
    // SELECCIONAR MANZANA
    $('#ordManzana').attr('checked', true);

    //DESHABILITAR ORD EXPEDIENTE
    $("#ordExpte").attr("disabled", "disabled");
    // LIMPIAR SELECCION SI ESTABA SELECCIONADO DE ANTES
    $('#ordExpte').attr('checked', false);

    //DESHABILITAR ORD ORIGEN    
    //LIMPIAR SELECCION SI ESTABA SELECCIONADO DE ANTES
    $("#ordOrigen").prop("checked", false);
    //DESHABILITAR LUEGO
    $("#ordOrigen").attr("disabled", "disabled");

    //ON CHANGE
    $("#cboColeccionesA4").on("change", function () {
        //SI ELEGI UNA COLECCION, HABILITAR BOTON GENERAR
        $("#btnGenerar").attr('disabled', false);

        var idColeccionA4 = $("#cboColeccionesA4").select2("data").id;
        var valorCombo = $("#cboColeccionesA4").val();
        var coleccionContiene = "";
        if (valorCombo != 0) {
            $.ajax({
                url: BASE_URL + "Ploteo/ComposicionColeccion",
                type: "GET",
                cache: false,
                data: { idColeccion: idColeccionA4 },
                success: function (data) {

                    var soloManzanas = true;
                    $.each(data, function (componente, cantidad) {
                        if (componente != "manzanas")
                            soloManzanas = false;
                    });

                    $("#lblColeccionCompuesta").html(getBadge(data));

                    //COLECCION ELEGIDA TIENE SOLO MANZANAS 
                    if (soloManzanas) {
                        habilitarOpcionesSoloManzanas();
                    } else {
                        habilitarOpcionesMultiplesComponentes();
                    }

                },
                error: function (error) {
                    console.debug(error);
                }
            });
        } else {
            $("#lblColeccionCompuesta").empty();
        }
    });

    $('[data-comercial]').change(function () {
        var obj = $(this);
        var opc = $(this).data('comercial');

        if (obj.prop("checked")) {
            $('[name="grafico[' + opc + ']"]').prop('checked', true);
            $('[name="grafico[' + opc + ']"]').prop('disabled', false);
            $('[name="leyenda[' + opc + ']"]').prop('checked', true);
            $('[name="leyenda[' + opc + ']"]').prop('disabled', false);
        } else {
            $('[name="grafico[' + opc + ']"]').prop('checked', false);
            $('[name="grafico[' + opc + ']"]').prop('disabled', true);
            $('[name="leyenda[' + opc + ']"]').prop('checked', false);
            $('[name="leyenda[' + opc + ']"]').prop('disabled', true);
        }

    });

    $('#collapseInformacionComercial [data-toggle="tooltip"]').tooltip({
        placement: 'right'
    });

    $('#collapseOpciones [data-toggle="tooltip"]').tooltip({
        placement: 'right'
    });
}

function habilitarOpcionesSoloManzanas() {

    //DESHABILITA FUNCION TildarManzanasDuplicadas
    //DESTILDAR CHECKBOX MANZANADUPLICADA
    $("#manzanasDupliacadas").prop("checked", false);
    //DESHABILITAR CHECKBOX MANZANADUPLICADA
    $("#manzanasDupliacadas").attr("disabled", "disabled");

    //SETEAR CONTROLES DE ORDENAMIENTO PARA COLECCION PLANCHETA A4

    //HABILITAR ORD MANZANA
    $("#lblManzana").removeClass("disabled");
    $("#ordManzana").removeAttr("disabled", "disabled");
    // SELECCIONAR MANZANA
    $('#ordManzana').attr('checked', true);

    //DESHABILITAR ORD EXPEDIENTE
    $("#lblExpte").addClass("disabled");
    $("#ordExpte").attr("disabled", "disabled");
    // LIMPIAR SELECCION SI ESTABA SELECCIONADO DE ANTES
    $('#ordExpte').attr('checked', false);

    //DESHABILITAR ORD ORIGEN    
    //LIMPIAR SELECCION SI ESTABA SELECCIONADO DE ANTES
    $("#ordOrigen").prop("checked", false);
    //DESHABILITAR LUEGO
    $("#lblOrigen").addClass("disabled");
    $("#ordOrigen").attr("disabled", "disabled");

    $("#grabarListado").prop("checked", false);
    $("#grabarListado").removeAttr("disabled", "disabled");
}

function habilitarOpcionesMultiplesComponentes() {
    //HABILITA FUNCION TildarManzanasDuplicadas
    //HABILITAR CHECKBOX MANZANADUPLICADA
    $("#manzanasDupliacadas").prop("checked", true);
    $("#manzanasDupliacadas").removeAttr("disabled", "disabled");

    //HABILITAR ORD MANZANA
    $("#lblOrigen").removeClass("disabled");
    $("#ordManzana").removeAttr("disabled", "disabled");
    // SELECCIONAR MANZANA
    $('#ordManzana').attr('checked', true);

    //HABILITAR ORD EXPEDIENTE
    $("#lblExpte").removeClass("disabled");
    $("#ordExpte").removeAttr("disabled", "disabled");
    // LIMPIAR SELECCION SI ESTABA SELECCIONADO DE ANTES
    $('#ordExpte').attr('checked', false);

    //DESHABILITAR ORD ORIGEN    
    //LIMPIAR SELECCION SI ESTABA SELECCIONADO DE ANTES
    $("#ordOrigen").prop("checked", false);
    //DESHABILITAR LUEGO
    $("#lblOrigen").addClass("disabled");
    $("#ordOrigen").attr("disabled", "disabled");

    $("#grabarListado").prop("checked", false);
    $("#grabarListado").removeAttr("disabled", "disabled");

}

//ON CHANGE
function ComposicionColeccionObra() {
    $("#cboColeccionesObras").on("change", function () {
        //SI ELEGI UNA COLECCION, HABILITAR BOTON GENERAR
        $("#btnGenerar").attr('disabled', false);

        var idColeccionA4 = $("#cboColeccionesObras").select2("data").id;
        var valorCombo = $("#cboColeccionesObras").val();
        var coleccionContiene = "";

        var idComponentePrincipal = $("#idComponentePrincipal").val();

        if (valorCombo != 0) {
            $.ajax({
                url: BASE_URL + "Ploteo/ComposicionColeccionObra",
                type: "GET",
                cache: false,
                data: {
                    idColeccion: idColeccionA4,
                    idCompPrincipal: idComponentePrincipal
                },
                success: function (data) {

                    var badge = $('<span>').addClass('badge');

                    badge.text(data + " " + (data != 1 ? "Objetos" : "Objeto"));

                    $("#lblColeccionCompuesta").html(badge);
                },
                error: function (error) {
                    console.debug(error);
                }
            });
        } else {
            $("#lblColeccionCompuesta").empty();
        }
    });
}

function TildarManzanasDuplicadas() {
    //SI LA COLECCION TIENE SOLO PARCELAS,SE HABILITA CHECK MANZANA DUPLICADA
    //DEFAULT ES CHECK DESTILDADO
    if ($("#radioColeccion").is(":checked")) {
        //ManzanasDuplicadas TILDADA
        if ($("#manzanasDupliacadas").is(':checked')) {
            //HABILITAR
            //ORIGEN            
            //$("#lblOrigen").removeClass("disabled");
            //$("#ordOrigen").removeAttr("disabled");

            //EXPEDIENTE
            $("#lblExpte").removeClass("disabled");
            $("#ordExpte").removeAttr("disabled");

            //MANZANA
            $("#lblManzana").removeClass("disabled");
            $("#ordManzana").removeAttr("disabled");
        }
        else {

            //ManzanasDuplicadas DESTILDADA
            //DESELECCIONAR*****************************
            //ORIGEN
            $("#ordOrigen").attr('checked', false);
            //EXPEDIENTE
            $("#ordExpte").attr('checked', false);

            //DESHABILITAR
            //ORIGEN
            $("#lblOrigen").addClass("disabled");
            $("#ordOrigen").attr("disabled", "disabled");
            //EXPEDIENTE
            $("#lblExpte").addClass("disabled");
            $("#ordExpte").attr("disabled", "disabled");

            //HABILITAR*********************************
            //MANZANA
            $("#lblManzana").removeAttr("disabled");
            $('#ordManzana').removeAttr("disabled", "disabled");

            //SELECCIONAR
            //MANZANA
            $('#ordManzana').attr('checked', true);
        }
    }


    if ($("#radioListExpte").is(":checked")) {

        if ($("#manzanasDupliacadas").is(':checked')) {
            //HABILITAR
            //ORIGEN            
            $("#lblOrigen").removeClass("disabled");
            $("#ordOrigen").removeAttr("disabled");

            //EXPEDIENTE
            $("#lblExpte").removeClass("disabled");
            $("#ordExpte").removeAttr("disabled");

        }
        else {
            //DESELECCIONAR*****************************
            //ORIGEN
            $("#ordOrigen").attr('checked', false);
            //EXPEDIENTE
            $("#ordExpte").attr('checked', false);

            //DESHABILITAR
            //ORIGEN
            $("#lblOrigen").addClass("disabled");
            $("#ordOrigen").attr("disabled", "disabled");
            //EXPEDIENTE
            $("#lblExpte").addClass("disabled");
            $("#ordExpte").attr("disabled", "disabled");

            //HABILITAR*********************************
            //MANZANA
            $("#lblManzana").removeAttr("disabled");
            $('#ordManzana').removeAttr("disabled", "disabled");

            //SELECCIONAR
            //MANZANA
            $('#ordManzana').attr('checked', true);
        }
    }
}

function HabilitarLisBtnGenerar() {
    //SI NO HAY ARCHIVO SELECCIONADO, DESHABILITAR BOTON GENERAR
    $("#btnGenerar").attr('disabled', false);
}

//@ sourceURL=ploteo.js