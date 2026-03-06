var connectionNotificacionHub = new signalR.HubConnectionBuilder().withUrl((window.appBasePath || "") + "/NotificacionHub").build();

function borrarTodasLasNotificaciones() {
    $('#zonaNotificaciones .noti-card').remove();
    ocultarContenedorSiVacio();
}

function ocultarContenedorSiVacio() {
    if ($('#zonaNotificaciones .noti-card').length === 0) {
        $('#zonaNotificaciones').fadeOut();
    }
}


$(function () {
    $("#zonaNotificaciones").draggable({
        handle: ".noti-header",
        containment: "window"
    });
});



    connectionNotificacionHub.start().then(function () {
        console.log("Conexion Exitosa Notificaciones");
    });

    connectionNotificacionHub.on("ReceiveNotificacion", function (notificacion) {
        const fecha = new Date(notificacion.fecha).toLocaleString();
        const mensaje = notificacion.mensaje;
        const usuario = notificacion.usuario;
        const tipo = notificacion.tipoMensaje; // 1: info, 2: error, 3: warning
        const id = `noti_${Date.now()}`;

        let icono = "fas fa-info-circle";
        let clase = "info";

        if (tipo === 2) {
            icono = "fas fa-times-circle";
            clase = "danger";
        } else if (tipo === 3) {
            icono = "fas fa-exclamation-triangle";
            clase = "warning";
        }

        const html = `
                <div class="noti-card ${clase}" id="${id}">
                    <div class="noti-icon"><i class="${icono}"></i></div>
                    <div class="noti-content">
                        <strong>${usuario}</strong><br/>
                        ${mensaje}
                        <div class="noti-date">${fecha}</div>
                    </div>
                    <button class="noti-close" onclick="document.getElementById('${id}').remove(); ocultarContenedorSiVacio();">
                        <i class="fas fa-trash-alt"></i>
                    </button>
                </div>
            `;

        const $zona = $('#zonaNotificaciones');
        $zona.show();
        $zona.find('.noti-header').after(html);

        setTimeout(() => {
            $(`#${id}`).fadeOut(400, function () {
                $(this).remove();
                ocultarContenedorSiVacio();
            });
        }, 9000);
    })

