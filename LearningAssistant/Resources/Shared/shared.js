/**
 * 网页游戏公共工具（Shared）。
 * 供所有 WebView2 游戏共用。暴露 window.GameUI 命名空间，各游戏 game.js 直接使用。
 *
 * 提供：
 *   - 基础通信（bridge / sendToHost / listenInit）
 *   - UI 组件（toast / playSound / speak / esc / applyTheme）
 *   - 游戏通用组件（$ / createTimer / formatTime / renderStars / setProgress / shuffle）
 *   - 设置同步（initSkipKnownRadios / metaTotalRemaining）
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

        // ==================== DOM 快捷方式 ====================

        /** document.getElementById 快捷方式。 */
        $(id) {
            return document.getElementById(id);
        },

        // ==================== UI 组件 ====================

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
         * @param kind
         *   "click"   = 短促点击音
         *   "correct" = 正确/成功（升调三连音）
         *   "wrong"   = 错误/失败（下降双音）
         *   "match"   = 配对成功（短上行音）
         *   "error"   = 配对错误（柔和低音）
         *   "complete" = 完成庆祝（上行音阶）
         *   "step"    = 轻步骤音
         *   "select"  = 选中/放置音
         *   "tick"    = 计时滴答
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
                switch (kind) {
                    case "click":
                        tone(800, 0, 0.05, "sine", 0.06);
                        break;
                    case "correct":
                        tone(523, 0, 0.1, "sine", 0.15);
                        tone(659, 0.08, 0.1, "sine", 0.15);
                        tone(784, 0.16, 0.15, "sine", 0.18);
                        break;
                    case "wrong":
                        tone(400, 0, 0.12, "square", 0.1);
                        tone(300, 0.1, 0.2, "square", 0.1);
                        break;
                    case "match":
                        tone(523.25, 0, 0.12, "sine", 0.12);
                        tone(783.99, 0.09, 0.16, "sine", 0.12);
                        break;
                    case "error":
                        tone(220, 0, 0.18, "sine", 0.08);
                        break;
                    case "complete":
                        for (var i = 0; i < 8; i++) {
                            (function(f, d) {
                                tone(f, d, 0.12, "sine", 0.15);
                            })(523.25 + i * 65, i * 0.06);
                        }
                        break;
                    case "step":
                        tone(600, 0, 0.04, "sine", 0.05);
                        break;
                    case "select":
                        tone(550, 0, 0.08, "sine", 0.1);
                        break;
                    case "tick":
                        tone(1200, 0, 0.025, "sine", 0.03);
                        break;
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

        // ==================== 游戏通用组件 ====================

        /**
         * 创建计时器。
         * @param {Object} config
         * @param {HTMLElement} [config.element] - 显示时间的 DOM 元素（可选）
         * @param {number} [config.interval=100] - 更新间隔（ms）
         * @param {Function} [config.onTick] - 每次 tick 回调，参数 (elapsedSeconds)
         * @param {number} [config.maxTime] - 超时秒数（可选）
         * @param {Function} [config.onTimeUp] - 超时回调（可选）
         * @returns {{ start, stop, reset, getElapsed }}
         */
        createTimer(config) {
            config = config || {};
            const interval = config.interval || 100;
            let id = null;
            let elapsed = 0;
            let startTs = 0;
            let running = false;

            function updateDisplay() {
                if (config.element) {
                    config.element.textContent = GameUI.formatTime(elapsed, true);
                }
                if (config.onTick) config.onTick(elapsed);
                if (config.maxTime && elapsed >= config.maxTime && running) {
                    running = false;
                    if (id) { clearInterval(id); id = null; }
                    if (config.onTimeUp) config.onTimeUp();
                }
            }

            return {
                /** 启动计时器（已运行时忽略）。 */
                start() {
                    if (id) return;
                    startTs = performance.now() - elapsed * 1000;
                    running = true;
                    id = setInterval(() => {
                        elapsed = (performance.now() - startTs) / 1000;
                        updateDisplay();
                    }, interval);
                },
                /** 停止计时器。 */
                stop() {
                    if (id) { clearInterval(id); id = null; }
                    running = false;
                },
                /** 重置计时器（停止并归零）。 */
                reset() {
                    this.stop();
                    elapsed = 0;
                    if (config.element) config.element.textContent = GameUI.formatTime(0, true);
                },
                /** 获取已过秒数。 */
                getElapsed() { return elapsed; },
                /** 计时器是否正在运行。 */
                isRunning() { return running; }
            };
        },

        /**
         * 格式化时间显示。
         * @param {number} seconds - 秒数
         * @param {boolean} [precise=false] - 是否显示小数（如 "12.5s"）
         * @returns {string}
         */
        formatTime(seconds, precise = false) {
            if (seconds < 0) seconds = 0;
            if (seconds < 60) {
                return precise ? seconds.toFixed(1) + "s" : Math.round(seconds) + "s";
            }
            const m = Math.floor(seconds / 60);
            const s = Math.round(seconds % 60);
            return `${m}m ${s}s`;
        },

        /**
         * 生成星级评分 HTML。
         * @param {number} count - 亮星数量（0-3）
         * @returns {string} 包含 .lit/.dim 的 span 星号 HTML
         */
        renderStars(count) {
            return Array.from({ length: 3 }, (_, i) =>
                `<span class="${i < count ? 'lit' : 'dim'}">★</span>`
            ).join("");
        },

        /**
         * 更新进度条宽度。
         * @param {HTMLElement} progressEl - 进度条 DOM 元素
         * @param {number} current - 当前值
         * @param {number} total - 总值
         */
        setProgress(progressEl, current, total) {
            if (!progressEl) return;
            progressEl.style.width = (total > 0 ? (current / total) * 100 : 0) + "%";
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

        // ==================== 宿主通信 ====================

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

        // ==================== 设置同步 ====================

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