// Download file helper (chamado via JS interop de páginas Blazor, ex. DownloadDanfe).
// Precisa ser um arquivo externo (script-src 'self', sem 'unsafe-inline' no CSP de produção) —
// um <script> inline em index.html é bloqueado pelo navegador e window.downloadFile nunca existe.
window.downloadFile = function (filename, contentType, content) {
    const link = document.createElement('a');
    link.download = filename;
    link.href = "data:" + contentType + ";base64," + content;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
