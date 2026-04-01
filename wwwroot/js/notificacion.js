var connectionNotificacionHub = new signalR.HubConnectionBuilder().withUrl((window.appBasePath || "") + "/NotificacionHub").build();

const DURACION_NOTIFICACION_MS = 9000;

function escapeHtml(texto) {
    return $('<div>').text(texto ?? '').html();
}

function sanitizarHtmlPermitido(html) {
    const template = document.createElement('template');
    template.innerHTML = html ?? '';

    const etiquetasPermitidas = new Set(['B', 'STRONG', 'I', 'EM', 'U', 'BR', 'SPAN', 'SMALL']);
    const atributosPermitidos = {
        SPAN: new Set(['class']),
        SMALL: new Set(['class'])
    };

    const nodos = template.content.querySelectorAll('*');
    nodos.forEach((nodo) => {
        const tag = nodo.tagName;

        if (!etiquetasPermitidas.has(tag)) {
            const padre = nodo.parentNode;
            while (nodo.firstChild) {
                padre.insertBefore(nodo.firstChild, nodo);
            }
            padre.removeChild(nodo);
            return;
        }

        [...nodo.attributes].forEach((attr) => {
            const nombre = attr.name.toLowerCase();
            const valor = (attr.value || '').toLowerCase();
            const permitidosTag = atributosPermitidos[tag];
            const atributoPermitido = permitidosTag && permitidosTag.has(attr.name);

            if (nombre.startsWith('on') || valor.includes('javascript:') || !atributoPermitido) {
                nodo.removeAttribute(attr.name);
            }
        });
    });

    return template.innerHTML;
}

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



connectionNotificacionHub.start()
    .then(function () {
        console.log("Conexion Exitosa Notificaciones");
    })
    .catch(function (err) {
        console.error("Error al conectar con NotificacionHub:", err);
    });

connectionNotificacionHub.onclose(function (err) {
    if (err) {
        console.error("Conexion cerrada con error en NotificacionHub:", err);
    }
});

connectionNotificacionHub.on("ReceiveNotificacion", function (notificacion) {
    $(document).trigger('tracker:notificacion', [notificacion]);

    const fechaNotificacion = new Date(notificacion.fecha);
    const fecha = Number.isNaN(fechaNotificacion.getTime()) ? '' : fechaNotificacion.toLocaleString();
    const mensaje = sanitizarHtmlPermitido(notificacion.mensaje);
    const usuario = escapeHtml(notificacion.usuario);
    const tipo = Number(notificacion.tipoMensaje); // 1: info, 2: error, 3: warning
    const id = `noti_${Date.now()}_${Math.floor(Math.random() * 10000)}`;
    const esError = tipo === 2;

    let icono = "fas fa-info-circle";
    let clase = "info";

    if (esError) {
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

    if (!esError) {
        setTimeout(() => {
            $(`#${id}`).fadeOut(400, function () {
                $(this).remove();
                ocultarContenedorSiVacio();
            });
        }, DURACION_NOTIFICACION_MS);
    }
});
