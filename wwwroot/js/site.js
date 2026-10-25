// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

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