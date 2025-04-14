window.networkStatusInterop = {
    init: function (dotNetRef) {
        function updateStatus() {
            dotNetRef.invokeMethodAsync('SetNetworkStatus', navigator.onLine);
        }

        window.addEventListener('online', updateStatus);
        window.addEventListener('offline', updateStatus);

        // Call it initially
        updateStatus();
    }
};
