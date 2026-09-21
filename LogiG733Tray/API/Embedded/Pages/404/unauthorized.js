document.addEventListener('click', async event => {
    const code = event.target.closest('code');

    if (!code) {
        return;
    }

    await util.copyText(code.textContent, code);
});