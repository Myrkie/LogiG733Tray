const API_BASE = window.location.origin;

const DEFAULT_USER_AGENT = 'LogiTrayControl';

const DEFAULT_BODIES = 
    {'/powerofftime': `{
    "minutes": 10
}`,

    '/lights': `{
    "target": "both",
    "upperR": 255,
    "upperG": 0,
    "upperB": 255,
    "lowerR": 255,
    "lowerG": 0,
    "lowerB": 255,
    "mode": "Static"
}`};

document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('apiBase').textContent = API_BASE;

    loadCredentials();
    initializeEndpointCards();

    document.getElementById('saveAuth').addEventListener('click', saveCredentials);
    document.getElementById('clearAuth').addEventListener('click', clearCredentials);
});

function loadCredentials() {
    const apiKey = sessionStorage.getItem('logitray_api_key');
    const userAgent = sessionStorage.getItem('logitray_user_agent');

    if (apiKey) {
        document.getElementById('apiKey').value = apiKey;
    }

    document.getElementById('userAgent').value = userAgent || DEFAULT_USER_AGENT;

    updateAuthStatus();
}

function saveCredentials() {
    const apiKey = document.getElementById('apiKey').value.trim();
    const userAgent = document.getElementById('userAgent').value.trim();

    if (!apiKey) {
        setAuthStatus('API key is required.', 'error');
        return;
    }

    if (!userAgent) {
        setAuthStatus('User-Agent is required.', 'error');
        return;
    }

    sessionStorage.setItem('logitray_api_key', apiKey);
    sessionStorage.setItem('logitray_user_agent', userAgent);

    setAuthStatus('Credentials saved.', 'success');
}

function clearCredentials() {
    sessionStorage.removeItem('logitray_api_key');
    sessionStorage.removeItem('logitray_user_agent');

    document.getElementById('apiKey').value = '';
    document.getElementById('userAgent').value = DEFAULT_USER_AGENT;

    setAuthStatus('Credentials cleared.');
}

function updateAuthStatus() {
    const apiKey = getApiKey();

    if (apiKey) {
        setAuthStatus('Credentials loaded.', 'success');
    } else {
        setAuthStatus('API key not configured.');
    }
}

function setAuthStatus(message, type = '') {
    const element = document.getElementById('authStatus');

    element.textContent = message;
    element.className = 'auth-status';

    if (type) {
        element.classList.add(type);
    }
}

function getApiKey() {
    return document.getElementById('apiKey').value.trim();
}

function getUserAgent() {
    return (document.getElementById('userAgent').value.trim() || DEFAULT_USER_AGENT);
}

function autoResizeJsonEditor(editor) {
    editor.style.height = 'auto';
    editor.style.height = `${editor.scrollHeight}px`;
}

function initializeEndpointCards() {
    document.querySelectorAll('.endpoint-card')
        .forEach(card => {

            const executeButton = card.querySelector('[data-action="execute"]');

            if (executeButton) {
                executeButton.addEventListener('click', () => executeEndpoint(card));
            }

            const resetButton = card.querySelector('[data-action="reset"]');

            if (resetButton) {
                resetButton.addEventListener('click', () => resetRequest(card));
            }

            const editor =
                card.querySelector('.json-editor');

            if (editor) {
                autoResizeJsonEditor(editor);

                editor.addEventListener('input', () => {
                    autoResizeJsonEditor(editor);
                });
            }
        });
}

async function executeEndpoint(card) {
    const method = card.dataset.method;
    const path = card.dataset.path;

    const button = card.querySelector('[data-action="execute"]');
    const responseContainer = card.querySelector('.response');
    const statusCode = card.querySelector('.status-code');
    const editor = card.querySelector('.json-editor');

    button.classList.add('loading');
    button.textContent = 'Running...';

    responseContainer.hidden = false;

    statusCode.className = 'status-code';
    statusCode.textContent = '...';

    const responseBody = ensureResponseElement(responseContainer);
    responseBody.textContent = 'Sending request...';

    try {
        const headers = {
            'X-Api-Key': getApiKey(),
            'User-Agent': getUserAgent()
        };

        const options = {
            method,
            headers
        };

        if (method === 'POST' && editor) {
            const rawBody = editor.value.trim();

            if (!rawBody) {
                throw new Error('Request body is empty.');
            }

            let parsedBody;

            try {
                parsedBody = JSON.parse(rawBody);
            } catch (error) {
                throw new Error(`Invalid JSON: ${error.message}`);
            }

            options.headers['Content-Type'] = 'application/json';
            options.body = JSON.stringify(parsedBody);
        }

        const startTime = performance.now();

        const response = await fetch(`${API_BASE}${path}`, options);

        const elapsed = Math.round(performance.now() - startTime);

        const contentType =
            response.headers.get('content-type') || '';

        const body = await response.text();

        statusCode.textContent =
            `${response.status} ${response.statusText} · ${elapsed} ms`;

        if (response.ok) {
            statusCode.classList.add('success');
        } else {
            statusCode.classList.add('error');
        }

        if (isHtmlResponse(contentType, body)) {
            renderHtmlResponse(
                responseContainer,
                body,
                response.url
            );
            return;
        }

        if (isJsonResponse(contentType, body)) {
            renderJsonResponse(responseContainer, body);
            return;
        }

        renderTextResponse(responseContainer, body);

    } catch (error) {
        statusCode.textContent = 'Request failed';
        statusCode.className = 'status-code error';

        const responseBody = ensureResponseElement(responseContainer);

        responseBody.textContent = formatError(error);

    } finally {
        button.classList.remove('loading');
        button.textContent = 'Execute';
    }
}
function isHtmlResponse(contentType, body) {
    const normalizedType = contentType.toLowerCase();

    if (normalizedType.includes('text/html') || normalizedType.includes('application/xhtml+xml')) {
        return true;
    }

    const trimmed = body.trim().toLowerCase();

    return (trimmed.startsWith('<!doctype html') || trimmed.startsWith('<html') || trimmed.startsWith('<html '));
}

function isJsonResponse(contentType, body) {
    const normalizedType = contentType.toLowerCase();

    if (normalizedType.includes('application/json') || normalizedType.includes('+json')) {
        return true;
    }

    const trimmed = body.trim();

    if (!trimmed) {
        return false;
    }

    try {
        JSON.parse(trimmed);
        return true;
    } catch {
        return false;
    }
}

function ensureResponseElement(responseContainer) {
    let responseBody = responseContainer.querySelector('.response-body');

    if (!responseBody) {
        const iframe = responseContainer.querySelector('.response-html');

        if (iframe) {
            iframe.remove();
        }

        responseBody = document.createElement('pre');

        responseBody.className = 'response-body';

        responseContainer.appendChild(responseBody);
    }

    return responseBody;
}

function renderHtmlResponse(responseContainer, html, responseUrl) {
    const responseBody = responseContainer.querySelector('.response-body');

    if (responseBody) {
        responseBody.remove();
    }

    const existingIframe = responseContainer.querySelector('.response-html');

    if (existingIframe) {
        existingIframe.remove();
    }

    const iframe = document.createElement('iframe');

    iframe.className = 'response-html';

    iframe.title = 'HTML API response';

    iframe.setAttribute('sandbox', 'allow-scripts');

    const baseUrl = responseUrl || API_BASE;

    iframe.srcdoc = addBaseUrlToHtml(html, baseUrl);

    responseContainer.appendChild(iframe);

    iframe.addEventListener('load', () => {
            resizeResponseIframe(iframe);
        }
    );
}

function addBaseUrlToHtml(html, baseUrl) {
    const escapedUrl =
        escapeHtmlAttribute(baseUrl);

    const baseTag =
        `<base href="${escapedUrl}">`;
    if (/<head[\s>]/i.test(html)) {
        return html.replace(
            /<head([^>]*)>/i,
            `<head$1>${baseTag}`
        );
    }
    if (/<html[\s>]/i.test(html)) {
        return html.replace(
            /<html([^>]*)>/i,
            `<html$1><head>${baseTag}</head>`
        );
    }
    return `
<!DOCTYPE html>
<html>
<head>
    ${baseTag}
    <meta charset="UTF-8">
</head>
<body>
    ${html}
</body>
</html>
`;}

function escapeHtmlAttribute(value) {
    return String(value)
        .replace(/&/g, '&amp;')
        .replace(/"/g, '&quot;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;');
}

function resizeResponseIframe(iframe) {
    try {
        const document = iframe.contentDocument;

        if (!document) {
            return;
        }

        const body = document.body;
        const html = document.documentElement;

        const height = Math.max(
            body ? body.scrollHeight : 0,
            body ? body.offsetHeight : 0,
            html ? html.scrollHeight : 0,
            html ? html.offsetHeight : 0
        );

        iframe.style.height = `${Math.max(height, 350)}px`;
    } catch (error) {
        console.warn('Could not resize response iframe:', error);
    }
}

function renderJsonResponse(responseContainer, body) {
    const responseBody = ensureResponseElement(responseContainer);

    try {
        const parsed = JSON.parse(body);

        responseBody.textContent = JSON.stringify(parsed, null, 4);
    } catch (error) {
        responseBody.textContent =
            body || '(empty response)';
    }
}

function renderTextResponse(responseContainer, body) {
    const responseBody =ensureResponseElement(responseContainer);

    responseBody.textContent = body || '(empty response)';
}

function resetRequest(card) {
    const path = card.dataset.path;
    const editor = card.querySelector('.json-editor');

    if (!editor) {
        return;
    }

    if (DEFAULT_BODIES[path]) {
        editor.value = DEFAULT_BODIES[path];
    }
}

function formatError(error) {
    if (!error) {
        return 'Unknown error.';
    }
    if (error.message === 'Failed to fetch') {
        return [
            'Failed to fetch the API.',
            '',
            'Possible causes:',
            '• The API host is not running.',
            '• The API address is unreachable.',
            '• The request was blocked by the browser.',
            '• Authentication headers were rejected.',
            '',
            'If this page is hosted by LogiTrayControl itself,',
            'check that the API server is listening correctly.'
        ].join('\n');
    }

    return error.message || String(error);
}

document.addEventListener('click', async event => {
    const code = event.target.closest('code');

    if (!code) {
        return;
    }

    await util.copyText(code.textContent, code);
});
