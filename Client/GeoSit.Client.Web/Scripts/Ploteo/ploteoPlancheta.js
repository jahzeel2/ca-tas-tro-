function PloteoPlanchetaController() {
    const events = {
        GENERAR_DISABLED: "generar-disabled",
        GENERAR_ENABLED: "generar-enabled",
        MOSTRAR_ERROR: "mostrar-error"
    }
    let subscriptions = {}, sliderInput, sliderCtrl, chkImagen, cboImagen,
        idPlantilla, cboColecciones, divColecciones, labelColeccionCompuesta,
        optColeccion, optZonaResultado, idComponentePrincipal, searchSelection;

    function init(form) {
        initCommon(form);
        initTransparencia(form);
        initResumen(form);
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

    async function initCommon(form) {
        idComponentePrincipal = Number($("#idComponentePrincipal", form).val());
        idPlantilla = Number($("#IdPlantilla", form).val());
        cboColecciones = $("#cboColeccionesPlancheta", form);
        divColecciones = $("#divColeccionPlancheta", form);
        labelColeccionCompuesta = $("#lblColeccionCompuesta", form);
        optColeccion = $("#optColeccion", form);
        optZonaResultado = $("#optZonaResultado", form);

        $(".select2").select2();

        const componentesPloteables = [{ DocType: "manzanas", Nombre: "Manzanas" }, { DocType: "parcelas", Nombre: "Parcelas" }];

        let hasSelectionBySearch = false;
        searchSelection = componentesPloteables.reduce((accum, cmp) => {
            let selected = [];
            $(`[data-componente="${cmp.DocType}"][data-componente-ploteable="true"]:checked`, ".results").each((_, elem) => {
                selected.push(Number($(elem).data("name")));
            });
            accum[cmp.DocType] = { Nombre: cmp.Nombre, Cantidad: selected.length, Objetos: selected };
            hasSelectionBySearch |= selected.length;
            return accum;
        }, {});
        enableBySearchSelection(hasSelectionBySearch);

        optColeccion.on("change", () => {
            divColecciones.show();
            cboColecciones.val("0").trigger("change");
            notify(events.GENERAR_DISABLED);
        });
        optZonaResultado.on("change", () => {
            labelColeccionCompuesta.empty();
            divColecciones.hide();
            enableBySearchSelection(hasSelectionBySearch);
        });
        cboColecciones.on("change", (evt) => {
            const idColeccion = Number(evt.target.value);

            if (idColeccion === 0) {
                labelColeccionCompuesta.empty();
            } else {
                $.get(`${BASE_URL}Ploteo/ComposicionColeccion`, { idColeccion })
                    .done((data) => {
                        generateBadge(data, labelColeccionCompuesta);
                        notify(events.GENERAR_ENABLED);
                    })
                    .fail((error) => {
                        const msg = error.status === 409
                            ? "La colección seleccionada contiene más de un componente principal de esta plantilla."
                            : "Ha ocurrido un error al validar el estado de la colección seleccionada.";
                        notify(events.GENERAR_DISABLED);
                        notify(events.MOSTRAR_ERROR, { error: msg });
                    });
            }
        });
        generateBadge(searchSelection, $("#lblObjetosSeleccionados", form));
    }
    function enableBySearchSelection(enable) {
        if (enable) {
            notify(events.GENERAR_ENABLED);
        } else {
            notify(events.GENERAR_DISABLED);
        }
    }

    function generateBadge(objs, lbl) {

        const data = Object.keys(objs).reduce((accum, key) => {
            const totalGrupo = objs[key].Cantidad;
            if (totalGrupo === 0) return accum;
            accum.tooltips.push(`${totalGrupo} ${objs[key].Nombre}`);
            accum.total += totalGrupo;
            return accum;
        }, { tooltips: [], total: 0 });

        lbl.html(`${data.total} Resultado${data.total !== 1 ? "s" : ""}`);
        if (data.total > 0) {
            lbl.tooltip({
                title: data.tooltips.join(", "),
                container: 'body'
            });
        }
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
            let data = {
                ordenarPorManzana: true,
                ordenarPorExpediente: false,
                grabarListado: false,
                isManzanaDuplicadaCheck: true,
                verCotas: false,
                idImagenSatelital,
                transparenciaPorc: Number(sliderCtrl.val())
            };
            if (optZonaResultado.is(":checked")) {
                const selection = Object.keys(searchSelection).flatMap(key => searchSelection[key].Objetos.reduce((accum, id) => [...accum, { Id: id, Componente: key }], []));
                data = {
                    ...data, ...{
                        idsObjetoGraf: JSON.stringify(selection),
                    }
                };
                action = "GenerarPloteoPlanchetaA4ByManzanaZonaResultado";
            } else {
                data = {
                    ...data, ...{
                        idColeccion,
                    }
                };
                action = "GenerarPloteoPlanchetaA4ByColeccion";
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