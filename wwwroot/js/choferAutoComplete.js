var tokenSet;
var appBasePath = window.appBasePath || "";

function ChoferesInit(token ) {
    tokenSet = token;
}

function resetChofer() {
    $('#ChoferDNI').val('');
    $('#Chofer').val('');
    $('#ChoferId').val('');
}

$(document).ready(function () {

    $("#Chofer").change(function () {
        if ($("#Chofer").val() === '')
            resetChofer();
    });

    $("#Chofer").autocomplete({

        select: function (event, ui) {
            setChofer(ui);
        },

        delay: 150,
        minLength: 3,
        source: function (request, response) {
            $.ajax({
                type: "POST",
                url: encodeURI(appBasePath + "/Choferes/AutoComplete"),
                dataType: "json",
                headers: { 'X-CSRF-TOKEN-HEADERNAME': tokenSet.requestToken },
                data: { 'term': $("#Chofer").val() },
                success: function (data) {

                    mapChofer(data, response);

                },
                error: function (e) {
                    console.log(e);
                }
            });
        }
    });

    $("#ChoferDNI").change(function () {
        if ($("#ChoferDNI").val() === '')
            resetChofer();
    });

    $("#ChoferDNI").autocomplete({
        select: function (event, ui) {

            setChofer(ui);

        },
        delay: 500,
        minLength: 3,
        source: function (request, response) {
            $.ajax({
                type: "POST",
                url: encodeURI(appBasePath + "/Choferes/AutoCompleteByDNI"),
                dataType: "json",
                headers: { 'X-CSRF-TOKEN-HEADERNAME': tokenSet.requestToken },
                data: { 'term': $("#ChoferDNI").val() },
                success: function (data) {

                    mapChofer(data, response);

                },
                error: function (e) {
                    console.log(e);
                }
            });
        }
    });



    function setChofer(ui) {

        var chofer = JSON.parse(ui.item.tagObj);

        $('#ChoferDNI').val(chofer.Dni);
        $('#Chofer').val(chofer.ApellidoNombre);
        $('#ChoferId').val(chofer.Id);
    }

    function mapChofer(data, response) {
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