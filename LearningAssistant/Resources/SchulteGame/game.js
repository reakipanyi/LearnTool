(() => {
    "use strict";

    const { toast, sendToHost, listenInit, isMock, applyTheme } = window.GameUI;

    // ---------- 视觉干扰配色 ----------
    const COLOR_PALETTE = [
        '#3b82f6', '#ef4444', '#22c55e', '#f59e0b', '#8b5cf6',
        '#ec4899', '#06b6d4', '#f97316', '#14b8a6', '#e11d48',
        '#6366f1', '#84cc16', '#d946ef', '#0ea5e9', '#f43f5e',
        '#a855f7', '#10b981', '#eab308', '#64748b', '#78716c',
        '#2dd4bf', '#fb923c', '#a78bfa', '#34d399', '#f472b6'
    ];

    // ---------- 状态 ----------
    let gridSize = 5;           // N×N
    let numbers = [];           // 打乱后的数字数组
    let currentTarget = 1;      // 当前应点击的数字
    let errors = 0;
    let score = 0;
    let timerSeconds = 0;
    let timerId = null;
    let started = false;
    let finished = false;
    let challengeTimeSec = 90;
    let difficulty = "普通";

    // ---------- DOM ----------
    const $ = (id) => document.getElementById(id);
    const board = $("board");
    const hint = $("hint");
    const nextTargetEl = $("nextTarget");
    const timerEl = $("timer");
    const errorsEl = $("errors");
    const scoreEl = $("score");
    const progressEl = $("progress");

    // ---------- Mock ----------
    const MOCK_CONFIG = {
        difficulty: "普通",
        gridSize: 5,
        challengeTimeSec: 90,
        theme: "light"
    };

    // ---------- 蜂巢几何参数 ----------
    // 六边形基础尺寸，按难度缩放
    const HEX_BASE_W = 76;     // 基础宽度 px
    const HEX_ASPECT = 1.1547; // 2/√3
    // 蜂巢排列间距
    const H_SPACING_RATIO = 0.72; // 水平间距 / 宽度
    const V_SPACING_RATIO = 0.5;  // 垂直间距 / 高度

    // ---------- 盘面生成 ----------
    function generateNumbers(n) {
        const arr = Array.from({ length: n * n }, (_, i) => i + 1);
        return GameUI.shuffle(arr);
    }

    function renderBoard() {
        board.innerHTML = "";
        const total = gridSize * gridSize;
        // 按难度调整容器大小和基础尺寸
        const scale = gridSize <= 3 ? 1.3 : gridSize <= 5 ? 1.0 : gridSize <= 6 ? 0.85 : 0.72;
        const baseW = Math.round(HEX_BASE_W * scale);
        const baseH = Math.round(baseW * HEX_ASPECT);
        const hSpacing = Math.round(baseW * H_SPACING_RATIO);
        const vSpacing = Math.round(baseH * V_SPACING_RATIO);
        const offsetX = Math.round(hSpacing * 0.5); // 奇数行偏移量

        // 盘面总宽高
        const boardW = (gridSize - 1) * hSpacing + baseW;
        const boardH = (gridSize - 1) * vSpacing + baseH;
        board.style.width = boardW + "px";
        board.style.height = boardH + "px";

        // 分配随机视觉属性
        const colorAssignments = [];
        const sizeAssignments = [];
        const rotAssignments = [];
        const fontAssignments = [];
        for (let i = 0; i < total; i++) {
            colorAssignments.push(COLOR_PALETTE[Math.floor(Math.random() * COLOR_PALETTE.length)]);
            // 大小偏差：±12%，但至少保证数字可见
            const sizeFactor = 1 + (Math.random() - 0.5) * 0.24;
            sizeAssignments.push(sizeFactor);
            // 旋转：约 35% 的格子有 ±5° 旋转
            rotAssignments.push(Math.random() > 0.65 ? (Math.random() - 0.5) * 10 : 0);
            // 字体粗细：混用 700/800/900
            fontAssignments.push([700, 800, 900][Math.floor(Math.random() * 3)]);
        }

        // 为每个数字分配颜色/大小组合，确保相邻不同色（简单去重）
        // 先打乱颜色分配顺序
        GameUI.shuffle(colorAssignments);

        // 放置六边形
        for (let idx = 0; idx < total; idx++) {
            const row = Math.floor(idx / gridSize);
            const col = idx % gridSize;
            const isOddRow = row % 2 === 1;

            const x = col * hSpacing + (isOddRow ? offsetX : 0);
            const y = row * vSpacing;

            // 该格实际尺寸
            const sz = sizeAssignments[idx];
            const w = Math.round(baseW * sz);
            const h = Math.round(baseH * sz);
            // 居中偏移（因为尺寸变化，需居中于标准位置）
            const cx = x + (baseW - w) / 2;
            const cy = y + (baseH - h) / 2;

            const el = document.createElement("div");
            el.className = "cell";
            el.textContent = numbers[idx];
            el.dataset.value = numbers[idx];
            el.dataset.index = idx;

            // 颜色
            el.style.backgroundColor = colorAssignments[idx];
            // 文字颜色：根据背景亮度取白或黑
            el.style.color = isLightColor(colorAssignments[idx]) ? '#1e293b' : '#ffffff';
            // 尺寸
            el.style.width = w + "px";
            el.style.height = h + "px";
            el.style.left = cx + "px";
            el.style.top = cy + "px";
            // 旋转
            const rot = rotAssignments[idx];
            if (Math.abs(rot) > 0.5) {
                el.style.transform = `rotate(${rot}deg)`;
            }
            // 字体粗细
            el.style.fontWeight = fontAssignments[idx];
            // 字体大小：按格子尺寸缩放
            const fontSize = Math.max(11, Math.round(w * 0.38));
            el.style.fontSize = fontSize + "px";

            el.style.animationDelay = (idx * 15) + "ms";

            el.addEventListener("click", () => onCellClick(el, numbers[idx]));
            board.appendChild(el);
        }
    }

    /** 判断颜色亮度（用于决定文字颜色） */
    function isLightColor(hex) {
        const r = parseInt(hex.slice(1, 3), 16);
        const g = parseInt(hex.slice(3, 5), 16);
        const b = parseInt(hex.slice(5, 7), 16);
        return (r * 0.299 + g * 0.587 + b * 0.114) > 160;
    }

    // ---------- 交互 ----------
    function onCellClick(el, value) {
        if (finished) return;
        if (el.classList.contains("done")) return;

        if (!started) {
            started = true;
            startTimer();
        }

        if (value === currentTarget) {
            // 正确——不改变颜色，只加淡标记
            el.classList.add("done");
            // 清除可能存在的旋转（标记后回归）
            el.style.transform = "";
            playSound(true);
            currentTarget++;
            nextTargetEl.textContent = currentTarget;
            updateProgress();

            if (currentTarget > gridSize * gridSize) {
                finishGame(true);
            }
        } else {
            // 错误——无颜色反馈，只轻微抖动 + 计数
            errors++;
            errorsEl.textContent = errors;
            el.classList.add("wrong");
            playSound(false);
            // 给出数字提示
            toast(`当前要找 ${currentTarget}，你点了 ${value}`);
            setTimeout(() => el.classList.remove("wrong"), 400);
        }
    }

    // ---------- 计时 ----------
    function startTimer() {
        const startTime = performance.now();
        timerId = setInterval(() => {
            timerSeconds = (performance.now() - startTime) / 1000;
            timerEl.textContent = timerSeconds.toFixed(1) + "s";
            if (challengeTimeSec > 0 && timerSeconds > challengeTimeSec) {
                toast("⏰ 超时提醒！");
            }
        }, 100);
    }

    function stopTimer() {
        if (timerId) clearInterval(timerId);
        timerId = null;
    }

    // ---------- 进度 ----------
    function updateProgress() {
        const total = gridSize * gridSize;
        const done = currentTarget - 1;
        progressEl.style.width = (total ? (done / total) * 100 : 0) + "%";
    }

    // ---------- 计分 ----------
    function calculateScore() {
        const totalCells = gridSize * gridSize;
        // 基础分与盘面大小正相关
        const baseScore = totalCells * 150;
        // 时间奖励——越快越高
        const timeBonus = Math.max(0, Math.round((totalCells * 2.0 - timerSeconds) * 12));
        // 错误惩罚
        const errorPenalty = errors * 80;
        // 难度系数
        const diffMultiplier = gridSize <= 3 ? 0.8 : gridSize <= 5 ? 1.0 : gridSize <= 6 ? 1.3 : 1.6;
        return Math.max(0, Math.round((baseScore + timeBonus - errorPenalty) * diffMultiplier));
    }

    function getStars() {
        // 按总格数基准时间评价
        const total = gridSize * gridSize;
        const avgTime = gridSize === 5 ? 30 : gridSize === 3 ? 8 : gridSize === 6 ? 45 : 60;
        if (timerSeconds < avgTime * 0.6) return 3;
        if (timerSeconds < avgTime * 1.0) return 2;
        if (timerSeconds < avgTime * 1.6) return 1;
        return 0;
    }

    function renderStars(count) {
        return Array.from({ length: 3 }, (_, i) =>
            `<span class="${i < count ? 'lit' : 'dim'}">★</span>`
        ).join("");
    }

    // ---------- 音效 ----------
    function playSound(correct) {
        try {
            const AC = window.AudioContext || window.webkitAudioContext;
            if (!AC) return;
            if (!window._gameAudioCtx) window._gameAudioCtx = new AC();
            const ctx = window._gameAudioCtx;
            if (ctx.state === "suspended") ctx.resume();
            const now = ctx.currentTime;
            if (correct) {
                // 短促高音
                const osc = ctx.createOscillator();
                const gain = ctx.createGain();
                osc.type = "sine";
                osc.frequency.value = 880;
                gain.gain.setValueAtTime(0.0001, now);
                gain.gain.exponentialRampToValueAtTime(0.08, now + 0.02);
                gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.08);
                osc.connect(gain).connect(ctx.destination);
                osc.start(now);
                osc.stop(now + 0.1);
            } else {
                // 短促低频
                const osc = ctx.createOscillator();
                const gain = ctx.createGain();
                osc.type = "sine";
                osc.frequency.value = 200;
                gain.gain.setValueAtTime(0.0001, now);
                gain.gain.exponentialRampToValueAtTime(0.05, now + 0.02);
                gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.15);
                osc.connect(gain).connect(ctx.destination);
                osc.start(now);
                osc.stop(now + 0.2);
            }
        } catch (e) { /* 静默 */ }
    }

    // ---------- 结算 ----------
    function finishGame(won) {
        if (finished) return;
        finished = true;
        stopTimer();
        score = calculateScore();
        scoreEl.textContent = score;

        const stars = getStars();
        $("resultEmoji").textContent = stars >= 3 ? "🏆" : stars >= 2 ? "🎉" : stars >= 1 ? "😊" : "💪";
        $("resultTitle").textContent = won ? "完成！" : "时间到！";
        $("resultStars").innerHTML = renderStars(stars);
        $("resultScore").textContent = score;
        $("resultTime").textContent = timerSeconds.toFixed(1) + "s";
        $("resultErrors").textContent = errors;
        $("resultGrid").textContent = `${gridSize}×${gridSize}`;
        $("resultOverlay").classList.remove("hidden");

        sendToHost({
            type: "gameEnd",
            mode: "schulte",
            difficulty,
            gridSize,
            timeMs: Math.round(timerSeconds * 1000),
            errors,
            score,
            star: stars
        });
    }

    // ---------- 初始化 ----------
    function boot(data, themeName, meta) {
        difficulty = data.difficulty || "普通";
        gridSize = data.gridSize || 5;
        challengeTimeSec = data.challengeTimeSec || 90;
        applyTheme(themeName);
        numbers = generateNumbers(gridSize);
        resetGame();
        renderBoard();
    }

    function resetGame() {
        currentTarget = 1;
        errors = 0;
        score = 0;
        timerSeconds = 0;
        started = false;
        finished = false;

        nextTargetEl.textContent = "1";
        timerEl.textContent = "0.0s";
        errorsEl.textContent = "0";
        scoreEl.textContent = "0";
        progressEl.style.width = "0%";
        hint.textContent = `在蜂巢中找到数字，按顺序点击 1 → ${gridSize * gridSize}`;
        stopTimer();
    }

    function loadData() {
        listenInit((data, theme, meta) => boot(data, theme, meta));
        if (isMock()) boot(MOCK_CONFIG, "light");
    }

    // 重新开始
    $("btnRestart").addEventListener("click", () => {
        if (window.GameUI.bridge()) { sendToHost({ type: "restart" }); return; }
        numbers = generateNumbers(gridSize);
        resetGame();
        renderBoard();
    });

    $("btnAgain").addEventListener("click", () => {
        $("resultOverlay").classList.add("hidden");
        $("btnRestart").click();
    });

    $("btnClose").addEventListener("click", () => $("resultOverlay").classList.add("hidden"));

    loadData();
})();