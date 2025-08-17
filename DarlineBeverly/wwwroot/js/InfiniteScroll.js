
window.initInfiniteScroll = (dotNetRef, sentinelId) => {
    const sentinel = document.getElementById(sentinelId);
    const observer = new IntersectionObserver((entries) => {
        if (entries[0].isIntersecting) {
            dotNetRef.invokeMethodAsync('LoadPage');
        }
    }, { threshold: 0.1 });
    observer.observe(sentinel);
};