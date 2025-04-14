window.registerUpdatePrompt = function (dotNetRef) {
    window.addEventListener('app-update-available', () => {
        dotNetRef.invokeMethodAsync('TriggerUpdateInstance');
    });
};
