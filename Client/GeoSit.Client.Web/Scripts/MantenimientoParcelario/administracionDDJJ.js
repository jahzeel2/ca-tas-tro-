var AdministracionDDJJ = function () {
    function init(moduleContainerElem, controller) {
        $('#btnDDJJ', moduleContainerElem).oneClick(function () {
            if (controller.isUTValida()) {
                showLoading();
                $.ajax({
                    url: `${BASE_URL}valuaciones/administrador/${controller.obtenerUT().UnidadTributariaId}`,
                    method: "GET",
                    dataType: "html",
                    success: function (html) {
                        controller.cargarHTML(html);
                    },
                    error: function () {
                        controller.mostrarError("Mantenedor Parcelario - Error", "Se ha producido un error al cargar el Formulario de Valuaciones.");
                    }
                });
            }
        });
        moduleContainerElem.addEventListener("ut-changed", function (evt) {
            if (controller.isUTValida()) {
                $("#btnDDJJ", moduleContainerElem).removeClass("disabled");
            } else {
                $("#btnDDJJ", moduleContainerElem).addClass("disabled");
            }
        });
    }

    return {
        init: init
    };
};