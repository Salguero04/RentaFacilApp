// Módulo compartido (RCL) para que el layout sepa si está en pantalla móvil o de escritorio.
// Se carga vía import("./_content/RentaFacil.UI/sidebar.js") tanto en MAUI como en Web.

let _mq = null;
let _handler = null;

export function init(dotNetRef) {
    _mq = window.matchMedia('(max-width: 991.98px)');
    _handler = () => dotNetRef.invokeMethodAsync('SetIsMobile', _mq.matches);
    // Notificación inicial + en cada cambio de tamaño/orientación.
    _handler();
    _mq.addEventListener('change', _handler);
}

export function dispose() {
    if (_mq && _handler) {
        _mq.removeEventListener('change', _handler);
    }
    _mq = null;
    _handler = null;
}
