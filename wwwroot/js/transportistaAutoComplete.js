var tokenSet;
var appBasePath = window.appBasePath || "";

function TransportistaAutoCompleteInit(token) {
    tokenSet = token;

}

function resetTransportista() {
    $('.TransportistaDestino').html('').val('');
    $('.TransportistaDestinoCodigo').html('').val('');
    $('.TransportistaDestinoDireccion').html('').val('');
}

$(document).ready(function () {

    $('.TransportistaDestino').change(function () {
        if ($('.TransportistaDestino').val() === '')
            resetTransportista();
    });

    $('.TransportistaDestino').autocomplete({
        select: function (event, ui) {
            setTransportista(ui);
        },

        delay: 150,
        minLength: 3,
        source: function (request, response) {
            $.ajax({
                type: "POST",
                url: encodeURI(appBasePath + "/Transportistas/AutoComplete"),
                dataType: "json",
                headers: { 'X-CSRF-TOKEN-HEADERNAME': tokenSet.requestToken },
                data: {
                    'term': $('#TransportistaDestino').val() },
                success: function (data) {

                    mapTransportista(data, response);

                },
                error: function (e) {
                    console.log(e);
                }
            });
        }
    });


    function setTransportista(ui) {

        var transportista = JSON.parse(ui.item.tagObj);

        $('.TransportistaDestino').html(transportista.ApellidoNombre).val(transportista.ApellidoNombre);
        $('.TransportistaDestinoCodigo').html(transportista.Codigo).val(transportista.Codigo);
        

        if (transportista.Direccion === undefined || transportista.Direccion === null) {
            $('.TransportistaDestinoDireccion').val('DIRECCION DEL TRANSPORTISTA NO INGRESADA, se toma la direccion de la guia').html('DIRECCION DEL TRANSPORTISTA NO INGRESADA, se toma la direccion de la guia');
        }
        else {
            $('.TransportistaDestinoDireccion').val(transportista.Direccion).html(transportista.Direccion);
        }
    }

    function mapTransportista(data, response) {
        var obj = data.d || data;

        response($.map(obj, function (item) {

            return {

                label: item.label,
                value: item.label,
                id: item.id,
                tagObj: item.tagObj
            };
        }));
    }
});