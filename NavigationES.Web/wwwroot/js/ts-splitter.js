// Question screen (TopicSession.razor): the bar between the question and the
// explanation can be dragged (or moved with the arrow keys) to give either side
// more room. The width of the explanation column lives in the --ts-aside-w CSS
// variable on <html> and is remembered in localStorage. Delegated listeners, so
// Blazor re-rendering the page needs no wiring.
(function () {
    const KEY = 'ts-aside-width';
    const MIN_ASIDE = 260;
    const MIN_MAIN = 420;
    const DEFAULT = 360;
    const STEP = 40;

    function clamp(width, layout) {
        const max = layout ? layout.getBoundingClientRect().width - MIN_MAIN : 900;
        return Math.round(Math.max(MIN_ASIDE, Math.min(width, Math.max(MIN_ASIDE, max))));
    }

    function apply(width, handle) {
        document.documentElement.style.setProperty('--ts-aside-w', width + 'px');
        if (handle) handle.setAttribute('aria-valuenow', String(width));
    }

    function save(width) {
        try { localStorage.setItem(KEY, String(width)); } catch (e) { /* private mode */ }
    }

    try {
        const stored = parseInt(localStorage.getItem(KEY), 10);
        if (stored > 0) apply(stored);
    } catch (e) { /* storage blocked: default width */ }

    function current(layout) {
        const aside = layout.querySelector('.ts-aside');
        return aside ? aside.getBoundingClientRect().width : DEFAULT;
    }

    document.addEventListener('pointerdown', function (e) {
        const handle = e.target.closest && e.target.closest('.ts-splitter');
        if (!handle || e.button !== 0) return;
        const layout = handle.closest('.ts-layout');
        if (!layout) return;
        e.preventDefault();
        handle.setPointerCapture(e.pointerId);
        document.body.classList.add('ts-resizing');
        let width = current(layout);

        function move(ev) {
            // The explanation sits on the right: its width runs from the pointer to the layout's edge.
            width = clamp(layout.getBoundingClientRect().right - ev.clientX, layout);
            apply(width, handle);
        }

        function up() {
            handle.removeEventListener('pointermove', move);
            handle.removeEventListener('pointerup', up);
            handle.removeEventListener('pointercancel', up);
            document.body.classList.remove('ts-resizing');
            save(width);
        }

        handle.addEventListener('pointermove', move);
        handle.addEventListener('pointerup', up);
        handle.addEventListener('pointercancel', up);
    });

    document.addEventListener('keydown', function (e) {
        const handle = e.target.closest && e.target.closest('.ts-splitter');
        if (!handle) return;
        const layout = handle.closest('.ts-layout');
        let width = current(layout);
        if (e.key === 'ArrowLeft') width += STEP;
        else if (e.key === 'ArrowRight') width -= STEP;
        else if (e.key === 'Home') width = DEFAULT;
        else return;
        e.preventDefault();
        e.stopPropagation();
        width = clamp(width, layout);
        apply(width, handle);
        save(width);
    }, true);

    document.addEventListener('dblclick', function (e) {
        const handle = e.target.closest && e.target.closest('.ts-splitter');
        if (!handle) return;
        apply(DEFAULT, handle);
        save(DEFAULT);
    });
})();
