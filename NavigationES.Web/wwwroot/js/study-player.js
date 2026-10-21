// The Estudiar video player (EstudiarVideo.razor): resumes at the saved position
// and reports progress to .NET every 10 s of playback, on pause and at the end.
window.studyPlayer = {
    attach(video, dotnet, startAt) {
        if (!video) return;
        let lastSent = 0;
        const send = (completed) => {
            lastSent = video.currentTime;
            dotnet.invokeMethodAsync("SaveProgress", Math.floor(video.currentTime), completed).catch(() => { });
        };

        video.addEventListener("loadedmetadata", () => {
            // A video watched to (almost) the end starts again from the beginning.
            if (startAt > 0 && startAt < video.duration - 5) video.currentTime = startAt;
        }, { once: true });
        video.addEventListener("timeupdate", () => {
            if (Math.abs(video.currentTime - lastSent) >= 10) send(false);
        });
        video.addEventListener("pause", () => { if (!video.ended) send(false); });
        video.addEventListener("ended", () => send(true));
        video.addEventListener("seeked", () => { lastSent = video.currentTime; });
    },

    seek(video, seconds) {
        if (!video) return;
        video.currentTime = seconds;
        video.play().catch(() => { });
    },
};
