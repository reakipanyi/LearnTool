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

        /**
         * 轻量 WebAudio 音效（无需音频资源文件）。
         * @param kind "match"=配对成功（轻快上行音），"error"=配对错误（柔和低音）。
         */
        playSound(kind) {
            try {
                const AC = window.AudioContext || window.webkitAudioContext;
                if (!AC) return;
                if (!window._gameAudioCtx) window._gameAudioCtx = new AC();
                const ctx = window._gameAudioCtx;
                if (ctx.state === "suspended") ctx.resume();
                const now = ctx.currentTime;
                const tone = (freq, start, dur, type = "sine", vol = 0.12) => {
                    const osc = ctx.createOscillator();
                    const gain = ctx.createGain();
                    osc.type = type;
                    osc.frequency.value = freq;
                    gain.gain.setValueAtTime(0.0001, now + start);
                    gain.gain.exponentialRampToValueAtTime(vol, now + start + 0.02);
                    gain.gain.exponentialRampToValueAtTime(0.0001, now + start + dur);
                    osc.connect(gain).connect(ctx.destination);
                    osc.start(now + start);
                    osc.stop(now + start + dur + 0.05);
                };
                if (kind === "match") {
                    tone(523.25, 0, 0.12);      // C5
                    tone(783.99, 0.09, 0.16);   // G5
                } else if (kind === "error") {
                    tone(220, 0, 0.18, "sine", 0.08); // 柔和低音
                }
            } catch (e) { /* 无音频输出时静默 */ }
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

        /** 监听宿主注入的 {type:"init"} 消息；onInit(data, theme, meta) 由各游戏实现。 */
        listenInit(onInit) {
            const wb = getBridge();
            if (wb) {
                wb.addEventListener("message", (e) => {
                    let msg = e.data;
                    if (typeof msg === "string") {
                        try { msg = JSON.parse(msg); } catch (err) { return; }
                    }
                    if (msg && msg.type === "init") onInit(msg.data, msg.theme, msg.meta);
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
        },

        /**
         * 初始化"跳过已知项 / 加载所有"单选组（位于"换一组"旁）。
         * @param groupId 单选组容器 id
         * @param onChange 切换回调，参数为 boolean（true=跳过已知项）
         * @returns {set} 用 set(meta.skipKnown) 同步前端初始状态
         */
        initSkipKnownRadios(groupId, onChange) {
            const group = document.getElementById(groupId);
            if (!group) return { set() {} };
            if (!group._bound) {
                group._bound = true;
                group.querySelectorAll('input[name="skipKnown"]').forEach((r) => {
                    r.addEventListener("change", () => {
                        if (r.checked) onChange(r.value === "skip");
                    });
                });
            }
            return {
                set(value) {
                    const target = value ? "skip" : "all";
                    group.querySelectorAll('input[name="skipKnown"]').forEach((r) => {
                        r.checked = r.value === target;
                    });
                }
            };
        },

        /** 读取当前 meta 中的总剩余条目数（无 meta 时返回 0）。 */
        metaTotalRemaining(meta) {
            return (meta && typeof meta.totalRemaining === "number") ? meta.totalRemaining : 0;
        }
    };
})();