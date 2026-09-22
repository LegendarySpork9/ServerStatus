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

        isLoading = true;
        hasMore = await dotNetHelper.invokeMethodAsync('OnScrollNearTop');
        isLoading = false;
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

export function appendLogEntries(element, logs, countElement, totalCount) {
    const commandDivider = element.querySelector('.command-divider');

    for (const log of logs) {
        const entry = document.createElement('div');
        entry.className = `log-entry log-${log.level.toLowerCase()}`;
        entry.setAttribute('data-js-appended', '');

        const timestamp = document.createElement('span');
        timestamp.className = 'log-timestamp';
        timestamp.textContent = log.timestamp.substring(0, 10) + ' ' + log.timestamp.substring(11, 19);

        const level = document.createElement('span');
        level.className = 'log-level';
        level.textContent = `[${log.level}]`;

        const type = document.createElement('span');
        type.className = 'log-type';
        type.textContent = `[${log.type}]`;

        const message = document.createElement('span');
        message.className = 'log-message';
        message.textContent = log.message;

        entry.appendChild(timestamp);
        entry.appendChild(document.createTextNode('\n'));
        entry.appendChild(level);
        entry.appendChild(document.createTextNode('\n'));
        entry.appendChild(type);
        entry.appendChild(document.createTextNode('\n'));
        entry.appendChild(message);

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

export function clearAppendedEntries(element) {
    const appended = element.querySelectorAll('[data-js-appended]');
    appended.forEach(el => el.remove());
}
