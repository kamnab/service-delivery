window.dataUpdateInterop = {
    registerDataUpdatePrompt: function (dotNetRef) {
        if (!navigator.serviceWorker) return;

        navigator.serviceWorker.addEventListener('message', (event) => {
            if (event.data?.type === 'data-update-available') {
                console.log('[JS] New data available for:', event.data.url);
                dotNetRef.invokeMethodAsync('TriggerUpdateInstance', event.data.url);
            }
        });
    }
};
