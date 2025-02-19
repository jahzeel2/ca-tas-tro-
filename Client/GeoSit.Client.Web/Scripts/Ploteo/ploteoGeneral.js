function PloteoGeneralController() {
    const events = {
        GENERAR_DISABLED: "generar-disabled",
        GENERAR_ENABLED: "generar-enabled",
        MOSTRAR_ERROR: "mostrar-error"
    }
    let subscriptions = {}, sliderInput, sliderCtrl, chkImagen, cboImagen,
        idPlantilla, cboColecciones, divColecciones, labelColeccionCompuesta,
        optColeccion, optVisualizacionActual, lstTextos, idComponentePrincipal;

    function init(form) {
        initCommon(form);
        initTransparencia(form);
        initResumen(form);
        notify(events.GENERAR_ENABLED);
    }

    function initResumen(form) {
        const previewCtrl = $("#pdf-preview", form);

        if (previewCtrl.length > 0) {
            const imagen = `${$("#file-name", form).val()}.png`;
            previewCtrl.fileinput("refresh", {
                showUpload: false,
                showClose: false,
                showRemove: false,
                showCaption: false,
                showPreview: true,
                initialPreview: [`<img src="CommonFiles/${imagen}" class="file-preview-image" width="160px" height="160px">`]
            });
            previewCtrl.fileinput("disable");
            previewCtrl.parent().addClass("hidden");
            $(".fileinput-remove", form).addClass("hidden");
        }
        $(".file", form).fileinput({
            showRemove: false,
            showUpload: false,
            showPreview: false,
            allowedFileExtensions: ["txt", "xlsx", "csv"]
        });
    }

    function initTransparencia(form) {
        sliderInput = $("#InputSlider", form);
        sliderCtrl = $("#slider", form);
        chkImagen = $("#chkHabilitarImagen", form);
        cboImagen = $("#cboImagenSatelitalA4", form);

        chkImagen
            .change((evt) => {
                cboImagen.enable(evt.target.checked);
                sliderInput.enable(evt.target.checked);
                sliderCtrl.enable(evt.target.checked);
            })
            .after("<label for='chkHabilitarImagen'></label>");

        cboImagen.enable(false);

        sliderCtrl
            .change((evt) => sliderInput.val(evt.target.value))
            .keypress((evt) => sliderInput.val(evt.target.value))
            .mousemove((evt) => sliderInput.val(evt.target.value))
            .val("50")
            .enable(false);

        sliderInput
            .keyup((evt) => debounce((evt) => sliderCtrl.val(evt.target.value), 250, evt))
            .inputmask('Regex', { regex: "^[1-9][0-9]$" })
            .val("50")
            .enable(false);
    }

    function initCommon(form) {
        idComponentePrincipal = Number($("#idComponentePrincipal", form).val());
        idPlantilla = Number($("#IdPlantilla", form).val());
        cboColecciones = $("#cboColeccionesGral", form);
        divColecciones = $("#divColeccionGral", form);
        labelColeccionCompuesta = $("#lblColeccionCompuesta", form);
        optColeccion = $("#radioColeccionGral", form);
        optVisualizacionActual = $("#radioVisualizacion", form);
        lstTextos = $("#listTextosVariables", form);

        $(".select2").select2();
        optColeccion.on("change", () => {
            divColecciones.show();
            cboColecciones.val("0").trigger("change");
            notify(events.GENERAR_DISABLED);
        });
        optVisualizacionActual.on("change", () => {
            divColecciones.hide();
            notify(events.GENERAR_ENABLED);
        });
        cboColecciones.on("change", (evt) => {
            const idColeccion = Number(evt.target.value);
            if (idColeccion === 0) {
                labelColeccionCompuesta.empty();
            } else {
                $.get(`${BASE_URL}Ploteo/ValidacionColeccion`, { idPlantilla, idColeccion })
                    .done(() => notify(events.GENERAR_ENABLED))
                    .fail((error) => {
                        const msg = error.status === 409
                            ? "La colección seleccionada contiene más de un componente principal de esta plantilla."
                            : "Ha ocurrido un error al validar el estado de la colección seleccionada.";
                        notify(events.GENERAR_DISABLED);
                        notify(events.MOSTRAR_ERROR, { error: msg });
                    });
            }
        });
    }

    async function generate() {
        return new Promise((ok, err) => {
            const idImagenSatelital = Number(cboImagen.val()),
                idColeccion = Number(cboColecciones.val());

            if (chkImagen.is(":checked") && idImagenSatelital === 0) {
                notify(events.MOSTRAR_ERROR, { error: "No ha seleccionado una Imagen Satelital." });
                return ok(false);
            }
            if (optColeccion.is(":checked") && idColeccion === 0) {
                notify(events.MOSTRAR_ERROR, { error: "No ha seleccionado una Colección." });
                return ok(false);
            }
            let textosVariables = [], action;
            $("label", lstTextos).each((_, elem) => {
                const text = $(`#${$(elem).attr("for")}`).val(),
                    field = $(elem).text();
                textosVariables = [...textosVariables, `${field},${text}`];
            });
            let data = {
                textosVariables: textosVariables.join(";"),
                idImagenSatelital, 
                transparenciaPorc: Number(sliderCtrl.val())
            };
            if (optVisualizacionActual.is(":checked")) {
                data = {
                    ...data, ...{
                        idPlantillaFondo: 0,
                        extent: GeoSIT.MapaController.obtenerExtent().join(),
                        scale: 1,
                        layersVisibles: GeoSIT.MapaController.obtenerCapasVisibles().join(),
                    }
                };
                action = "GenerarPloteoVistaActual";
            } else {
                data = {
                    ...data, ...{
                        idColeccion,
                        idComponentePrincipal
                    }
                };
                action = "GenerarPloteoGeneral";
            }

            $.ajax({
                url: `${BASE_URL}Ploteo/${action}/${idPlantilla}`,
                data: data,
                type: "POST",
                contentType: "application/x-www-form-urlencoded",
                success: () => { ok(true) },
                error: err,
            });
        });
    }

    function notify(notification, params) {
        for (let sub of subscriptions[notification]) {
            sub(params);
        }
    }
    function on(notification, fn) {
        const subscription = Object.assign(Object.defineProperty({}, notification, { value: [], writable: true }), subscriptions);
        subscriptions[notification] = subscription[notification].concat(fn);
    }

    return {
        init,
        generate,
        onGenerarDisabled: on.bind(null, events.GENERAR_DISABLED),
        onGenerarEnabled: on.bind(null, events.GENERAR_ENABLED),
        onMostrarError: on.bind(null, events.MOSTRAR_ERROR),
    };
}