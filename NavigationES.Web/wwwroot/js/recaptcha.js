// reCAPTCHA v3 for the signup form (RecaptchaClient.cs) and forgot-password.html.
// Google's script is fetched only when a page first asks for a token, so visitors
// who never open those forms never load it. Every failure resolves to null: the
// API then decides (it refuses browser calls without a token only when its secret
// key is configured).
window.navigationesRecaptcha = (function () {
    let loading = null;

    function load(siteKey) {
        if (!loading) {
            loading = new Promise(function (resolve, reject) {
                const script = document.createElement('script');
                script.src = 'https://www.google.com/recaptcha/api.js?render=' + encodeURIComponent(siteKey);
                script.async = true;
                script.onload = function () { window.grecaptcha.ready(resolve); };
                script.onerror = function () { loading = null; reject(new Error('reCAPTCHA failed to load')); };
                document.head.appendChild(script);
            });
        }
        return loading;
    }

    return {
        // Warm-up when the form opens, so the first submit does not wait for the download.
        preload: function (siteKey) {
            if (siteKey) load(siteKey).catch(function () { });
        },

        execute: async function (siteKey, action) {
            if (!siteKey) return null;
            try {
                await load(siteKey);
                return await window.grecaptcha.execute(siteKey, { action: action });
            } catch (e) {
                console.error(e);
                return null;
            }
        }
    };
})();
