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


/*
1- Your service worker caches fresh data and posts a message (type: 'data-update-available')
2- Your JS interop listens and calls TriggerUpdateInstance(url) on Blazor
3- Blazor shows a nice toast, prompting the user to reload for fresh data
*/