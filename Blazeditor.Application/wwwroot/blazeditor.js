(function () {
    let tileRole = {
        none: 0,
        floor: 1,
        shim: 2,
    };
    let shimType = {
        none: 0,
        run: 'Run',
        capMask: 'CapMask',
        overhangMask: 'OverhangMask'
    };

    window.blazeditor = {
        tileRole: tileRole,
        shimType: shimType,
    };
})();
