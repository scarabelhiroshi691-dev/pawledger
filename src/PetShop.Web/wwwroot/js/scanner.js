(() => {
    const start = document.querySelector('#startScanner');
    const video = document.querySelector('#scanVideo');
    const status = document.querySelector('#scanStatus');
    if (!start || !video || !status) return;
    let stream;

    start.addEventListener('click', async () => {
        if (!('BarcodeDetector' in window) || !navigator.mediaDevices?.getUserMedia) {
            status.textContent = '当前浏览器或连接方式不支持直接扫码，请手动输入编码。使用 HTTPS 和新版 Chrome 可启用摄像头。';
            return;
        }
        try {
            const detector = new BarcodeDetector({ formats: ['qr_code', 'code_128', 'ean_13', 'ean_8'] });
            stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' }, audio: false });
            video.srcObject = stream; video.hidden = false; await video.play();
            status.textContent = '将编码放入取景框内。'; start.disabled = true;
            const tick = async () => {
                if (!stream) return;
                const codes = await detector.detect(video);
                if (codes[0]?.rawValue) {
                    stream.getTracks().forEach(track => track.stop()); stream = undefined;
                    window.location.assign(`/Scan?Code=${encodeURIComponent(codes[0].rawValue)}`); return;
                }
                window.requestAnimationFrame(tick);
            };
            tick();
        } catch (error) {
            status.textContent = `摄像头不可用：${error?.name === 'NotAllowedError' ? '请允许摄像头权限' : '请改用手动输入'}`;
            stream?.getTracks().forEach(track => track.stop()); stream = undefined; start.disabled = false;
        }
    });
    window.addEventListener('pagehide', () => stream?.getTracks().forEach(track => track.stop()));
})();
