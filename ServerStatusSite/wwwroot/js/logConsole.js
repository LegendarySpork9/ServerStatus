export function initScrollDetection(element, dotNetHelper) {
    element.addEventListener('scroll', () => {
        if (element.scrollTop < 200) {
            dotNetHelper.invokeMethodAsync('OnScrollNearTop');
        }
    });
}

export function scrollToBottom(element) {
    element.scrollTop = element.scrollHeight;
}
