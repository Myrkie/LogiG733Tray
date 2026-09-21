const util = {
    async copyText(text, element) {
        try {
            const copyValue =
                element.dataset.originalText ?? text;

            if (navigator.clipboard?.writeText) {
                await navigator.clipboard.writeText(copyValue);
            } else {
                util.fallbackCopy(copyValue);
            }

            util.flashCopied(element);

        } catch (error) {
            console.error('Copy failed:', error);
        }
    },

    fallbackCopy(text) {
        const textarea = document.createElement('textarea');

        textarea.value = text;
        textarea.style.position = 'fixed';
        textarea.style.left = '-9999px';

        document.body.appendChild(textarea);
        textarea.select();

        try {
            document.execCommand('copy');
        } finally {
            document.body.removeChild(textarea);
        }
    },

    flashCopied(element) {
        if (!element.dataset.originalText) {
            element.dataset.originalText = element.textContent;
        }

        element.textContent = 'Copied!';

        if (element._copyTimeout) {
            clearTimeout(element._copyTimeout);
        }

        element._copyTimeout = setTimeout(() => {
            element.textContent = element.dataset.originalText;
            element._copyTimeout = null;
        }, 1000);
    }
};