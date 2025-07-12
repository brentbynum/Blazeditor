window.areaEditorKeyboard = {
    init: function (dotNetRef) {
        function handler(e) {
            if (e.ctrlKey && e.key === 'z') {
                if (e.shiftKey) {
                    // Redo
                    dotNetRef.invokeMethodAsync('OnRedo');
                } else {
                    // Undo
                    dotNetRef.invokeMethodAsync('OnUndo');
                }
                e.preventDefault();
            }
        }
        window.addEventListener('keydown', handler);
        window._areaEditorKeyboardHandler = handler;
    },
    dispose: function () {
        if (window._areaEditorKeyboardHandler) {
            window.removeEventListener('keydown', window._areaEditorKeyboardHandler);
            window._areaEditorKeyboardHandler = null;
        }
    }
};