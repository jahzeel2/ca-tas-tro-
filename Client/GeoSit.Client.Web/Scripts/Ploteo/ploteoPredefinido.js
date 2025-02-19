function PloteoPredefinidoController() {
    const events = {
        GENERAR_DISABLED: "generar-disabled",
        GENERAR_ENABLED: "generar-enabled",
        MOSTRAR_ERROR: "mostrar-error"
    }
    let subscriptions = {}, sliderInput, sliderCtrl, chkImagen, cboImagen,
        idPlantilla, btnBuscar, txtFiltro, chkEliminarTodos, chkAgregarTodos,
        lstResultados, lstAgregados, idComponentePrincipal;

    function init(form) {
        initCommon(form);
        initTransparencia(form);
        initResumen(form);
        notify(events.GENERAR_DISABLED);
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
        btnBuscar = $("#buscarComponente", form);
        txtFiltro = $('#filtro', form);
        chkEliminarTodos = $("#checkGralEliminar", form);
        chkAgregarTodos = $("#checkGralAgregar", form);
        lstResultados = $("#listCheckAgregar", form);
        lstAgregados = $("#agregados", form);

        btnBuscar.click(search.bind(null, form));
        txtFiltro.keypress((evt) => {
            evt.keyCode === 13 && search(form);
        });

        chkEliminarTodos.on("click", (evt) => $("input[type=checkbox]", lstAgregados).prop("checked", evt.target.checked));
        chkAgregarTodos.on("click", (evt) => $("input[type=checkbox]", lstResultados).prop("checked", evt.target.checked));

        $("#btn_Agregar", form).on("click", () => {
            let newSelection = [], currentSelection = [];

            $("input", lstAgregados).each((_, elem) => currentSelection.push(elem.id));

            $("input:checked", lstResultados).prop("checked", false).each(function (_, elem) {
                const id = elem.id,
                    text = $(`#span_${id}`, lstResultados).text();

                if (currentSelection.includes(id)) return;

                const selected = `<div class="agregado col-xs-12">\
                                <label class="cursor-pointer">\
                                    <input style=" margin-top: 2px" class="faChkSqr" id="${id}" type="checkbox" onchange="cleanCheckGralAgregados();">\
                                        <span id="span_${id}">${text}</span>\
                                </label>\
                            </div>`;
                newSelection = [...newSelection, selected];
            });
            lstAgregados.append(newSelection.join(""));
            chkAgregarTodos.prop("checked", false);
            checkValidPlot();
        });

        $("#btn_Eliminar", form).on("click", () => {
            $("input:checked", lstAgregados).parents("div.agregado").remove();
            chkEliminarTodos.prop("checked", false);
            checkValidPlot();
        });

        $("#cboComponentes", form).on("change", (evt) => {
            idComponentePrincipal = Number(evt.target.value);
            if (idComponentePrincipal !== 0) {
                txtFiltro.removeAttr("readonly");
                btnBuscar.css("pointer-events", "auto");
            } else {
                txtFiltro.attr("readonly", "readonly");
                btnBuscar.css("pointer-events", "none");
            }
            txtFiltro.val("");
            lstResultados.empty();
            lstAgregados.empty();
        });

        $(".select2", form).select2();
    }

    function checkValidPlot(form) {
        let notification = events.GENERAR_DISABLED;
        if ($("input", $("#agregados", form)).length) {
            notification = events.GENERAR_ENABLED;
        }
        notify(notification);
    }

    function search(form) {
        const lstResultados = $("#listCheckAgregar", form).empty();
        const idComponente = Number($("#cboComponentes", form).val()),
            tipo = $("#cboComponentes option:selected", form).attr("data-doctype"),
            filtro = $("#filtro", form).val();

        idComponente !== 0 && $.post(`${BASE_URL}Search/ByType`, { tipo, filtro, idComponente })
            .done(data => {
                const options = $.parseJSON(data).response.docs.reduce((options, doc) => {
                    return `${options}<div class="col-xs-12">\
                                            <label class="cursor-pointer">\
                                                <input style=" margin-top: 2px" class="faChkSqr" id="${doc.id}" type="checkbox" >\
                                                    <span id="span_${doc.id}">${doc.nombre}</span>\
                                            </label>\
                                        </div>`;
                }, "");
                lstResultados.html(options);
            })
            .fail((error) => notify(events.MOSTRAR_ERROR, { error: "Ha ocurrido un error al buscar la información" }));
    }

    async function generate() {
        return new Promise((ok, err) => {
            let selected = [];
            const idImagenSatelital = Number(cboImagen.val());

            $("input[type=checkbox]", lstAgregados).each((_, elem) => selected.push(Number(elem.id)));

            if (!idComponentePrincipal) {
                notify(events.MOSTRAR_ERROR, { error: "No se ha seleccionado un componente principal para plotear." });
                return ok(false);
            }
            if (!selected.length) {
                notify(events.MOSTRAR_ERROR, { error: "No se han seleccionado objetos para plotear." });
                return ok(false);
            }
            const data = {
                idComponentePrincipal,
                idsObjetoGraf: JSON.stringify(selected),
                idImagenSatelital,
                transparenciaPorc: Number(sliderCtrl.val())
            };

            $.ajax({
                url: `${BASE_URL}Ploteo/GenerarPloteoPredefinido/${idPlantilla}`,
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