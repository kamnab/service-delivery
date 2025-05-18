document.body.addEventListener('click', function (e) {
    const target = e.target.closest('a.liveToastBtn');
    if (!target) return;

    e.preventDefault();

    const toastLiveExample = document.getElementById('liveToast');
    if (!toastLiveExample) return;

    const toast = new bootstrap.Toast(toastLiveExample);

    const hrefToCopy = target.dataset.href;
    navigator.clipboard.writeText(hrefToCopy).then(() => {
        const tooltip = document.getElementById('toast-msg');
        if (tooltip) {
            tooltip.innerHTML = hrefToCopy;
        }

        toast.show();
    }).catch(err => {
        console.error('Failed to copy text: ', err);
    });
});

// ------ Components
window.loadViewComponentScript = () => {
    const scriptId = "view-component-script";
    if (!document.getElementById(scriptId)) {
        const script = document.createElement("script");
        script.src = "/js/components/view.js";
        script.id = scriptId;
        script.defer = true;
        document.body.appendChild(script);
    }
};

window.initializeCanvasAnimations = () => {
    const canvasList = Array.from(document.getElementsByTagName('canvas'));
    console.log("loadCanvas", canvasList.length);

    canvasList.forEach(canvas => {
        const gifUrl = canvas.getAttribute('data-my-gif-url');
        if (gifUrl) {
            loadCanvas(canvas, gifUrl); // Make sure this function is globally defined
        }
    });
};

const loadCanvas = (canvas, gifUrl) => {
    const ctx = canvas.getContext('2d');

    // Create a new Image object
    const gifImage = new Image();

    // Set the source of the image
    gifImage.src = gifUrl;

    // Once the GIF has loaded, draw it onto the canvas
    gifImage.onload = function () {
        // Set canvas dimensions to match GIF dimensions
        canvas.width = gifImage.width;
        canvas.height = gifImage.height;

        // Draw the current frame of the GIF onto the canvas
        ctx.drawImage(gifImage, 0, 0);
    };
}
