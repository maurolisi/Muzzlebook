

// url di base per lo storage cloudflare, da usare per le immagini e i file statici
const baseUrl = 'https://media.muzzlebook.it';

// ==========================================
// GESTIONE VOCE DI MENU "INSTALLA APP" (PC & Mobile)
// ==========================================
const menuInstallItem = document.getElementById('menu-install-pwa');
const menuInstallBtn = document.getElementById('menu-install-btn');

// Verifica se l'app è già installata (modalità standalone)
const isAppInstalled = window.matchMedia('(display-mode: standalone)').matches || navigator.standalone;

// Mostra la voce di menu se l'app NON è installata
if (!isAppInstalled && menuInstallItem) {
    menuInstallItem.classList.remove('d-none');
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
            // Nascondi la voce di menu dopo l'installazione
            if (menuInstallItem) {
                menuInstallItem.classList.add('d-none');
            }
        }
        
        deferredPrompt = null;
    });
}
// ==========================================
// GESTIONE PWA INSTALL BANNER (Mostra a ogni ricarica/sessione)
// ==========================================
let deferredPrompt = null;
const pwaBanner = document.getElementById('pwa-install-banner');
const acceptBtn = document.getElementById('pwa-accept-btn');
const closeBtn = document.getElementById('pwa-close-btn');

// Funzione per verificare se siamo nella Home e lo schermo è piccolo (mobile)
function shouldShowBanner() {
    const isMobile = window.innerWidth < 768;
    // Controlla se siamo esattamente nella root ("/" o "/Index")
    const isHome = window.location.pathname === '/' || window.location.pathname.toLowerCase() === '/index';
    
    return isMobile && isHome;
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

function nascondiBanner() {
    if (pwaBanner) {
        pwaBanner.classList.remove('show-banner');
        setTimeout(() => pwaBanner.classList.add('d-none'), 300);
    }
}

// 4. Nascondi il banner se la PWA è già stata installata
window.addEventListener('appinstalled', () => {
    console.log('Muzzlebook PWA è stata installata con successo!');
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