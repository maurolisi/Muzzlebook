// modali bootstrap senza  jQuery.js e bootbox.js-
const box = (() => {
    // Variabile per mantenere l'istanza del loader
    let loaderInstance = null;

    const loader = (action = 'on',closeDelay=500) => {
        const loaderId = 'box-loader-modal';
        let el = document.getElementById(loaderId);

        if (action === 'off') {
            setTimeout(() => {
                if (loaderInstance) {
                    loaderInstance.hide();
                    loaderInstance = null;
                }
                if (el) el.remove();
            },closeDelay);
            return;
        }

        // Se già aperto, non ricrearlo per evitare sovrapposizioni[cite: 1]
        if (el || loaderInstance) return;

        el = document.createElement('div');
        el.id = loaderId;
        el.className = 'modal fade';
        // Z-index elevato per garantire che sia in primo piano rispetto ad altre modali eventualmente sottostanti
        el.style.zIndex = '1065'; 

        // Crea una modale centrata con sfondo invisibile, lasciando in risalto solo lo spinner
        el.innerHTML = `
            <div class="modal-dialog modal-dialog-centered" style="justify-content: center; pointer-events: none;">
                <div class="modal-content" style="background: transparent; border: none; box-shadow: none;">
                    <div class="modal-body text-center">
                        <div class="spinner-border text-primary" style="width: 4rem; height: 4rem; border-width: 0.35em;" role="status">
                            <span class="visually-hidden">Caricamento...</span>
                        </div>
                    </div>
                </div>
            </div>`;

        document.body.appendChild(el);

        // backdrop: 'static' e keyboard: false impediscono all'utente di chiuderlo inavvertitamente[cite: 1]
        loaderInstance = new bootstrap.Modal(el, { backdrop: 'static', keyboard: false });

        // Cleanup automatico alla chiusura per rimuoverlo dal DOM[cite: 1]
        el.addEventListener('hidden.bs.modal', () => {
            el.remove();
            loaderInstance = null;
        }, { once: true });

        loaderInstance.show();
    };

    const alert = (msg, title = '', className = '') => {
        const el = document.createElement('div');
        el.className = 'modal fade';

        const header = title
            ? `<div class="modal-header border-0">
                <h5 class="modal-title">${title}</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>`
            : '';

        const contentClass = className ? `modal-dialog ${className}` : 'modal-dialog';

        el.innerHTML = `
            <div class="${contentClass}">
                <div class="modal-content">
                    ${header}
                    <div class="modal-body">${msg}</div>
                        <div class="modal-footer border-0">
                        <button style="padding:3px;padding-left: 1em;padding-right: 1em;" type="button" class="btn btn-primary btn-sm" data-bs-dismiss="modal">Ok</button>
                    </div>
                </div>
            </div>`;

        document.body.appendChild(el);
        const m = new bootstrap.Modal(el);
        el.addEventListener('hidden.bs.modal', () => el.remove(), { once: true });
        m.show();
        return m;
    };


    const dialog = (html, title = '', className = '') => {
        const el = document.createElement('div');
        el.className = 'modal fade';

        const header = title
            ? `<div class="modal-header border-0">
            <h5 class="modal-title">${title}</h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
        </div>`
            : '';

        const contentClass = className ? `modal-dialog ${className}` : 'modal-dialog';

        el.innerHTML = `
        <div class="${contentClass}">
            <div class="modal-content">
                ${header}
                <div class="modal-body">${html}</div>
            </div>
        </div>`;

        document.body.appendChild(el);

        // Inietta il token antiforgery in ogni form dentro la modal
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (token) {
            el.querySelectorAll('form').forEach(form => {
                if (!form.querySelector('input[name="__RequestVerificationToken"]')) {
                    const input = document.createElement('input');
                    input.type = 'hidden';
                    input.name = '__RequestVerificationToken';
                    input.value = token;
                    form.appendChild(input);
                }
            });
        }

        // Esegue gli script iniettati
        el.querySelectorAll('script').forEach(oldScript => {
            const newScript = document.createElement('script');
            oldScript.getAttributeNames().forEach(attr =>
                newScript.setAttribute(attr, oldScript.getAttribute(attr))
            );
            newScript.textContent = oldScript.textContent;
            oldScript.replaceWith(newScript);
        });

        // Inizializza HTMX sul nuovo contenuto
        if(typeof htmx !== 'undefined') {
             htmx.process(el);
        }

        const m = new bootstrap.Modal(el, { backdrop: 'static', keyboard: true });
        el.addEventListener('hidden.bs.modal', () => el.remove(), { once: true });
        m.show();
        return m;
    };
    const confirm = (msg, callback, title = '', className = '') => {
        const el = document.createElement('div');
        el.className = 'modal fade';

        const header = title
            ? `<div class="modal-header border-0">
                <h5 class="modal-title">${title}</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>`
            : '';

        const contentClass = className ? `modal-dialog ${className}` : 'modal-dialog';

        el.innerHTML = `
            <div class="${contentClass}">
                <div class="modal-content">
                    ${header}
                    <div class="modal-body">${msg}</div>
                    <div class="modal-footer border-0">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Annulla</button>
                        <button type="button" class="btn btn-danger box-confirm">Conferma</button>
                    </div>
                </div>
            </div>`;

        document.body.appendChild(el);
        const m = new bootstrap.Modal(el);

        // Gestione click su Conferma
        el.querySelector('.box-confirm').addEventListener('click', () => {
            m.hide();
            if (typeof callback === 'function') callback(true);
        });

        // Gestione chiusura (Annulla o ESC o backdrop)
        el.addEventListener('hidden.bs.modal', () => {
            if (typeof callback === 'function') callback(false);
            el.remove();
        }, { once: true });

        m.show();
        return m;
    };

    const hideAll = () =>
        document.querySelectorAll('.modal').forEach(el => bootstrap.Modal.getInstance(el)?.hide());

    const hideLast = () => {
        const modals = document.querySelectorAll('.modal.show');
        const last = modals[modals.length - 1];
        if (last) bootstrap.Modal.getInstance(last)?.hide();
    };

    // Esponiamo la nuova funzione 'loader' insieme alle altre
    return { alert, dialog, confirm, hideAll, hideLast, loader };
})();