let isLoading = false;
let hasMore = true;
let isAtBottom = true;
let savedScrollHeight = 0;

export function initScrollDetection(element, dotNetHelper) {
    isLoading = false;
    hasMore = true;
    isAtBottom = true;

    element.addEventListener('scroll', async () => {
        const threshold = 50;
        isAtBottom = element.scrollTop + element.clientHeight >= element.scrollHeight - threshold;

        if (!hasMore || isLoading || element.scrollTop >= 200) {
            return;
        }

        try {
            isLoading = true;
            hasMore = await dotNetHelper.invokeMethodAsync('OnScrollNearTop');
        } catch {
            hasMore = true;
        } finally {
            isLoading = false;
        }
    });
}

export function scrollToBottom(element) {
    element.scrollTop = element.scrollHeight;
}

export function scrollToBottomIfNeeded(element) {
    if (isAtBottom) {
        element.scrollTop = element.scrollHeight;
    }
}

export function captureScrollHeight(element) {
    savedScrollHeight = element.scrollHeight;
}

export function restoreScrollPosition(element) {
    element.scrollTop += element.scrollHeight - savedScrollHeight;
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

export function appendLogEntries(element, logs, countElement, totalCount) {
    const commandDivider = element.querySelector('.command-divider');
    const scopeAttr = element.getAttributeNames().find(a => a.startsWith('b-'));

    const scope = scopeAttr ? ` ${scopeAttr}` : '';

    for (const log of logs) {
        const ts = log.timestamp.substring(0, 10) + ' ' + log.timestamp.substring(11, 19);
        const lvl = log.level;
        const tp = log.type;
        const msg = escapeHtml(log.message);

        const template = document.createElement('template');
        template.innerHTML =
            `<div${scope} class="log-entry log-${lvl.toLowerCase()}" data-js-appended>` +
            `<span${scope} class="log-timestamp">${ts}</span>` +
            `<span${scope} class="log-level">[${lvl}]</span>` +
            `<span${scope} class="log-type">[${tp}]</span>` +
            `<span${scope} class="log-message">${msg}</span>` +
            `</div>`;

        const entry = template.content.firstChild;

        if (commandDivider) {
            element.insertBefore(entry, commandDivider);
        } else {
            element.appendChild(entry);
        }
    }

    countElement.textContent = `${totalCount} log(s)`;

    if (isAtBottom) {
        element.scrollTop = element.scrollHeight;
    }
}

export function getCommandTarget() {
    const select = document.getElementById('commandTarget');
    return select ? select.value : 'Server';
}

export function getCommandText() {
    const input = document.getElementById('commandInput');
    return input ? input.value.trim() : '';
}

export function clearCommandText() {
    const input = document.getElementById('commandInput');
    if (input) input.value = '';
}

export function initCommandInput(dotNetHelper) {
    const input = document.getElementById('commandInput');
    if (input) {
        input.addEventListener('keydown', async (e) => {
            if (e.key === 'Enter' && input.value.trim()) {
                await dotNetHelper.invokeMethodAsync('OnCommandSubmit');
            }
        });
    }
}

export function clearAppendedEntries(element) {
    const appended = element.querySelectorAll('[data-js-appended]');
    appended.forEach(el => el.remove());
}
