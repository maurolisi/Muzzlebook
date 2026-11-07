

// url di base per lo storage cloudflare, da usare per le immagini e i file statici
const baseUrl = 'https://media.muzzlebook.it';

// ==========================================
// GESTIONE VOCE DI MENU "INSTALLA APP" (PC & Mobile)
// ==========================================
const menuInstallItem = document.getElementById('menu-install-pwa');
const menuInstallBtn = document.getElementById('menu-install-btn');
let deferredPrompt = null;
const pwaBanner = document.getElementById('pwa-install-banner');
const acceptBtn = document.getElementById('pwa-accept-btn');
const closeBtn = document.getElementById('pwa-close-btn');
const installedStorageKey = 'muzzlebook_pwa_installed';

// Verifica se l'app è già installata (modalità standalone)
const isStandaloneDisplayMode = () =>
    window.matchMedia('(display-mode: standalone)').matches ||
    ('standalone' in window.navigator && Boolean(window.navigator.standalone));

function wasAppInstalled() {
    try {
        return localStorage.getItem(installedStorageKey) === 'true';
    } catch (error) {
        console.error('Impossibile leggere lo stato di installazione della PWA:', error);
        return false;
    }
}

function markAppInstalled() {
    try {
        localStorage.setItem(installedStorageKey, 'true');
    } catch (error) {
        console.error('Impossibile salvare lo stato di installazione della PWA:', error);
    }
}

function syncInstallUi() {
    const installed = isStandaloneDisplayMode() || wasAppInstalled();

    if (menuInstallItem) {
        menuInstallItem.classList.toggle('d-none', installed);
    }

    if (installed && pwaBanner) {
        nascondiBanner();
    }
}

// Mostra la voce di menu se l'app NON è installata
syncInstallUi();

const standaloneMedia = window.matchMedia('(display-mode: standalone)');
if (standaloneMedia && typeof standaloneMedia.addEventListener === 'function') {
    standaloneMedia.addEventListener('change', syncInstallUi);
} else if (standaloneMedia && typeof standaloneMedia.addListener === 'function') {
    standaloneMedia.addListener(syncInstallUi);
}

// Gestisci il click sul link di menu per avviare il prompt d'installazione
if (menuInstallBtn) {
    menuInstallBtn.addEventListener('click', async (e) => {
        e.preventDefault();

        if (!deferredPrompt) {
            alert("L'installazione non è al momento disponibile o l'app è già installata.");
            return;
        }

        // Mostra il prompt nativo del browser (funziona anche su Chrome/Edge da PC)
        deferredPrompt.prompt();
        
        const { outcome } = await deferredPrompt.userChoice;
        console.log(`Scelta utente PWA da menu: ${outcome}`);

        if (outcome === 'accepted') {
            markAppInstalled();
            syncInstallUi();
        }
        
        deferredPrompt = null;
    });
}
// ==========================================
// GESTIONE PWA INSTALL BANNER (Mostra a ogni ricarica/sessione)
// ==========================================
// Funzione per verificare se siamo nella Home e lo schermo è piccolo (mobile)
function shouldShowBanner() {
    const isMobile = window.innerWidth < 768;
    // Controlla se siamo esattamente nella root ("/" o "/Index")
    const isHome = window.location.pathname === '/' || window.location.pathname.toLowerCase() === '/index';
    
    return isMobile && isHome && !isStandaloneDisplayMode() && !wasAppInstalled();
}

// 1. Intercetta l'evento di installabilità del browser
window.addEventListener('beforeinstallprompt', (e) => {
    e.preventDefault();
    deferredPrompt = e;

    // Mostra il banner se siamo su mobile, in home, e l'utente non lo ha chiuso in questa sessione
    if (shouldShowBanner() && !sessionStorage.getItem('pwa_banner_closed_session')) {
        if (pwaBanner) {
            pwaBanner.classList.remove('d-none');
            setTimeout(() => pwaBanner.classList.add('show-banner'), 50);
        }
    }
});

// 2. Gestisci il click sul pulsante "Installa" del banner
if (acceptBtn) {
    acceptBtn.addEventListener('click', async () => {
        if (!deferredPrompt) {
            alert("L'installazione automatica non è al momento disponibile su questo browser/dispositivo.");
            return;
        }

        deferredPrompt.prompt();
        const { outcome } = await deferredPrompt.userChoice;
        console.log(`Scelta utente PWA: ${outcome}`);

        if (outcome === 'accepted') {
            markAppInstalled();
            syncInstallUi();
        }

        deferredPrompt = null;
        nascondiBanner();
    });
}

// 3. Gestisci il click sulla "X" per chiudere il banner nella sessione corrente
if (closeBtn) {
    closeBtn.addEventListener('click', () => {
        nascondiBanner();
        // Usa sessionStorage: il banner ricomparirà ricaricando o riaprendo il sito
        sessionStorage.setItem('pwa_banner_closed_session', 'true');
    });
}

window.addEventListener('resize', () => {
    if (!shouldShowBanner()) {
        nascondiBanner();
    }
});

function nascondiBanner() {
    if (pwaBanner) {
        pwaBanner.classList.remove('show-banner');
        setTimeout(() => pwaBanner.classList.add('d-none'), 300);
    }
}

// 4. Nascondi il banner se la PWA è già stata installata
window.addEventListener('appinstalled', () => {
    console.log('Muzzlebook PWA è stata installata con successo!');
    markAppInstalled();
    syncInstallUi();
    nascondiBanner();
    deferredPrompt = null;
});

// ==========================================
// 2. FUNZIONI HELPER GLOBALI (es. Canvas Resize)
// ==========================================
async function resizeImage(file, maxDimension = 800, quality = 0.85) {
    const imageBitmap = await createImageBitmap(file);
    let width = imageBitmap.width;
    let height = imageBitmap.height;

    if (width > maxDimension || height > maxDimension) {
        if (width > height) {
            height = Math.round((height * maxDimension) / width);
            width = maxDimension;
        } else {
            width = Math.round((width * maxDimension) / height);
            height = maxDimension;
        }
    }

    const canvas = document.createElement("canvas");
    canvas.width = width;
    canvas.height = height;
    const ctx = canvas.getContext("2d");
    ctx.drawImage(imageBitmap, 0, 0, width, height);

    return new Promise((resolve) => {
        canvas.toBlob((blob) => resolve(blob), "image/webp", quality);
    });
}

// 1. Definisci la funzione in modo esplicito sull'oggetto window per garantirne lo scope globale
window.ckFissa = function() {
    const btnFissa = document.getElementById('btn-fissa');
    const btnRimuovi = document.getElementById('btn-rimuovi'); 
    const navIconLX = document.getElementById('nav-fissa-icon-lx');
    const navIconDX = document.getElementById('nav-fissa-icon-dx');
    const formSwitch = document.getElementById('form-switch');
    
    // Controlla se la posizione fissa è attiva
    if (localStorage.getItem('posizioneFissaAttiva') === 'true') {
        if(navIconLX) navIconLX.classList.remove('d-none');
        if(navIconDX) navIconDX.classList.remove('d-none');
        if(btnFissa) btnFissa.classList.add('d-none');
        if(btnRimuovi) btnRimuovi.classList.remove('d-none');
        if(formSwitch) formSwitch.classList.add('opacity-50', 'pe-none');
    } else {
        if(navIconLX) navIconLX.classList.add('d-none');
        if(navIconDX) navIconDX.classList.add('d-none');
        if(btnFissa) btnFissa.classList.remove('d-none');
        if(btnRimuovi) btnRimuovi.classList.add('d-none');
        if(formSwitch) formSwitch.classList.remove('opacity-50', 'pe-none');
    }
    
    // Gestione mobile / desktop dell'icona navbar
    const isMobileWidth = window.innerWidth < 768;
    if (isMobileWidth) {
        if(navIconDX) navIconDX.remove();
    } else {
        if(navIconLX) navIconLX.remove();
    }   
};

// 2. Chiamala al caricamento classico della pagina (ricarica con F5 o primo accesso)
document.addEventListener('DOMContentLoaded', window.ckFissa);

// 3. Chiamala dopo ogni transizione di pagina gestita da HTMX (se utilizzato nel progetto)
document.body.addEventListener('htmx:afterSwap', window.ckFissa);