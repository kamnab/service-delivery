window.networkStatusInterop = {
    init: function (dotNetRef) {
        function updateStatus() {
            dotNetRef.invokeMethodAsync('SetNetworkStatus', navigator.onLine);
        }

        window.addEventListener('online', updateStatus);
        window.addEventListener('offline', updateStatus);

        // Initial status call (only once)
        updateStatus();
    },

    getOnlineStatus: function () {
        return navigator.onLine;
    }
};
