document.querySelectorAll('code').forEach(code => {
    code.style.cursor = 'pointer';
    code.title = 'Click to copy';
    code.addEventListener('click', () => {
        const text = code.textContent;

        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(text)
                .then(() => flashCopied(code))
                .catch(() => fallbackCopy(text, code));
        } else {
            fallbackCopy(text, code);
        }
    });
});

function fallbackCopy(text, code) {
    const textarea = document.createElement('textarea');
    textarea.value = text;
    textarea.style.position = 'fixed';
    textarea.style.opacity = '0';
    document.body.appendChild(textarea);
    textarea.select();
    try {
        // noinspection JSDeprecatedSymbols
        document.execCommand('copy');
        flashCopied(code);
    } catch (err) {
        console.error('Fallback copy failed', err);
    }
    document.body.removeChild(textarea);
}

function flashCopied(code) {
    const original = code.textContent;
    code.textContent = 'Copied!';
    setTimeout(() => code.textContent = original, 1000);
}