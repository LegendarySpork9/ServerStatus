let isLoading = false;
let hasMore = true;

export function initScrollDetection(element, dotNetHelper) {
    isLoading = false;
    hasMore = true;

    element.addEventListener('scroll', async () => {
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
