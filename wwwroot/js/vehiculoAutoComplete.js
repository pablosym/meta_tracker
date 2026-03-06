var tokenSet;
var appBasePath = window.appBasePath || "";
function VehiculoInit(token ) {
    tokenSet = token;
}

function resetVehiculo() {
    $('#Vehiculo').val('');
    $('#VehiculoId').val('');
    $('#Patente').val('');
    $('#VehiculoTipo').val('');
    $('#VehiculoTipoId').val('');
}

$(document).ready(function () {

    $("#Patente").change(function () {
        if ($("#Patente").val() === '') {
           resetVehiculo();
        }

    });

    $("#Patente").autocomplete({
        select: function (event, ui) {
            setVehiculo(ui);
        },
        delay: 150,
        minLength: 2,
        source: function (request, response) {
            $.ajax({
                type: "POST",
                url: encodeURI(appBasePath + "/Vehiculos/AutoComplete"),
                dataType: "json",
                headers: { 'X-CSRF-TOKEN-HEADERNAME': tokenSet.requestToken },
                data: { 'term': $("#Patente").val() },
                success: function (data) {

                    mapVehiculo(data, response);
                },
                error: function (e) {
                    console.log(e);
                }
            });
        }
    });



    function setVehiculo(ui) {

        var vehiculo = JSON.parse(ui.item.tagObj);

        $('#VehiculoId').val(vehiculo.Id);
        $('#Patente').val(vehiculo.Patente);
        $('#Vehiculo').val(vehiculo.Descripcion);
        $('#VehiculoTipo').val(vehiculo.Tipo.Descripcion);
        $('#VehiculoTipoId').val(vehiculo.TipoId);

    }

    function mapVehiculo(data, response) {
        var obj = data.d || data;

        //Si no existe limpio los demas campos.
        if (obj.length === 0) {

            resetVehiculo();

            toastr.warning('No se encontró el vehículo.');
        }
        else {
            response($.map(obj, function (item) {

                return {
                    label: item.label,
                    value: item.label,
                    id: item.id,
                    tagObj: item.tagObj
                };
            }));
        }
    }
});