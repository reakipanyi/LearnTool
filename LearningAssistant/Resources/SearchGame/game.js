(function () {
    "use strict";

    const { $, toast, sendToHost, listenInit, isMock, applyTheme, shuffle, createTimer, formatTime, renderStars, setProgress } = window.GameUI;

    // ========== 常量 ==========
    const MODE_NAMES = { cancel: "动态划消", dualSearch: "双重搜索", layered: "分层干扰", saccade: "扫视挑战" };
    const SYMBOLS = ["★", "●", "▲", "■", "◆", "♥", "♦", "♣", "♠", "✿", "✦", "⬟"];
    const COLORS = ["#E8463A", "#1DC981", "#4B3FE3", "#F5A623", "#9B59B6", "#1ABC9C"];
    const COLOR_NAMES = ["红", "绿", "蓝", "黄", "紫", "青"];
    const SHAPES = ["●", "■", "▲", "◆", "★", "⬟"];

    // 难度配置
    const DIFF = {
        cancel: { 简单: { gridSize: 4, targets: 2, timeLimit: 10, rounds: 8 }, 普通: { gridSize: 5, targets: 3, timeLimit: 8, rounds: 10 }, 困难: { gridSize: 6, targets: 4, timeLimit: 6, rounds: 12 } },
        dualSearch: { 简单: { gridSize: 4, items: 8, rounds: 8 }, 普通: { gridSize: 5, items: 12, rounds: 10 }, 困难: { gridSize: 6, items: 16, rounds: 12 } },
        layered: { 简单: { gridSize: 4, rounds: 8 }, 普通: { gridSize: 5, rounds: 10 }, 困难: { gridSize: 6, rounds: 12 } },
        saccade: { 简单: { gridSize: 5, targets: 15, timeoutMs: 3000 }, 普通: { gridSize: 6, targets: 20, timeoutMs: 2000 }, 困难: { gridSize: 7, targets: 25, timeoutMs: 1500 } }
    };

    // ========== 状态 ==========
    let mode = "cancel";
    let difficulty = "普通";
    let finished = false;
    let score = 0;
    let correctCount = 0;
    let wrongCount = 0;
    let totalTrials = 0;

    // DOM
    const modeHint = $("modeHint");
    const hintEl = $("hint");
    const scoreEl = $("score");
    const correctEl = $("correct");
    const roundEl = $("round");
    const timerEl = $("timer");
    const progressEl = $("progress");
    const timer = createTimer({ element: timerEl, interval: 200 });

    const areas = { cancel: $("cancelArea"), dualSearch: $("dualSearchArea"), layered: $("layeredArea"), saccade: $("saccadeArea") };

    function showArea(m) { Object.keys(areas).forEach(k => areas[k].classList.toggle("hidden", k !== m)); }
    function randInt(min, max) { return Math.floor(Math.random() * (max - min + 1)) + min; }
    function randItem(arr) { return arr[Math.floor(Math.random() * arr.length)]; }
    function updateStats() { scoreEl.textContent = score; correctEl.textContent = correctCount; }
    function setRound(cur, total) { roundEl.textContent = cur + "/" + total; setProgress(progressEl, cur, total); }

    // ========== 动态划消 ==========

    let cancel = { cfg: null, roundIdx: 0, grid: [], targets: [], found: 0, totalTargets: 0, roundTimer: null };

    function initCancel(cfg) {
        cancel.cfg = cfg;
        cancel.roundIdx = 0;
        totalTrials = cfg.rounds;
        showArea("cancel");
        buildCancelGrid();
        startCancelRound();
    }

    function buildCancelGrid() {
        const el = $("cancelGrid");
        el.innerHTML = "";
        const size = cancel.cfg.gridSize;
        el.style.gridTemplateColumns = "repeat(" + size + ", 52px)";
        for (let i = 0; i < size * size; i++) {
            const cell = document.createElement("button");
            cell.className = "cell";
            cell.dataset.idx = i;
            cell.addEventListener("click", () => handleCancelClick(i));
            el.appendChild(cell);
        }
    }

    function startCancelRound() {
        if (finished || cancel.roundIdx >= cancel.cfg.rounds) { finishGame(); return; }
        const cfg = cancel.cfg;
        const size = cfg.gridSize;
        const total = size * size;

        // 选择目标符号
        const shuffled = shuffle([...SYMBOLS]);
        cancel.targets = shuffled.slice(0, cfg.targets);
        cancel.found = 0;
        cancel.totalTargets = 0;

        // 生成网格
        cancel.grid = [];
        const allSymbols = SYMBOLS.slice(0, 6);
        for (let i = 0; i < total; i++) {
            let sym = randItem(allSymbols);
            if (cancel.targets.includes(sym)) cancel.totalTargets++;
            cancel.grid.push(sym);
        }

        // 确保至少有一个目标
        if (cancel.totalTargets === 0) {
            const idx = randInt(0, total - 1);
            cancel.grid[idx] = randItem(cancel.targets);
            cancel.totalTargets = 1;
        }

        modeHint.textContent = "动态划消 · " + difficulty + " · 第 " + (cancel.roundIdx + 1) + "/" + cfg.rounds;
        $("cancelTargets").textContent = cancel.targets.join(" ");
        hintEl.textContent = "点击目标符号，共 " + cancel.totalTargets + " 个";
        updateStats();
        setRound(cancel.roundIdx, cfg.rounds);

        renderCancelGrid();

        // 时间限制
        if (cancel.roundTimer) clearTimeout(cancel.roundTimer);
        cancel.roundTimer = setTimeout(() => {
            // 超时，未找到的算漏掉
            const missed = cancel.totalTargets - cancel.found;
            if (missed > 0) {
                wrongCount += missed;
                score = Math.max(0, score - missed * 5);
                toast("⏰ 漏了 " + missed + " 个！");
                updateStats();
            }
            advanceCancel();
        }, cfg.timeLimit * 1000);
    }

    function renderCancelGrid() {
        const cells = $("cancelGrid").querySelectorAll(".cell");
        const size = cancel.cfg.gridSize;
        cells.forEach((c, i) => {
            c.textContent = cancel.grid[i];
            c.className = "cell";
            c.disabled = false;
            // 已被找到的目标高亮
            if (cancel.targets.includes(cancel.grid[i]) && cancel.grid[i] === "__found") {
                c.className = "cell hit";
                c.disabled = true;
            }
        });
    }

    function handleCancelClick(idx) {
        if (finished || cancel.roundTimer === null) return;
        const cells = $("cancelGrid").querySelectorAll(".cell");
        if (cells[idx].disabled) return;

        const sym = cancel.grid[idx];
        if (cancel.targets.includes(sym)) {
            cells[idx].className = "cell hit";
            cells[idx].disabled = true;
            cancel.grid[idx] = "__found";
            cancel.found++;
            score += 10;
            correctCount++;
            updateStats();
            if (cancel.found >= cancel.totalTargets) {
                if (cancel.roundTimer) clearTimeout(cancel.roundTimer);
                toast("✅ 全部找到！");
                setTimeout(() => advanceCancel(), 500);
            }
        } else {
            cells[idx].className = "cell miss";
            wrongCount++;
            score = Math.max(0, score - 5);
            updateStats();
            setTimeout(() => { cells[idx].className = "cell"; }, 300);
            toast("❌ 不是目标！");
        }
    }

    function advanceCancel() {
        cancel.roundIdx++;
        if (cancel.roundTimer) { clearTimeout(cancel.roundTimer); cancel.roundTimer = null; }
        startCancelRound();
    }

    // ========== 双重搜索 ==========

    let dual = { cfg: null, roundIdx: 0, grid: [], targets: [], found: 0, totalTargets: 0 };

    function initDualSearch(cfg) {
        dual.cfg = cfg;
        dual.roundIdx = 0;
        totalTrials = cfg.rounds;
        showArea("dualSearch");
        startDualRound();
    }

    function startDualRound() {
        if (finished || dual.roundIdx >= dual.cfg.rounds) { finishGame(); return; }
        const cfg = dual.cfg;
        const size = cfg.gridSize;
        const total = size * size;

        // 两个目标：一个颜色，一个形状
        const colorTarget = randItem(COLOR_NAMES);
        const shapeTarget = randItem(SHAPES);
        dual.targets = { color: colorTarget, shape: shapeTarget };

        // 生成网格
        dual.grid = [];
        dual.found = 0;
        dual.totalTargets = 0;
        for (let i = 0; i < total; i++) {
            const shape = randItem(SHAPES);
            const colorIdx = randInt(0, COLOR_NAMES.length - 1);
            const color = COLOR_NAMES[colorIdx];
            dual.grid.push({ shape, color, isTarget: shape === shapeTarget || color === colorTarget });
            if (shape === shapeTarget || color === colorTarget) dual.totalTargets++;
        }

        modeHint.textContent = "双重搜索 · " + difficulty + " · 第 " + (dual.roundIdx + 1) + "/" + cfg.rounds;
        hintEl.textContent = "找到 " + dual.totalTargets + " 个目标";
        updateStats();
        setRound(dual.roundIdx, cfg.rounds);

        // 显示双目标
        $("dualTargets").innerHTML = `
            <div class="dt-item"><span class="dt-icon">●</span> 颜色：${colorTarget}</div>
            <div class="dt-item"><span class="dt-icon">${shapeTarget}</span> 形状：${shapeTarget}</div>
        `;

        renderDualGrid();
    }

    function renderDualGrid() {
        const el = $("dualGrid");
        el.innerHTML = "";
        const size = dual.cfg.gridSize;
        el.style.gridTemplateColumns = "repeat(" + size + ", 52px)";
        dual.grid.forEach((item, i) => {
            const cell = document.createElement("button");
            cell.className = "cell" + (item._found ? " hit" : "");
            cell.textContent = item.shape;
            cell.style.color = COLORS[COLOR_NAMES.indexOf(item.color)];
            cell.disabled = !!item._found;
            cell.dataset.idx = i;
            cell.addEventListener("click", () => handleDualClick(i));
            el.appendChild(cell);
        });
    }

    function handleDualClick(idx) {
        if (finished) return;
        const item = dual.grid[idx];
        if (item._found) return;

        const cells = $("dualGrid").querySelectorAll(".cell");
        if (item.isTarget) {
            cells[idx].className = "cell hit";
            cells[idx].disabled = true;
            item._found = true;
            dual.found++;
            score += 10;
            correctCount++;
            updateStats();
            if (dual.found >= dual.totalTargets) {
                toast("✅ 全部找到！");
                setTimeout(() => advanceDual(), 500);
            }
        } else {
            cells[idx].className = "cell miss";
            wrongCount++;
            score = Math.max(0, score - 5);
            updateStats();
            setTimeout(() => { cells[idx].className = "cell"; }, 300);
            toast("❌ 不是目标！");
        }
    }

    function advanceDual() {
        dual.roundIdx++;
        startDualRound();
    }

    // ========== 分层干扰 ==========

    let layered = { cfg: null, roundIdx: 0, grid: [], target: "", totalTargets: 0, found: 0 };

    function initLayered(cfg) {
        layered.cfg = cfg;
        layered.roundIdx = 0;
        totalTrials = cfg.rounds;
        showArea("layered");
        buildLayeredGrid();
        startLayeredRound();
    }

    function buildLayeredGrid() {
        const el = $("layeredGrid");
        el.innerHTML = "";
        const size = layered.cfg.gridSize;
        el.style.gridTemplateColumns = "repeat(" + size + ", 60px)";
        for (let i = 0; i < size * size; i++) {
            const cell = document.createElement("button");
            cell.className = "lcell";
            cell.dataset.idx = i;
            cell.addEventListener("click", () => handleLayeredClick(i));
            el.appendChild(cell);
        }
    }

    function startLayeredRound() {
        if (finished || layered.roundIdx >= layered.cfg.rounds) { finishGame(); return; }
        const cfg = layered.cfg;
        const size = cfg.gridSize;
        const total = size * size;

        layered.target = randItem(SYMBOLS.slice(0, 6));
        layered.found = 0;
        layered.totalTargets = 0;

        // 生成网格：每个格子有 top 和 bottom 符号
        layered.grid = [];
        const allSymbols = SYMBOLS.slice(0, 8);
        for (let i = 0; i < total; i++) {
            let top = randItem(allSymbols);
            let bottom = randItem(allSymbols);
            // 避免 top 和 bottom 相同（太容易）
            while (bottom === top) bottom = randItem(allSymbols);
            layered.grid.push({ top, bottom });
            if (top === layered.target) layered.totalTargets++;
        }

        // 确保至少一个目标
        if (layered.totalTargets === 0) {
            const idx = randInt(0, total - 1);
            layered.grid[idx].top = layered.target;
            layered.totalTargets = 1;
        }

        modeHint.textContent = "分层干扰 · " + difficulty + " · 第 " + (layered.roundIdx + 1) + "/" + cfg.rounds;
        $("layeredTarget").textContent = layered.target;
        $("layeredHint").textContent = "忽略底层，找上层符号（共 " + layered.totalTargets + " 个）";
        hintEl.textContent = "点击上层符号为 " + layered.target + " 的格子";
        updateStats();
        setRound(layered.roundIdx, cfg.rounds);

        renderLayeredGrid();
    }

    function renderLayeredGrid() {
        const cells = $("layeredGrid").querySelectorAll(".lcell");
        cells.forEach((c, i) => {
            const item = layered.grid[i];
            if (item._found) {
                c.className = "lcell selected";
                c.disabled = true;
                c.innerHTML = `<span class="top-symbol">${item.top}</span><span class="bottom-symbol">${item.bottom}</span>`;
            } else {
                c.className = "lcell";
                c.disabled = false;
                c.innerHTML = `<span class="top-symbol">${item.top}</span><span class="bottom-symbol">${item.bottom}</span>`;
            }
        });
    }

    function handleLayeredClick(idx) {
        if (finished) return;
        const item = layered.grid[idx];
        if (item._found) return;

        const cells = $("layeredGrid").querySelectorAll(".lcell");
        if (item.top === layered.target) {
            cells[idx].className = "lcell selected";
            cells[idx].disabled = true;
            item._found = true;
            layered.found++;
            score += 10;
            correctCount++;
            updateStats();
            if (layered.found >= layered.totalTargets) {
                toast("✅ 全部找到！");
                setTimeout(() => advanceLayered(), 500);
            }
        } else {
            cells[idx].className = "lcell wrong";
            wrongCount++;
            score = Math.max(0, score - 5);
            updateStats();
            setTimeout(() => { cells[idx].className = "lcell"; }, 400);
            toast("❌ 上层符号不是 " + layered.target);
        }
    }

    function advanceLayered() {
        layered.roundIdx++;
        startLayeredRound();
    }

    // ========== 扫视挑战 ==========

    let saccade = { cfg: null, targetIdx: 0, totalTargets: 0, currentPos: -1, timeoutId: null };

    function initSaccade(cfg) {
        saccade.cfg = cfg;
        saccade.targetIdx = 0;
        saccade.totalTargets = cfg.targets;
        totalTrials = cfg.targets;
        showArea("saccade");
        buildSaccadeGrid();
        showSaccadeTarget();
    }

    function buildSaccadeGrid() {
        const el = $("saccadeGrid");
        el.innerHTML = "";
        const size = saccade.cfg.gridSize;
        el.style.gridTemplateColumns = "repeat(" + size + ", 64px)";
        for (let i = 0; i < size * size; i++) {
            const cell = document.createElement("button");
            cell.className = "scell";
            cell.dataset.idx = i;
            cell.addEventListener("click", () => handleSaccadeClick(i));
            el.appendChild(cell);
        }
    }

    function showSaccadeTarget() {
        if (finished || saccade.targetIdx >= saccade.totalTargets) { finishGame(); return; }
        const cfg = saccade.cfg;
        const size = cfg.gridSize;
        const total = size * size;

        // 随机选择一个位置
        let pos;
        do { pos = randInt(0, total - 1); } while (pos === saccade.currentPos);
        saccade.currentPos = pos;

        modeHint.textContent = "扫视挑战 · " + difficulty + " · 第 " + (saccade.targetIdx + 1) + "/" + saccade.totalTargets;
        hintEl.textContent = "快速找到 ★ 并点击！";
        updateStats();
        setRound(saccade.targetIdx, saccade.totalTargets);

        const cells = $("saccadeGrid").querySelectorAll(".scell");
        cells.forEach(c => { c.className = "scell"; c.textContent = ""; c.disabled = false; });
        cells[pos].className = "scell has-target";
        cells[pos].textContent = "★";

        // 超时
        if (saccade.timeoutId) clearTimeout(saccade.timeoutId);
        saccade.timeoutId = setTimeout(() => {
            cells[pos].className = "scell missed";
            cells[pos].textContent = "★";
            wrongCount++;
            score = Math.max(0, score - 5);
            updateStats();
            toast("⏰ 超时！");
            setTimeout(() => { saccade.targetIdx++; showSaccadeTarget(); }, 600);
        }, cfg.timeoutMs);
    }

    function handleSaccadeClick(idx) {
        if (finished) return;
        const cells = $("saccadeGrid").querySelectorAll(".scell");
        if (cells[idx].disabled) return;

        if (idx === saccade.currentPos) {
            if (saccade.timeoutId) clearTimeout(saccade.timeoutId);
            cells[idx].className = "scell found";
            cells[idx].textContent = "★";
            cells[idx].disabled = true;
            score += 10;
            correctCount++;
            updateStats();
            saccade.targetIdx++;
            setTimeout(() => showSaccadeTarget(), 400);
        } else {
            cells[idx].className = "scell missed";
            cells[idx].textContent = "✕";
            wrongCount++;
            score = Math.max(0, score - 5);
            updateStats();
            setTimeout(() => { cells[idx].className = "scell"; cells[idx].textContent = ""; }, 300);
            toast("❌ 点错了！");
        }
    }

    // ========== 结算 ==========

    function finishGame() {
        if (finished) return;
        finished = true;
        timer.stop();
        if (cancel.roundTimer) { clearTimeout(cancel.roundTimer); cancel.roundTimer = null; }
        if (saccade.timeoutId) { clearTimeout(saccade.timeoutId); saccade.timeoutId = null; }

        const total = totalTrials;
        const rate = total ? Math.round(correctCount / total * 100) : 0;
        let stars = 3;
        if (rate < 80) stars = 2;
        if (rate < 60) stars = 1;
        if (rate < 40) stars = 0;
        const elapsed = timer.getElapsed();
        const emoji = stars >= 3 ? "🏆" : stars >= 2 ? "🎉" : stars >= 1 ? "😊" : "💪";

        $("resultEmoji").textContent = emoji;
        $("resultTitle").textContent = stars >= 1 ? "完成！" : "继续加油！";
        $("resultStars").innerHTML = renderStars(stars);
        $("resultScore").textContent = score;
        $("resultRate").textContent = rate + "%";
        $("resultRight").textContent = correctCount + "/" + total;
        $("resultTime").textContent = formatTime(elapsed);
        $("resultMode").textContent = MODE_NAMES[mode] + " · " + difficulty;
        $("resultOverlay").classList.remove("hidden");

        sendToHost({
            type: "gameEnd", mode: "search", subMode: mode, difficulty,
            timeMs: Math.round(elapsed * 1000), score, correct: correctCount,
            errors: wrongCount, total, star: stars
        });
    }

    // ========== 重置 ==========

    function resetGame() {
        finished = false;
        score = 0;
        correctCount = 0;
        wrongCount = 0;
        totalTrials = 0;
        if (cancel.roundTimer) { clearTimeout(cancel.roundTimer); cancel.roundTimer = null; }
        if (saccade.timeoutId) { clearTimeout(saccade.timeoutId); saccade.timeoutId = null; }
        timer.reset();
        $("resultOverlay").classList.add("hidden");
        scoreEl.textContent = "0";
        correctEl.textContent = "0";
        roundEl.textContent = "0/0";
        timerEl.textContent = "0s";
        setProgress(progressEl, 0, 1);
    }

    // ========== 启动 ==========

    function startMode(m, cfg) {
        switch (m) {
            case "cancel": initCancel(cfg); break;
            case "dualSearch": initDualSearch(cfg); break;
            case "layered": initLayered(cfg); break;
            case "saccade": initSaccade(cfg); break;
        }
    }

    function boot(data) {
        mode = data.mode || "cancel";
        difficulty = data.difficulty || "普通";
        applyTheme(data.theme || "light");

        const cfg = DIFF[mode][difficulty] || DIFF[mode]["普通"];
        modeHint.textContent = MODE_NAMES[mode] + " · " + difficulty;
        resetGame();
        startMode(mode, cfg);
        timer.start();
    }

    listenInit((data, theme, meta) => { boot(data || {}); });
    if (isMock()) { boot({ mode: "cancel", difficulty: "普通" }); }
})();