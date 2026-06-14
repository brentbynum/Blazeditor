(function () {
    let tileRole = {
        none: 0,
        floor: 1,
        shim: 2,
    };
    let shimType = {
        none: 0,
        run: 1,
        capMask: 2,
        overhangMask: 3
    };

    window.blazeditor = {
        tileRole: tileRole,
        shimType: shimType,
    };
})();
