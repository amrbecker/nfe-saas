// Ori — ponte JS da assistente (docs/assistente/CAPTURA_CONTEXTO.md). Sem script inline (CSP).
// Captura SIGNIFICADO, não DOM: o campo com foco (data-ajuda), atividade para o cochilo e o atalho de teclado.
// Valores de campos sensíveis nunca saem daqui.
(function () {
    let ref = null;
    let sensiveis = new Set();
    let ultimaTecla = 0;
    let ultimaAtividade = 0;

    function rotulo(el) {
        const controle = el.closest('.mud-input-control') || el.parentElement;
        const label = controle && controle.querySelector('label');
        return (label && label.textContent.trim()) || el.getAttribute('aria-label') || null;
    }

    function aoFocar(ev) {
        if (!ref) return;
        const alvo = ev.target;
        const marcado = alvo && alvo.closest && alvo.closest('[data-ajuda]');
        if (!marcado) return;
        const campo = marcado.getAttribute('data-ajuda');
        const itemAttr = marcado.closest('[data-ajuda-item]');
        const item = itemAttr ? parseInt(itemAttr.getAttribute('data-ajuda-item'), 10) : null;
        const input = alvo.matches('input, textarea, select') ? alvo : marcado.querySelector('input, textarea, select');
        const valor = sensiveis.has(campo) || !input || input.type === 'password' ? null : (input.value || '').slice(0, 60);
        ref.invokeMethodAsync('OriFoco', campo, rotulo(input || marcado), valor, isNaN(item) ? null : item);
    }

    function aoTeclar(ev) {
        ultimaTecla = Date.now();
        if (ev.ctrlKey && ev.shiftKey && ev.code === 'Space') {
            ev.preventDefault();
            ref && ref.invokeMethodAsync('OriAtalho');
        } else if (ev.key === 'Escape') {
            ref && ref.invokeMethodAsync('OriEscape');
        }
        atividade();
    }

    function atividade() {
        const agora = Date.now();
        if (ref && agora - ultimaAtividade > 10000) {
            ultimaAtividade = agora;
            ref.invokeMethodAsync('OriAtividade');
        }
    }

    window.ori = {
        iniciar: function (dotnetRef, camposSensiveis) {
            ref = dotnetRef;
            sensiveis = new Set(camposSensiveis || []);
            document.addEventListener('focusin', aoFocar, true);
            document.addEventListener('keydown', aoTeclar, true);
            document.addEventListener('pointerdown', atividade, { passive: true });
            document.addEventListener('pointermove', atividade, { passive: true });
            document.addEventListener('visibilitychange', function () {
                document.body.classList.toggle('ori-oculta', document.hidden);
            });
        },
        parar: function () {
            document.removeEventListener('focusin', aoFocar, true);
            document.removeEventListener('keydown', aoTeclar, true);
            ref = null;
        },
        // Chamadas síncronas (IJSInProcessRuntime) usadas pelas regras anti-intrusão dos balões.
        msDesdeUltimaTecla: function () { return ultimaTecla === 0 ? 999999 : Date.now() - ultimaTecla; },
        modalAberto: function () { return !!document.querySelector('.mud-dialog-container, .mud-overlay-dialog'); },
        focar: function (id) { const el = document.getElementById(id); if (el) el.focus(); },
        rolarFim: function (id) { const el = document.getElementById(id); if (el) el.scrollTop = el.scrollHeight; }
    };
})();
