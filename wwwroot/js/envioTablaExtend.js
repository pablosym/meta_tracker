var tokenSet;
var estadoPendienteId = 0;
var estadoConErrorId = 0;

var appBasePath = window.appBasePath || "";

function EnvioTablaInit(token, estadoPendiente, estadoConError) {
    tokenSet = token;
    estadoPendienteId = estadoPendiente;
    estadoConErrorId = estadoConError;
}


$(document).ready(function () {

    $('#tabla').on('click', 'td.expand', function () {

        var tr = $(this).closest('tr');
        $(this)[0].innerHTML = '<i class=\"fas fa-minus-square text-red\"></i>';
        //
        // Expando la tabla de Articulos si presionan en Guias
        //
        if ($(this).hasClass('Guia')) {

            var row = $(tr).closest('table').DataTable().row(tr);

            if (row.child.isShown()) {
                // This row is already open - close it
                row.child.hide();
                tr.removeClass('shown');
                $(this)[0].innerHTML = '<i class=\"fas fa-plus-square text-green\"></i>';
            }
            else {

                verTablaArticulos(tr, row);
            }
        }
        //
        // Expando la tabla de Guias si presionan en Envios
        //
        else {

            var row = $("#tabla").DataTable().row(tr);

            if (row.child.isShown()) {
                // This row is already open - close it
                row.child.hide();
                tr.removeClass('shown');
                $(this)[0].innerHTML = '<i class=\"fas fa-plus-square text-green\"></i>';
            }
            else {

                verTablaGuias(tr, row);

            }
        }
    });

    function verTablaGuias(tr, row) {
        var data = $("#tabla").DataTable().row(tr).data();
        var tablaId = `tablaGuias_${data.numero}`;

        var strTablaGuia = `<div class="row">
                            <div class="col-md-12"><p class="lead"> Guias Disponibles </p>
                                <div class="col-md-12 col-xs-12">
                                    <div class="card card-gray card-outline shadow">
                                        <div class="card-body table-responsive p-0">
                                            <table class="table table-hover table-sm text-nowrap w-100" id="${tablaId}"> </table>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>`;

        row.child(strTablaGuia).show();
        tr.addClass('shown');

        createTableGuias(data.numero, `#${tablaId}`);
    }


    function createTableGuias(numero) {
        const selectorTabla = `#tablaGuias_${numero}`;

        $(selectorTabla).DataTable({
            destroy: true,
            processing: true,
            serverSide: true,
            deferRender: true,
            responsive: true,
            paging: true,
            lengthChange: true,
            searching: false,
            ordering: false,
            info: true,
            lengthMenu: [10, 25, 50, 100, 200, 500, 1000],
            dom: 'Blfrtip',
            buttons: [
                {
                    extend: 'collection',
                    autoClose: 'true',
                    text: '',
                    tag: 'span',
                    buttons: [
                        { extend: 'excel', text: 'Excel', filename: 'Export_' + '@Html.Raw(ViewData["Title"])' },
                        { extend: 'pdf', text: 'PDF', filename: 'Export_' + "'" + '@Html.Raw(ViewData["Title"])' },
                        { extend: 'colvis', text: 'Columnas Ver / Ocultar' }
                    ]
                }
            ],

            ajax: {
                url: encodeURI(appBasePath + "/Envios/GetGuiasIndex"),
                type: "POST",
                datatype: "json",
                headers: { 'X-CSRF-TOKEN-HEADERNAME': tokenSet.requestToken },
                data: function (d) {


                    var filtro = [
                        { key: 'numero', value: numero }

                    ];

                    d.param = JSON.stringify(filtro);
                },
                dataSrc: function (resp) {
                    return resp.data;
                },

                error: function handleAjaxError(xhr, textStatus, error) {

                    $(".dataTables_processing").hide();
                }
            },

            columns: [
                { data: null, title: "", className: 'expand Guia', orderable: false, defaultContent: '<i class="fas fa-plus-square text-green"></i>', width: 2 },
                { data: "numero", title: "Numero", name: "numero", defaultContent: "" },
                {
                    data: "fecha", title: "Fecha", name: "fecha", defaultContent: "", render: function (data, row) {
                        return moment(data).format('DD/MM/YYYY HH:mm');
                    }
                },
                {
                    data: null, title: "Cliente", name: "cliente", defaultContent: "",
                    render: function (data, row) {
                        return '<b>' + data.clienteCodigo + '</b> ' + data.clienteNombre;
                    }
                },
                { data: "clienteDireccion", title: "Direccion", name: "direccion", defaultContent: "" },
                { data: "cantidadComprobantes", title: "Cantidad Comprobantes", name: "cantidadComprobantes", defaultContent: "0" },
                { data: "estado", title: "Estado", name: "estado", defaultContent: "" },
                {
                    data: null, width: 20, orderable: false, defaultContent: "",
                    render: function (data, type, row) {
                        if (data.envioId !== null && (data.estadoId === estadoPendienteId || data.estadoId === estadoConErrorId)) {
                            return '<a href="#" class="js-sync" data-id="' + data.envioId + '" data-guia="' + data.numero + '" title="Sincronizar"><i class="fas fa-satellite text-warning"></i></a>';
                        }
                        return "";
                    }
                }

            ],

            createdRow: function (row, data) {
                if (data.estadoColor !== '') {
                    $(row.cells).each(function () {
                        var color = "'" + data.estadoColor + "'";
                        $(this).css("color", color);
                    });
                }
            },
            language: {
                url: encodeURI(appBasePath + "/plugins/datatables/idioma.json"),
            }
        })
    }

    function verTablaArticulos(tr, row) {
        var data = $(tr).closest('table').DataTable().row(tr).data();
        var tablaId = `tablaArticulos_${data.numero}`;

        var strTablaArticulo = `<div class="row">
                                <div class="col-md-12"><p class="lead"> Articulos Disponibles </p>
                                    <div class="col-md-12 col-xs-12">
                                        <div class="card card-gray card-outline shadow">
                                            <div class="card-body table-responsive p-0">
                                                <table class="table table-hover table-sm text-nowrap w-100" id="${tablaId}"> </table>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>`;

        row.child(strTablaArticulo).show();
        tr.addClass('shown');

        createTableArticulos(data.numero);
    }


    function createTableArticulos(numero) {

        const selectorTabla = `#tablaArticulos_${numero}`;

        $(selectorTabla).DataTable({
            destroy: true,
            processing: true,
            serverSide: true,
            deferRender: true,
            responsive: true,
            paging: true,
            lengthChange: true,
            searching: false,
            ordering: false,
            info: true,
            lengthMenu: [10, 25, 50, 100, 200, 500, 1000],
            dom: 'Blfrtip',
            buttons: [
                {
                    extend: 'collection',
                    autoClose: 'true',
                    text: '',
                    tag: 'span',
                    buttons: [
                        { extend: 'excel', text: 'Excel', filename: 'Export_' + '@Html.Raw(ViewData["Title"])' },
                        { extend: 'pdf', text: 'PDF', filename: 'Export_' + "'" + '@Html.Raw(ViewData["Title"])' },
                        { extend: 'colvis', text: 'Columnas Ver / Ocultar' }
                    ]
                }
            ],

            ajax: {
                url: encodeURI(appBasePath + "/Envios/GetArticulosIndex"),
                type: "POST",
                datatype: "json",
                headers: { 'X-CSRF-TOKEN-HEADERNAME': tokenSet.requestToken },
                data: function (d) {


                    var filtro = [
                        { key: 'numero', value: numero }

                    ];

                    d.param = JSON.stringify(filtro);
                },
                dataSrc: function (resp) {
                    return resp.data;
                },

                error: function handleAjaxError(xhr, textStatus, error) {

                    $(".dataTables_processing").hide();
                }
            },

            columns: [

                { data: "numeroComprobante", title: "Numero", name: "numero", defaultContent: "" },
                { data: "cabeceraComprobantesAfiliado", title: "Comprobante Afiliado", name: "cabeceraComprobantesAfiliado", defaultContent: "" },

                { data: "nroReceta", title: "Receta", name: "nroReceta", defaultContent: "" },
                { data: "listaPrecio", title: "Convenio", name: "listaPrecio", defaultContent: "" },
                {
                    data: null, title: "Articulo", name: "articulo", defaultContent: "",
                    render: function (data, row) {
                        return '<b>' + data.articuloCodigo + '</b> ' + data.articuloDescripcion;
                    }
                },

                {
                    data: null,
                    title: "Teléfono",
                    render: function (data, type, row) {

                        if (!row.telefono)
                            return "";

                        let badgeClass = "badge-secondary";

                        switch ((row.telefonoOrigen || "").toLowerCase()) {

                            case "domicili":
                                badgeClass = "badge-success";
                                break;

                            case "afiliado":
                                badgeClass = "badge-info";
                                break;

                            case "afiliado_multiples_tel":
                                badgeClass = "badge-warning";
                                break;
                        }

                        return `<i class="fas fa-phone-alt text-primary mr-1"></i>
                ${row.telefono}
                <span class="badge ${badgeClass} ml-2">${row.telefonoOrigen}</span>`;
                    }
                },

                { data: "cantidadSolicitada", title: "Cantidad", name: "cantidadSolicitada", defaultContent: "0" }
            ],

            createdRow: function (row, data) {
                if (data.estadoColor !== '') {
                    $(row.cells).each(function () {
                        var color = "'" + data.estadoColor + "'";
                        $(this).css("color", color);
                    });
                }
            },
            language: {
                url: encodeURI(appBasePath + "/plugins/datatables/idioma.json"),
            }
        })
    }


    $(document).on('click', '.js-sync', function (e) {
        e.preventDefault();
        var $a = $(this);
        var envioId = $a.data('id');
        var guiaNumero = $a.data('guia');

        $.ajax({
            url: encodeURI(appBasePath + "/Envios/SincronizarConGuia"),
            type: "POST",
            datatype: "json",
            headers: { 'X-CSRF-TOKEN-HEADERNAME': tokenSet.requestToken },
            data: {
                EnvioId: envioId,
                GuiaNumero: guiaNumero
            },
            success: function (resp) {

                viewMessage(resp);

            },
            dataSrc: function (resp) {
                return resp.data;
            },
            error: function handleAjaxError(xhr, textStatus, error) {
                $(".dataTables_processing").hide();
                toastr.error('Error de red al sincronizar.');
            }
        });
    });

});