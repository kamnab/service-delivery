const myModal = document.getElementById('myModal')
myModal.addEventListener('show.bs.modal', async (e) => {
    console.log("show.bs.modal event fired");

    var id = e.relatedTarget.dataset.id;
    var url = e.relatedTarget.dataset.myAction;
    var title = e.relatedTarget.dataset.myModalTitle;
    var noFooter = e.relatedTarget.dataset.myModalNoFooter;
    var btnSubmit = e.relatedTarget.dataset.myModalSubmitButtonText;
    var btnSubmitClass = e.relatedTarget.dataset.myModalSubmitButtonClass;

    const modalBody = myModal.querySelector('.modal-body');
    modalBody.innerHTML = '';

    if (noFooter !== undefined) {
        myModal.querySelector('.modal-footer').style.display = 'none';
    } else {
        myModal.querySelector('.modal-footer').style.display = '';
    }

    myModal.querySelector('.modal-body').innerHTML = url;

    // -> create a new div element
    const p = document.createElement("p");
    // p.style.fontSize = "15px"; 
    p.style.marginTop = "20px";
    // -> and give it some content
    const newContent = document.createTextNode(document.getElementById(id).textContent);
    // -> add the text node to the newly created div
    p.appendChild(newContent);

    myModal.querySelector('.modal-body').append(p);

    // $(myModal).find('.modal-body').load(url);
    $(myModal).find('.modal-title').text(title);
    $(myModal).find('button[type="submit"]').text(btnSubmit);
    if (btnSubmitClass) {
        $(myModal).find('button[type="submit"]')
            .removeClass()
            .addClass(btnSubmitClass);
    }
});

// const loadCanvas = (canvas, gifUrl) => {
//     const ctx = canvas.getContext('2d');

//     // Create a new Image object
//     const gifImage = new Image();

//     // Set the source of the image
//     gifImage.src = gifUrl;

//     // Once the GIF has loaded, draw it onto the canvas
//     gifImage.onload = function () {
//         // Set canvas dimensions to match GIF dimensions
//         canvas.width = gifImage.width;
//         canvas.height = gifImage.height;

//         // Draw the current frame of the GIF onto the canvas
//         ctx.drawImage(gifImage, 0, 0);
//     };
// }

// window.onload = function () {
//     const canvasList = Array.from(document.getElementsByTagName('canvas'));

//     canvasList.forEach(canvas => {
//         const gifUrl = canvas.getAttribute('data-my-gif-url'); // Assuming you have a data attribute for gif URL
//         loadCanvas(canvas, gifUrl);
//     });
// }
