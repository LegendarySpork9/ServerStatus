let isLoading = false;
let hasMore = true;
let isAtBottom = true;

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
