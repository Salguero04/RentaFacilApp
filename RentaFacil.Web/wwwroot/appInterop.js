// Descarga un archivo en el navegador a partir de su contenido en base64.
// Lo usa WebDispositivoServicio.GuardarArchivoAsync para "compartir" recibos PDF en web.
window.rentaFacilDescargarArchivo = (nombreArchivo, base64) => {
    const binario = atob(base64);
    const bytes = new Uint8Array(binario.length);
    for (let i = 0; i < binario.length; i++) {
        bytes[i] = binario.charCodeAt(i);
    }
    const blob = new Blob([bytes], { type: 'application/pdf' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = nombreArchivo;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};
