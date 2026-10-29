

// url di base per lo storage cloudflare, da usare per le immagini e i file statici
const baseUrl = 'https://media.muzzlebook.it';

let deferredPrompt = null;
const installBtn = document.getElementById('pwa-install-btn');

// 1. Intercetta l'evento prima dell'installazione
window.addEventListener('beforeinstallprompt', (e) => {
    // Previene il banner di default del browser
    e.preventDefault();
    
    // Salva l'evento per poterlo attivare al click
    deferredPrompt = e;
    
    // Mostra il pulsante custom nell'interfaccia
    if (installBtn) {
        installBtn.classList.remove('d-none');
    }
});

// 2. Gestisci il click sul pulsante "Installa App"
if (installBtn) {
    installBtn.addEventListener('click', async () => {
        if (!deferredPrompt) return;

        // Mostra il dialog nativo di installazione
        deferredPrompt.prompt();

        // Leggi la risposta dell'utente
        const { outcome } = await deferredPrompt.userChoice;
        console.log(`Scelta utente PWA: ${outcome}`);

        // Il prompt può essere usato una sola volta, quindi lo azzeriamo
        deferredPrompt = null;

        // Nascondi nuovamente il pulsante
        installBtn.classList.add('d-none');
    });
}

// 3. Nascondi il pulsante se la PWA è già stata installata
window.addEventListener('appinstalled', () => {
    console.log('Muzzlebook PWA è stata installata con successo!');
    if (installBtn) {
        installBtn.classList.add('d-none');
    }
    deferredPrompt = null;
});

//iOS:
// Rileva se l'utente è su iOS e l'app non è in modalità Standalone
const isIOS = /iPad|iPhone|iPod/.test(navigator.userAgent) && !window.MSStream;
const isStandalone = window.matchMedia('(display-mode: standalone)').matches || navigator.standalone;

if (isIOS && !isStandalone) {
    // Puoi mostrare un piccolo banner o un pulsante che apre un box informativo:
    // "Per installare Muzzlebook su iPhone: premi il tasto Condividi e seleziona 'Aggiungi a schermata Home'"
}

        // Funzione Helper per ridimensionamento Canvas
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