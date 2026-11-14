mergeInto(LibraryManager.library, {
    ColdRoom_GetViewportWidth: function () {
        return Math.round(window.innerWidth || document.documentElement.clientWidth || 0);
    },

    ColdRoom_GetViewportHeight: function () {
        return Math.round(window.innerHeight || document.documentElement.clientHeight || 0);
    },

    ColdRoom_GetDevicePixelRatio: function () {
        return window.devicePixelRatio || 1;
    }
});
