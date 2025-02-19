var directivaParcelas = function ($window, $http) {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultParcelasTemplate.html',
        link: function (scope) {
            scope.allowMantenedor = $.inArray(seguridadResource.MantenedorParcelario, scope.permisos) > -1;
            scope.allowInformeParcelario = $.inArray(seguridadResource.InformeParcelario, scope.permisos) > -1;
            scope.parcela = scope.$parent.elem;
            let partes = [scope.parcela.nombre];
            if (scope.parcela.dato_nomenclatura) {
                partes = [...partes, scope.parcela.dato_nomenclatura];
            }
            scope.parcela.partida_nomenclatura = partes.join("-");
            scope.parcela.selected = false;
            scope.abrir = function (url, id) {
                loadView(`${BASE_URL}${url}/${id}`);
                hideSearchBlur();
            };
            scope.getInformeParcelario = function () {
                showLoading();
                var request = {
                    method: 'POST',
                    url: `${BASE_URL}MantenimientoParcelario/GetInformeParcelario/${scope.parcela.id}`,
                };
                $http(request)
                    .then(function () {
                        $window.open(`${BASE_URL}MantenimientoParcelario/abrir/`);
                    }, function (error) {
                        alert(error.statusText);
                    })
                    .then(hideLoading);
            };
            scope.getConstanciaNomenclaturaCatastral = function () {
                showLoading();
                var request = {
                    method: 'POST',
                    url: `${BASE_URL}MantenimientoParcelario/GetConstanciaNomenclaturaCatastral/${scope.parcela.id}`,
                };
                $http(request)
                    .then(function () {
                        $window.open(`${BASE_URL}MantenimientoParcelario/abrir/`);
                    }, function (error) {
                        alert(error.statusText);
                    })
                    .then(hideLoading);
            };
            scope.generarReporteMedidasLinderos = function () {
                showLoading();
                $http({
                    method: "POST",
                    url: `${BASE_URL}MantenimientoParcelario/GetInformeMedidasLinderos?idParcela=${scope.parcela.id}`,
                }).then(function () {
                    $window.open(BASE_URL + 'UnidadTributaria/AbrirReporte/', '_blank');
                }, function () {
                    alert("error");
                }).then(hideLoading);
            };
        }
    };
};
var directivaParcelasProvinciales = function ($window, $http) {
    return {
        restrict: 'A',
        templateUrl: `${BASE_URL}Templates/resultParcelasTemplate.html`,
        link: function (scope) {
            scope.allowInformeParcelario = $.inArray(seguridadResource.InformeParcelario, scope.permisos) > -1;
            scope.parcela = scope.$parent.elem;
            let partes = [scope.parcela.nombre];
            if (scope.parcela.dato_nomenclatura) {
                partes = [...partes, scope.parcela.dato_nomenclatura];
            }
            scope.parcela.partida_nomenclatura = partes.join("-");
            scope.parcela.selected = false;
            scope.getInformeParcelario = function () {
                showLoading();
                var request = {
                    method: 'POST',
                    url: `${BASE_URL}MantenimientoParcelario/GetInformeParcelarioProvincial`,
                    data: { id: Number(scope.parcela.id) }
                };
                $http(request)
                    .then(function () {
                        $window.open(`${BASE_URL}MantenimientoParcelario/abrir/`);
                    }, function (error) {
                        alert(error.statusText);
                    })
                    .then(hideLoading);
            };
        }
    };
};
appJS.directive('resultType', function ($compile, $http, $window) {
    return {
        scope: {
            pattern: "=",
            tipo: "=",
            permisos: "="
        },
        restrict: 'A',
        terminal: true,
        priority: 1000,
        link: function (scope, element) {
            /*agrego el atributo dinamicamente dependiendo del valor que este dado por el atributo result-type*/
            element.attr("result-" + scope.tipo.toLowerCase(), "");
            /*remuevo los atributos para evitar loop infinito (contemplo las 2 posibles nomenclaturas)*/
            element.removeAttr("result-type");
            element.removeAttr("data-result-type");
            scope.allowExportarExcel = $.inArray(seguridadResource.ExportarExcelBuscador, scope.permisos) > -1;
            scope.allowPlanoAprobado = false;
            scope.verEnMapa = function ($evt, elem, zoom) {
                $evt.stopImmediatePropagation();
                GeoSIT.MapaController.seleccionarObjetos([elem.featids], [elem.capa], zoom);
                hideSearchBlur();
            };
            scope.exportarObjetoExcel = function ($evt) {
                $evt.stopImmediatePropagation();
                scope.$emit('exportarExcel', this.$parent.elem);
            };
            scope.showAtributos = function () {
                scope.$emit('showAtributos');
                showLoading();
                var request = {
                    method: 'POST',
                    url: `${BASE_URL}DetalleObjeto/GetDetalleObjetoByDocType`,
                    params: { objetoId: scope.$parent.elem.id, docType: scope.$parent.elem.tipo }
                };
                $http(request)
                    .then(function (resp) { mostrarDetalleObjetoSearch(resp.data); }, function (error) {
                        console.log(error.status);
                        alert("El componente no está bien configurado");
                    })
                    .then(hideLoading);
            };
            scope.verDocumento = function ($evt) {
                $evt.stopImmediatePropagation();
                $window.open(`${BASE_URL}documento/download/${scope.$parent.elem.iddocumento}`);
            };
            scope.selectedChanged = function () {
                this.$parent.$parent.selected = false;
            };
            scope.chkboxid = scope.$parent.elem.tipo + scope.$parent.elem.id;
            scope.ploteable = true;
            scope.$parent.grupo.exportable = true;
            scope.tieneDescripcion = !!scope.$parent.elem.descripcion;
            scope.tieneGrafico = !!scope.$parent.elem.featids && !!scope.$parent.elem.featids.length;
            scope.tieneDocumento = !!scope.$parent.elem.iddocumento;
            scope.esDocumentoProvincial = false;
            $compile(element)(scope);
        }
    };
});
appJS.directive('resultUnidadestributarias', function ($window, $http) {
    return {
        restrict: 'A',
        templateUrl: `${BASE_URL}Templates/resultUnidadesTributariasTemplate.html`,
        link: function (scope) {
            scope.unidadTributaria = scope.$parent.elem;
            scope.allowMantenedor = $.inArray(seguridadResource.MantenedorParcelario, scope.permisos) > -1 && scope.unidadTributaria.idpadre;
            scope.allowReportePropiedad = $.inArray(seguridadResource.ReportePropiedad, scope.permisos) > -1;
            scope.allowDDJJ = Number(scope.unidadTributaria.dato_partida) !== 0 && $.inArray(seguridadResource.VisualizarDDJJ, scope.permisos) > -1 && scope.unidadTributaria.idpadre;
            scope.allowCertificadoValuatorio = $.inArray(seguridadResource.CertificadoValuatorio, scope.permisos) > -1;
            scope.allowInformeUnidadTributaria = $.inArray(seguridadResource.InformeUnidadTributaria, scope.permisos) > -1;
            scope.esTipoPH = Number(scope.unidadTributaria.dato_esTipoPH) === 1;
            scope.unidadTributaria.selected = false;
            scope.abrir = function (url, id) {
                loadView(`${BASE_URL}${url}/${id}?UnidadTributariaId=${scope.unidadTributaria.id}`);
                hideSearchBlur();
            };
            scope.generarReportePropiedad = function () {
                showLoading();
                $http({
                    method: "POST",
                    url: `${BASE_URL}MantenimientoParcelario/GetInformeParcelario?id=${scope.unidadTributaria.idpadre}`,
                }).then(function () {
                    $window.open(BASE_URL + 'UnidadTributaria/AbrirReporte/', '_blank');
                }, function () {
                    alert("error");
                }).then(hideLoading);
            };
            scope.generarReporteCambioTitularidad = function () {
                showLoading();
                $http({
                    method: "POST",
                    url: `${BASE_URL}MantenimientoParcelario/GetInformeCambioTitularidad?idUnidadTributaria=${scope.unidadTributaria.id}&idParcela=${scope.unidadTributaria.idpadre}`,
                }).then(function () {
                    $window.open(BASE_URL + 'UnidadTributaria/AbrirReporte/', '_blank');
                }, function () {
                    alert("error");
                }).then(hideLoading);
            };
            scope.getReporteUnidadTributaria = function () {
                showLoading();
                $http({
                    method: "POST",
                    url: `${BASE_URL}MantenimientoParcelario/GetInformeUTFromSearch`,
                    params: { idUnidadTributaria: scope.unidadTributaria.id, idParcela: scope.unidadTributaria.idpadre }
                }).then(function () {
                    $window.open(`${BASE_URL}MantenimientoParcelario/Abrir`);
                }, function () {
                    alert("error");
                }).then(hideLoading);
            };
            scope.getCertificadoValuatorio = function () {
                showLoading();
                $http({
                    url: `${BASE_URL}Valuaciones/CertificadoValuatorio`,
                    params: { id: scope.unidadTributaria.id },
                    method: "POST"
                }).then(function () {
                    $window.open(`${BASE_URL}Valuaciones/AbrirReporte`);
                }, function (err) {
                    switch (err.status) {
                        case 400:
                            alert("Se ha producido un error al generar el Certificado Valuatorio. La partida no posee datos valuatorios.");
                            break;
                        case 409:
                            alert("Se ha producido un error al generar el Certificado Valuatorio. La partida no posee dominio o su superficie es 0.");
                            break;
                        default:
                            alert("Ha ocurrido un error al generar el Certificado Valuatorio");
                    }
                }).then(hideLoading);
            };
            scope.abrirAdministradorValuaciones = function () {
                showLoading();
                loadView(`${BASE_URL}valuaciones/administrador/${scope.unidadTributaria.id}`);
                hideSearchBlur();
            };
        }
    };
});
appJS.directive('resultUnidadestributariasprovinciales', function ($window, $http) {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultUnidadesTributariasTemplate.html',
        link: function (scope) {
            scope.allowInformeUnidadTributaria = $.inArray(seguridadResource.InformeUnidadTributaria, scope.permisos) > -1;
            scope.esUTPH = scope.$parent.elem.dato_esUTPH;
            scope.unidadTributaria = scope.$parent.elem;
            scope.unidadTributaria.selected = false;
            scope.getReporteUnidadTributaria = function () {
                showLoading();
                $http({
                    method: "POST",
                    url: BASE_URL + "MantenimientoParcelario/GetInformeUTProvincialFromSearch",
                    data: { idUnidadTributaria: Number(scope.unidadTributaria.id), idParcela: Number(scope.unidadTributaria.idpadre), partida: scope.unidadTributaria.nombre }
                }).then(function () {
                    $window.open(BASE_URL + "MantenimientoParcelario/Abrir");
                }, function () {
                    alert("error");
                }).then(hideLoading);
            };
        }
    };
});
appJS.directive('resultUnidadestributariasbaja', function ($window, $http) {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultUnidadesTributariasHistoricasTemplate.html',
        link: function (scope) {
            scope.allowInformeUnidadTributaria = $.inArray(seguridadResource.InformeUnidadTributariaBaja, scope.permisos) > -1;
            scope.esUTPH = scope.$parent.elem.dato_esUTPH;
            scope.unidadTributaria = scope.$parent.elem;
            scope.unidadTributaria.selected = false;
            scope.getReporteUnidadTributaria = function () {
                showLoading();
                $http({
                    method: "POST",
                    url: `${BASE_URL}MantenimientoParcelario/GetInformeUTBaja`,
                    data: { idUnidadTributaria: Number(scope.unidadTributaria.id), idParcela: Number(scope.unidadTributaria.idpadre), partida: scope.unidadTributaria.nombre }
                }).then(function () {
                    $window.open(`${BASE_URL}MantenimientoParcelario/abrir`);
                }, function () {
                    alert("error");
                }).then(hideLoading);
            };
        }
    };
});
appJS.directive('resultDepartamentos', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
        }
    };
});
appJS.directive('resultJurisdicciones', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
        }
    };
});
appJS.directive('resultMunicipios', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
        }
    };
});
appJS.directive('resultLocalidades', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
            scope.allowExportarExcel = false;
        }
    };
});
appJS.directive('resultSecciones', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
        }
    };
});
appJS.directive('resultBarrios', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
        }
    };
});
appJS.directive('resultCentrosurbanos', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
        }
    };
});
appJS.directive('resultParajes', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
        }
    };
});
appJS.directive('resultAfluentes', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
        }
    };
});
appJS.directive('resultLagunas', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
        }
    };
});
appJS.directive('resultReservas', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
        }
    };
});
appJS.directive('resultRedgeo1', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
            scope.esDocumentoProvincial = true;
        }
    };
});
appJS.directive('resultRedgeo2', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
            scope.esDocumentoProvincial = true;
        }
    };
});
appJS.directive('resultRedgeo3', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
            scope.esDocumentoProvincial = true;
        }
    };
});
appJS.directive('resultRedgeo4', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
            scope.esDocumentoProvincial = true;
        }
    };
});

appJS.directive('resultParcelas', directivaParcelas);
appJS.directive('resultPrescripciones', directivaParcelas);
appJS.directive('resultParcelasmunicipales', directivaParcelas);
appJS.directive('resultParcelasproyectos', directivaParcelas);
appJS.directive('resultParcelasprovinciales', directivaParcelasProvinciales);
appJS.directive('resultParcelasbaja', function ($window, $http) {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultParcelasHistoricasTemplate.html',
        link: function (scope) {
            scope.allowInformeParcelario = $.inArray(seguridadResource.InformeParcelarioBaja, scope.permisos) > -1;
            scope.parcela = scope.$parent.elem;
            let partes = [scope.parcela.nombre];
            if (scope.parcela.dato_nomenclatura) {
                partes = [...partes, scope.parcela.dato_nomenclatura];
            }
            scope.parcela.partida_nomenclatura = partes.join("-");
            scope.parcela.selected = false;
            scope.getInformeParcelario = function () {
                showLoading();
                var request = {
                    method: 'POST',
                    url: `${BASE_URL}MantenimientoParcelario/GetInformeParcelarioBaja`,
                    data: { id: Number(scope.parcela.id), partida: scope.parcela.nombre }
                };
                $http(request)
                    .then(function () {
                        $window.open(`${BASE_URL}MantenimientoParcelario/abrir`);
                    }, function (error) {
                        alert(error.statusText);
                    })
                    .then(hideLoading);
            };

        }
    };
});

appJS.directive('resultManzanas', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
        }
    };
});
appJS.directive('resultManzanasprovinciales', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
        }
    };
});
appJS.directive('resultPersonas', function ($window, $http) {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultPersonasTemplate.html',
        link: function (scope) {
            scope.$parent.grupo.exportable = false;
            scope.ploteable = false;
            scope.allowPersona = $.inArray(seguridadResource.ABMPersonas, scope.permisos) > -1;
            scope.allowReportePersona = $.inArray(seguridadResource.ReportePersona, scope.permisos) > -1;
            scope.allowReporteBienesRegistrados = $.inArray(seguridadResource.ReporteBienesRegistrados, scope.permisos) > -1;
            scope.persona = scope.$parent.elem;
            scope.persona.selected = false;
            scope.abrir = function (url, id) {
                loadView(BASE_URL + url + id);
                hideSearchBlur();
            };
            scope.generarReportePersona = function () {
                showLoading();
                $http({
                    type: "POST",
                    url: BASE_URL + 'Persona/GenerarReportePersona/' + scope.persona.id
                }).then(function () {
                    $window.open(BASE_URL + 'Persona/AbrirReporte/', "_blank");
                }, function () {
                    alert("error");
                }).then(hideLoading);
            };
            scope.generarReporteBienesRegistrados = function () {
                showLoading();
                $http({
                    type: "POST",
                    url: BASE_URL + 'Persona/GenerarReporteBienesRegistrados/' + scope.persona.id
                }).then(function () {
                    $window.open(BASE_URL + 'Persona/AbrirReporte/', "_blank");
                }, function () {
                    alert("error");
                }).then(hideLoading);
            };
        }
    };
});
appJS.directive('resultDocumentos', function ($window) {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultDocumentosTemplate.html',
        link: function (scope) {
            scope.$parent.grupo.exportable = false;
            scope.ploteable = false;
            scope.allowDocumento = $.inArray(seguridadResource.ABMDocumentos, scope.permisos) > -1 && scope.$parent.elem.dato_tieneDocumento === "S";
            scope.documento = scope.$parent.elem;
            scope.documento.selected = false;
            scope.download = function () {
                $window.location = `${BASE_URL}Documento/Download/${scope.documento.id}`;
            }
        }
    };
});
appJS.directive('resultDomicilios', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultDomiciliosTemplate.html',
        link: function (scope) {
            scope.$parent.grupo.exportable = false;
            scope.ploteable = false;
            scope.allowDomicilio = $.inArray(seguridadResource.ABMDomicilios, scope.permisos) > -1;
            scope.domicilio = scope.$parent.elem;
            scope.domicilio.selected = false;
            scope.abrir = function (url, id) {
                loadView(BASE_URL + url + id);
                hideSearchBlur();
            };
        }
    };
});
appJS.directive('resultCuadras', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultCuadrasTemplate.html',
        link: function (scope) {
            scope.$parent.grupo.exportable = false;
            scope.ploteable = false;
            scope.cuadra = scope.$parent.elem;
            scope.cuadra.selected = false;
        }
    };
});
appJS.directive('resultTramites', function ($window, $http) {
    return {
        restrict: "A",
        templateUrl: `${BASE_URL}Templates/resultTramitesTemplate.html`,
        link: function (scope) {
            scope.$parent.grupo.exportable = false;
            scope.ploteable = false;
            scope.allowTramite = $.inArray(seguridadResource.ABMTramites, scope.permisos) > -1;
            scope.tramite = scope.$parent.elem;
            scope.tramite.selected = false;
            scope.abrirHojaRuta = function () {
                showLoading();
                $http({
                    type: "POST",
                    url: `${BASE_UR}MesaEntradas/GetInformeHojaDeRuta/${scope.tramite.id}`
                }).then(function () {
                    $window.open(`${BASE_URL}MesaEntradas/Download`, "_blank");
                }, function () {
                    alert("error");
                }).then(hideLoading);
            };
            scope.abrirBandeja = function () {
                loadView(`${BASE_URL}MesaEntradas`);
            };
        }
    };
});
appJS.directive('resultCalles', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
            scope.allowExportarExcel = false;
        }
    };
});
appJS.directive('resultMensuras', function ($window, $http) {
    return {
        restrict: 'A',
        templateUrl: `${BASE_URL}Templates/resultObjetoGraficoComunTemplate.html`,
        link: function (scope) {
            scope.allowPlanoAprobado = $.inArray(seguridadResource.InformePlanoAprobado, scope.permisos) > -1;
            scope.getReportePlanoAprobado = function () {
                showLoading();
                $http({
                    method: "POST",
                    url: `${BASE_URL}Mensura/GetInformePlanoAprobado`,
                    params: { id: scope.objeto.id }
                }).then(function () {
                    $window.open(`${BASE_URL}Mensura/abrir/`);
                }, function () {
                    alert("error");
                }).then(hideLoading);
            };
            scope.allowExportarExcel = $.inArray(seguridadResource.ExportarExcelBuscador, scope.permisos) > -1;
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
        }
    };
});
appJS.directive('resultPaf', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
        }
    };
});
appJS.directive('resultParcelassaneables', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
        }
    };
});
appJS.directive('resultMejoras', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
        }
    };
});
appJS.directive('resultMejorasprovinciales', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
        }
    };
});
appJS.directive('resultPcc', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
        }
    };
});
appJS.directive('resultRedplani', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
        }
    };
});

appJS.directive('resultInfraestructuras', function () {
    return {
        restrict: 'A',
        templateUrl: BASE_URL + 'Templates/resultObjetoGraficoComunTemplate.html',
        link: function (scope) {
            scope.objeto = scope.$parent.elem;
            scope.objeto.selected = false;
        }
    };
});
