const util = {
    async copyText(text, element) {
        try {
            const copyValue = element.dataset.originalText ?? text;

            await navigator.clipboard.writeText(copyValue);

            util.flashCopied(element);
        } catch (error) {
            console.error('Copy failed:', error);
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