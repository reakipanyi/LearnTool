/**
 * 网页游戏公共工具（Shared）。
 * 供单词消消乐 / 记忆翻牌 / 连连看 / 单词拼写 / 打地鼠 等所有 WebView2 游戏共用。
 * 暴露 window.GameUI 命名空间，各游戏 game.js 直接使用。
 */
(() => {
    "use strict";

    // 获取 WebView2 宿主桥接对象（无宿主时返回 null，浏览器调试用）。
    // 用闭包函数而非 this，避免游戏侧把方法解构出来独立调用时丢失 this 上下文。
    function getBridge() {
        return (window.chrome && window.chrome.webview) ? window.chrome.webview : null;
    }

    window.GameUI = {

        /** 获取 WebView2 宿主桥接对象（无宿主时返回 null，浏览器调试用）。 */
        bridge() {
            return getBridge();
        },

        /** 向宿主发送消息（WebView2 自动序列化 JSON）。 */
        sendToHost(msg) {
            const wb = getBridge();
            if (wb) wb.postMessage(msg);
        },

        /** 顶部 toast 提示；需要页面存在 <div id="toast" class="toast">。 */
        toast(text, duration = 1300) {
            const el = document.getElementById("toast");
            if (!el) return;
            el.textContent = text;
            el.classList.add("show");
            clearTimeout(el._t);
            el._t = setTimeout(() => el.classList.remove("show"), duration);
        },

        /** 朗读文本。WebView2 内优先走宿主 TTS（系统语音更可靠）；无宿主（浏览器调试）时用 speechSynthesis 兜底。 */
        speak(text, lang = "en-US") {
            const wb = getBridge();
            if (wb) {
                wb.postMessage({ type: "speak", text, lang });
                return;
            }
            try {
                if (!window.speechSynthesis) return;
                const u = new SpeechSynthesisUtterance(text);
                u.lang = lang;
                u.rate = 0.9;
                window.speechSynthesis.cancel();
                window.speechSynthesis.speak(u);
            } catch (e) { /* 静默降级 */ }
        },

        /** HTML 转义，防止拼接进 innerHTML 时被注入。 */
        esc(text) {
            return String(text == null ? "" : text)
                .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;")
                .replace(/"/g, "&quot;").replace(/'/g, "&#39;");
        },

        /** 是否为浏览器调试模式（URL 带 ?mock=1）。 */
        isMock() {
            return new URLSearchParams(location.search).get("mock") === "1";
        },

        /** 监听宿主注入的 {type:"init"} 消息；onInit(data, theme) 由各游戏实现。 */
        listenInit(onInit) {
            const wb = getBridge();
            if (wb) {
                wb.addEventListener("message", (e) => {
                    let msg = e.data;
                    if (typeof msg === "string") {
                        try { msg = JSON.parse(msg); } catch (err) { return; }
                    }
                    if (msg && msg.type === "init") onInit(msg.data, msg.theme);
                });
                // 通知宿主监听器已就绪；若宿主在监听器注册前就下发了 init，会自动补发，避免数据丢失。
                wb.postMessage({ type: "__ready" });
            }
        },

        /** Fisher-Yates 洗牌（原地打乱并返回原数组）。 */
        shuffle(arr) {
            for (let i = arr.length - 1; i > 0; i--) {
                const j = Math.floor(Math.random() * (i + 1));
                [arr[i], arr[j]] = [arr[j], arr[i]];
            }
            return arr;
        },

        /** 应用明暗主题：设置 body[data-theme]。 */
        applyTheme(theme) {
            document.body.setAttribute("data-theme", theme || "light");
        }
    };
})();