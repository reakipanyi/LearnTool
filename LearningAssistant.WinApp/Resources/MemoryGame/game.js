(function () {
    "use strict";

    const { $, toast, sendToHost, listenInit, isMock, applyTheme, shuffle, createTimer, formatTime, renderStars, setProgress } = window.GameUI;

    // ========== 常量 ==========
    const MODE_NAMES = { nback: "N-Back", digitSpan: "数字倒背", spatial: "空间矩阵", sequence: "序列操作" };
    const SEQ_ACTIONS = ["拍手", "跺脚", "举手", "点头", "摇头", "转圈", "弯腰"];
    const N_BACK_CATEGORIES = "0123456789".split("");

    // 难度配置
    const DIFF = {
        nback: { 简单: { n: 1, stimuli: 20, showMs: 1000 }, 普通: { n: 2, stimuli: 25, showMs: 800 }, 困难: { n: 3, stimuli: 30, showMs: 700 } },
        digitSpan: { 简单: { minSpan: 3, maxSpan: 6, trials: 10 }, 普通: { minSpan: 3, maxSpan: 8, trials: 12 }, 困难: { minSpan: 3, maxSpan: 10, trials: 15 } },
        spatial: { 简单: { gridSize: 4, minLights: 2, maxLights: 5, trials: 10 }, 普通: { gridSize: 5, minLights: 3, maxLights: 7, trials: 12 }, 困难: { gridSize: 6, minLights: 4, maxLights: 9, trials: 15 } },
        sequence: { 简单: { minSteps: 2, maxSteps: 5, trials: 10 }, 普通: { minSteps: 2, maxSteps: 7, trials: 12 }, 困难: { minSteps: 2, maxSteps: 9, trials: 15 } }
    };

    // ========== 状态 ==========
    let mode = "nback";
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

    // 模式区域
    const areas = {
        nback: $("nbackArea"),
        digitSpan: $("digitSpanArea"),
        spatial: $("spatialArea"),
        sequence: $("sequenceArea")
    };

    // ========== 工具 ==========

    function showArea(m) {
        Object.keys(areas).forEach(k => areas[k].classList.toggle("hidden", k !== m));
    }

    function randInt(min, max) {
        return Math.floor(Math.random() * (max - min + 1)) + min;
    }

    function randItem(arr) {
        return arr[Math.floor(Math.random() * arr.length)];
    }

    function updateStats() {
        scoreEl.textContent = score;
        correctEl.textContent = correctCount;
    }

    function setRound(cur, total) {
        roundEl.textContent = cur + "/" + total;
        setProgress(progressEl, cur, total);
    }

    // ========== N-Back ==========

    let nback = {
        n: 1, stimuli: [], currentIdx: 0, total: 0, showMs: 1000,
        timerId: null, responding: false, hitCount: 0, totalMatches: 0, listening: false
    };

    function initNBack(cfg) {
        nback.n = cfg.n;
        nback.total = cfg.stimuli;
        nback.showMs = cfg.showMs;
        nback.stimuli = generateNBackStimuli(cfg.stimuli, cfg.n);
        nback.currentIdx = 0;
        nback.hitCount = 0;
        nback.totalMatches = 0;
        // 计算总匹配数
        for (let i = 0; i < cfg.stimuli; i++) {
            if (nback.stimuli[i].isMatch) nback.totalMatches++;
        }
        totalTrials = cfg.stimuli;
        showArea("nback");
        hintEl.textContent = "当前匹配 n=" + cfg.n + "，看到与往前第 " + cfg.n + " 个相同的数字请按空格";
        renderNBackHistory();
        scheduleNextNBack();
    }

    function generateNBackStimuli(count, n) {
        const seq = [];
        for (let i = 0; i < count; i++) {
            if (i < n) {
                seq.push({ value: randItem(N_BACK_CATEGORIES), isMatch: false });
            } else {
                // 30% 概率为匹配项
                const isMatch = Math.random() < 0.3;
                if (isMatch) {
                    seq.push({ value: seq[i - n].value, isMatch: true });
                } else {
                    let v;
                    do { v = randItem(N_BACK_CATEGORIES); } while (v === seq[i - n].value);
                    seq.push({ value: v, isMatch: false });
                }
            }
        }
        return seq;
    }

    function renderNBackHistory() {
        const el = $("nbackHistory");
        el.innerHTML = "";
        const start = Math.max(0, nback.currentIdx - nback.n - 2);
        const end = nback.currentIdx;
        for (let i = start; i < end; i++) {
            const d = document.createElement("span");
            d.className = "h-item" + (i === nback.currentIdx - 1 ? " active" : "");
            if (i >= 0) d.textContent = nback.stimuli[i].value;
            else d.textContent = "·";
            el.appendChild(d);
        }
    }

    function scheduleNextNBack() {
        if (finished || nback.currentIdx >= nback.total) {
            finishGame();
            return;
        }

        const s = nback.stimuli[nback.currentIdx];
        const el = $("nbackStimulus");
        el.textContent = s.value;
        el.style.color = s.isMatch ? "#E8463A" : "var(--text, #171717)";
        nback.responding = true;

        // 更新提示
        const remaining = nback.total - nback.currentIdx - 1;
        modeHint.textContent = "N-Back · " + difficulty + " · n=" + nback.n + " · 第 " + (nback.currentIdx + 1) + "/" + nback.total;
        hintEl.textContent = s.isMatch ? "🔴 匹配！按空格" : "不匹配，等待下一个";
        updateStats();
        setRound(nback.currentIdx, nback.total);

        // 自动进入下一题
        nback.timerId = setTimeout(() => {
            if (nback.responding) {
                nback.responding = false;
                // 没响应
                if (s.isMatch) {
                    // 漏了匹配项
                    wrongCount++;
                    score = Math.max(0, score - 5);
                    toast("⏰ 漏了匹配！");
                }
                advanceNBack();
            }
        }, nback.showMs);
    }

    function advanceNBack() {
        nback.currentIdx++;
        renderNBackHistory();
        // 小间隔后显示下一个
        setTimeout(() => scheduleNextNBack(), 300);
    }

    function handleNBackKey(e) {
        if (e.key === " " || e.key === "Space") {
            e.preventDefault();
            if (!nback.responding || finished) return;
            nback.responding = false;
            if (nback.timerId) { clearTimeout(nback.timerId); nback.timerId = null; }

            const s = nback.stimuli[nback.currentIdx];
            if (s.isMatch) {
                score += 10;
                correctCount++;
                nback.hitCount++;
                toast("✅ 正确！");
            } else {
                wrongCount++;
                score = Math.max(0, score - 5);
                toast("❌ 误报！");
            }
            advanceNBack();
        }
    }

    function startNBackListening() {
        if (nback.listening) return;
        nback.listening = true;
        document.addEventListener("keydown", handleNBackKey);
    }

    function stopNBackListening() {
        nback.listening = false;
        document.removeEventListener("keydown", handleNBackKey);
    }

    // ========== 数字倒背 ==========

    let digitSpan = {
        span: 3, minSpan: 3, maxSpan: 6, trials: 10, trialIdx: 0,
        digits: [], userInput: [], inputIdx: 0, displaying: false, showTimerId: null,
        consecutiveFails: 0
    };

    function initDigitSpan(cfg) {
        digitSpan.minSpan = cfg.minSpan;
        digitSpan.maxSpan = cfg.maxSpan;
        digitSpan.trials = cfg.trials;
        digitSpan.span = cfg.minSpan;
        digitSpan.trialIdx = 0;
        digitSpan.consecutiveFails = 0;
        totalTrials = cfg.trials;
        showArea("digitSpan");
        hintEl.textContent = "记住数字序列，然后按相反顺序点击输入";
        buildNumpad();
        startDigitTrial();
    }

    function buildNumpad() {
        const el = $("numpad");
        el.innerHTML = "";
        const nums = [1, 2, 3, 4, 5, 6, 7, 8, 9, null, 0, null];
        nums.forEach(n => {
            if (n === null) {
                const sp = document.createElement("div");
                sp.style.width = "60px";
                el.appendChild(sp);
                return;
            }
            const btn = document.createElement("button");
            btn.className = "np-btn";
            btn.textContent = n;
            btn.addEventListener("click", () => handleDigitInput(n));
            el.appendChild(btn);
        });
        // 退格和确认
        const bk = document.createElement("button");
        bk.className = "np-btn";
        bk.textContent = "⌫";
        bk.addEventListener("click", () => handleDigitBackspace());
        el.appendChild(bk);
        const ok = document.createElement("button");
        ok.className = "np-btn wide";
        ok.textContent = "确认";
        ok.addEventListener("click", () => handleDigitConfirm());
        el.appendChild(ok);
    }

    function startDigitTrial() {
        if (finished || digitSpan.trialIdx >= digitSpan.trials) {
            finishGame();
            return;
        }

        digitSpan.displaying = true;
        const len = digitSpan.span;
        digitSpan.digits = Array.from({ length: len }, () => randInt(0, 9));
        digitSpan.userInput = [];
        digitSpan.inputIdx = 0;

        modeHint.textContent = "数字倒背 · " + difficulty + " · 位数 " + len + " · 第 " + (digitSpan.trialIdx + 1) + "/" + digitSpan.trials;
        hintEl.textContent = "记住数字…";
        updateStats();
        setRound(digitSpan.trialIdx, digitSpan.trials);

        // 逐个显示数字
        const display = $("digitDisplay");
        display.innerHTML = "";
        let idx = 0;
        const showNext = () => {
            if (idx >= len) {
                // 显示完毕，进入输入阶段
                digitSpan.displaying = false;
                display.innerHTML = "? ".repeat(len).trim();
                hintEl.textContent = "按相反顺序输入数字";
                renderDigitInput();
                return;
            }
            display.innerHTML = "";
            for (let i = 0; i <= idx; i++) {
                const sp = document.createElement("span");
                sp.className = "d-char";
                sp.textContent = digitSpan.digits[i];
                display.appendChild(sp);
            }
            idx++;
            digitSpan.showTimerId = setTimeout(showNext, 700);
        };
        digitSpan.showTimerId = setTimeout(showNext, 500);
    }

    function renderDigitInput() {
        const el = $("digitInput");
        el.innerHTML = "";
        for (let i = 0; i < digitSpan.span; i++) {
            const d = document.createElement("div");
            d.className = "d-answer";
            if (i < digitSpan.inputIdx) d.textContent = digitSpan.userInput[i];
            else if (i === digitSpan.inputIdx) d.style.borderColor = "var(--brand, #4B3FE3)";
            el.appendChild(d);
        }
    }

    function handleDigitInput(n) {
        if (digitSpan.displaying || finished) return;
        digitSpan.userInput.push(n);
        digitSpan.inputIdx++;
        renderDigitInput();
        if (digitSpan.inputIdx >= digitSpan.span) {
            handleDigitConfirm();
        }
    }

    function handleDigitBackspace() {
        if (digitSpan.displaying || finished || digitSpan.inputIdx === 0) return;
        digitSpan.userInput.pop();
        digitSpan.inputIdx--;
        renderDigitInput();
    }

    function handleDigitConfirm() {
        if (digitSpan.displaying || finished || digitSpan.inputIdx === 0) return;
        // 补全未输入
        while (digitSpan.inputIdx < digitSpan.span) {
            digitSpan.userInput.push(-1);
            digitSpan.inputIdx++;
        }

        const expected = [...digitSpan.digits].reverse();
        const correct = digitSpan.userInput.every((v, i) => v === expected[i]);

        if (correct) {
            score += 10;
            correctCount++;
            digitSpan.consecutiveFails = 0;
            if (digitSpan.span < digitSpan.maxSpan) digitSpan.span++;
            toast("✅ 正确！位数提升至 " + digitSpan.span);
        } else {
            wrongCount++;
            score = Math.max(0, score - 5);
            digitSpan.consecutiveFails++;
            if (digitSpan.consecutiveFails >= 2 && digitSpan.span > digitSpan.minSpan) {
                digitSpan.span--;
                digitSpan.consecutiveFails = 0;
            }
            toast("❌ 正确答案：" + expected.join(" "));
        }

        updateStats();
        digitSpan.trialIdx++;
        setTimeout(() => startDigitTrial(), correct ? 800 : 1500);
    }

    // ========== 空间矩阵 ==========

    let spatial = {
        gridSize: 4, minLights: 2, maxLights: 5, trials: 10, trialIdx: 0,
        lights: [], selected: [], phase: "idle", consecutiveFails: 0, curLights: 2
    };

    function initSpatial(cfg) {
        spatial.gridSize = cfg.gridSize;
        spatial.minLights = cfg.minLights;
        spatial.maxLights = cfg.maxLights;
        spatial.trials = cfg.trials;
        spatial.curLights = cfg.minLights;
        spatial.trialIdx = 0;
        spatial.consecutiveFails = 0;
        totalTrials = cfg.trials;
        showArea("spatial");
        buildSpatialGrid();
        hintEl.textContent = "记住亮起的位置";
        startSpatialTrial();
    }

    function buildSpatialGrid() {
        const el = $("spatialGrid");
        el.innerHTML = "";
        const size = spatial.gridSize;
        el.style.gridTemplateColumns = "repeat(" + size + ", 48px)";
        for (let i = 0; i < size * size; i++) {
            const cell = document.createElement("button");
            cell.className = "cell";
            cell.dataset.idx = i;
            cell.addEventListener("click", () => handleSpatialClick(i));
            el.appendChild(cell);
        }
    }

    function startSpatialTrial() {
        if (finished || spatial.trialIdx >= spatial.trials) {
            finishGame();
            return;
        }

        spatial.phase = "show";
        const size = spatial.gridSize;
        const count = spatial.curLights;
        const all = Array.from({ length: size * size }, (_, i) => i);
        shuffle(all);
        spatial.lights = all.slice(0, count);
        spatial.selected = [];

        modeHint.textContent = "空间矩阵 · " + difficulty + " · " + size + "×" + size + " · 亮 " + count + " 个 · 第 " + (spatial.trialIdx + 1) + "/" + spatial.trials;
        hintEl.textContent = "记住亮起的位置…";
        updateStats();
        setRound(spatial.trialIdx, spatial.trials);

        // 清空所有格子
        const cells = $("spatialGrid").querySelectorAll(".cell");
        cells.forEach(c => { c.className = "cell"; c.disabled = true; });

        // 逐个点亮
        let idx = 0;
        const lightNext = () => {
            if (idx >= count) {
                // 全部亮完，短暂停留后熄灭
                setTimeout(() => {
                    cells.forEach(c => c.className = "cell");
                    spatial.phase = "recall";
                    cells.forEach(c => c.disabled = false);
                    hintEl.textContent = "点击亮起的位置（共 " + count + " 个）";
                }, 600);
                return;
            }
            const ci = spatial.lights[idx];
            cells[ci].classList.add("lit");
            idx++;
            setTimeout(lightNext, 400);
        };
        setTimeout(lightNext, 300);
    }

    function handleSpatialClick(idx) {
        if (spatial.phase !== "recall" || finished) return;
        const cells = $("spatialGrid").querySelectorAll(".cell");
        if (cells[idx].classList.contains("selected") || cells[idx].classList.contains("wrong")) return;

        if (spatial.lights.includes(idx)) {
            cells[idx].classList.add("selected");
            spatial.selected.push(idx);
            // 检查是否已全部选完
            if (spatial.selected.length === spatial.lights.length) {
                spatial.phase = "done";
                score += 10;
                correctCount++;
                spatial.consecutiveFails = 0;
                if (spatial.curLights < spatial.maxLights) spatial.curLights++;
                toast("✅ 全部正确！增加至 " + spatial.curLights + " 个");
                cells.forEach(c => c.disabled = true);
                updateStats();
                spatial.trialIdx++;
                setTimeout(() => startSpatialTrial(), 800);
            }
        } else {
            // 点错了
            cells[idx].classList.add("wrong");
            spatial.phase = "done";
            wrongCount++;
            score = Math.max(0, score - 5);
            spatial.consecutiveFails++;
            if (spatial.consecutiveFails >= 2 && spatial.curLights > spatial.minLights) {
                spatial.curLights--;
                spatial.consecutiveFails = 0;
            }
            cells.forEach(c => c.disabled = true);
            // 显示正确位置
            spatial.lights.forEach(li => cells[li].classList.add("lit"));
            updateStats();
            toast("❌ 点错了！");
            spatial.trialIdx++;
            setTimeout(() => startSpatialTrial(), 1500);
        }
    }

    // ========== 序列操作 ==========

    let sequence = {
        minSteps: 2, maxSteps: 5, trials: 10, trialIdx: 0,
        steps: [], userSteps: [], phase: "idle", stepIdx: 0,
        consecutiveFails: 0, curSteps: 2
    };

    function initSequence(cfg) {
        sequence.minSteps = cfg.minSteps;
        sequence.maxSteps = cfg.maxSteps;
        sequence.trials = cfg.trials;
        sequence.curSteps = cfg.minSteps;
        sequence.trialIdx = 0;
        sequence.consecutiveFails = 0;
        totalTrials = cfg.trials;
        showArea("sequence");
        buildSeqButtons();
        hintEl.textContent = "记住动作顺序";
        startSeqTrial();
    }

    function buildSeqButtons() {
        const el = $("seqActions");
        el.innerHTML = "";
        SEQ_ACTIONS.forEach(a => {
            const btn = document.createElement("button");
            btn.className = "sq-btn";
            btn.textContent = a;
            btn.dataset.action = a;
            btn.addEventListener("click", () => handleSeqClick(a));
            el.appendChild(btn);
        });
    }

    function startSeqTrial() {
        if (finished || sequence.trialIdx >= sequence.trials) {
            finishGame();
            return;
        }

        sequence.phase = "show";
        const count = sequence.curSteps;
        sequence.steps = Array.from({ length: count }, () => randItem(SEQ_ACTIONS));
        sequence.userSteps = [];
        sequence.stepIdx = 0;

        modeHint.textContent = "序列操作 · " + difficulty + " · " + count + " 步 · 第 " + (sequence.trialIdx + 1) + "/" + sequence.trials;
        hintEl.textContent = "记住动作顺序…";
        updateStats();
        setRound(sequence.trialIdx, sequence.trials);

        const display = $("seqDisplay");
        const buttons = $("seqActions").querySelectorAll(".sq-btn");
        buttons.forEach(b => b.disabled = true);

        // 逐个高亮显示动作
        let idx = 0;
        const showNext = () => {
            if (idx >= count) {
                // 显示完毕，进入回忆阶段
                setTimeout(() => {
                    display.innerHTML = "? → ".repeat(count).replace(/ → $/, "");
                    sequence.phase = "recall";
                    sequence.stepIdx = 0;
                    buttons.forEach(b => b.disabled = false);
                    hintEl.textContent = "按顺序点击动作";
                    renderSeqProgress();
                }, 600);
                return;
            }
            // 高亮当前按钮
            buttons.forEach(b => b.classList.toggle("highlight", b.dataset.action === sequence.steps[idx]));
            display.innerHTML = sequence.steps.slice(0, idx + 1).map(s => `<span class="s-char">${s}</span>`).join(" → ");
            idx++;
            setTimeout(showNext, 800);
        };
        setTimeout(showNext, 500);
    }

    function renderSeqProgress() {
        const el = $("seqProgress");
        el.innerHTML = "";
        const count = sequence.curSteps;
        for (let i = 0; i < count; i++) {
            const d = document.createElement("div");
            d.className = "sp-dot" + (i < sequence.stepIdx ? " filled" : "");
            el.appendChild(d);
        }
    }

    function handleSeqClick(action) {
        if (sequence.phase !== "recall" || finished) return;
        sequence.userSteps.push(action);
        sequence.stepIdx++;

        // 高亮点击的按钮
        const buttons = $("seqActions").querySelectorAll(".sq-btn");
        buttons.forEach(b => b.classList.toggle("highlight", b.dataset.action === action));
        setTimeout(() => {
            buttons.forEach(b => b.classList.remove("highlight"));
        }, 200);

        renderSeqProgress();

        if (sequence.stepIdx >= sequence.curSteps) {
            // 检查是否全部正确
            const correct = sequence.userSteps.every((v, i) => v === sequence.steps[i]);
            sequence.phase = "done";
            buttons.forEach(b => b.disabled = true);

            if (correct) {
                score += 10;
                correctCount++;
                sequence.consecutiveFails = 0;
                if (sequence.curSteps < sequence.maxSteps) sequence.curSteps++;
                toast("✅ 顺序正确！增加至 " + sequence.curSteps + " 步");
            } else {
                wrongCount++;
                score = Math.max(0, score - 5);
                sequence.consecutiveFails++;
                if (sequence.consecutiveFails >= 2 && sequence.curSteps > sequence.minSteps) {
                    sequence.curSteps--;
                    sequence.consecutiveFails = 0;
                }
                toast("❌ 正确顺序：" + sequence.steps.join(" → "));
            }
            updateStats();
            sequence.trialIdx++;
            setTimeout(() => startSeqTrial(), correct ? 800 : 1500);
        }
    }

    // ========== 结算 ==========

    function finishGame() {
        if (finished) return;
        finished = true;
        timer.stop();
        stopAllListening();

        const total = totalTrials;
        const rate = total ? Math.round(correctCount / total * 100) : 0;
        let stars = 3;
        if (rate < 80) stars = 2;
        if (rate < 60) stars = 1;
        if (rate < 40) stars = 0;
        const elapsed = timer.getElapsed();
        const emoji = stars >= 3 ? "🏆" : stars >= 2 ? "🎉" : stars >= 1 ? "😊" : "💪";

        // 最佳成绩
        let best = "—";
        if (mode === "nback") best = "命中 " + nback.hitCount + "/" + nback.totalMatches;
        else if (mode === "digitSpan") best = "最大位数 " + digitSpan.span;
        else if (mode === "spatial") best = "最大 " + spatial.curLights + " 个";
        else if (mode === "sequence") best = "最大 " + sequence.curSteps + " 步";

        $("resultEmoji").textContent = emoji;
        $("resultTitle").textContent = stars >= 1 ? "完成！" : "继续加油！";
        $("resultStars").innerHTML = renderStars(stars);
        $("resultScore").textContent = score;
        $("resultRate").textContent = rate + "%";
        $("resultRight").textContent = correctCount + "/" + total;
        $("resultBest").textContent = best;
        $("resultTime").textContent = formatTime(elapsed);
        $("resultMode").textContent = MODE_NAMES[mode] + " · " + difficulty;
        $("resultOverlay").classList.remove("hidden");

        sendToHost({
            type: "gameEnd",
            mode: "memory",
            subMode: mode,
            difficulty,
            timeMs: Math.round(elapsed * 1000),
            score,
            correct: correctCount,
            errors: wrongCount,
            total,
            star: stars
        });
    }

    // ========== 重置 ==========

    function resetGame() {
        finished = false;
        score = 0;
        correctCount = 0;
        wrongCount = 0;
        totalTrials = 0;
        stopAllListening();
        if (nback.timerId) { clearTimeout(nback.timerId); nback.timerId = null; }
        if (digitSpan.showTimerId) { clearTimeout(digitSpan.showTimerId); digitSpan.showTimerId = null; }
        timer.reset();
        $("resultOverlay").classList.add("hidden");
        scoreEl.textContent = "0";
        correctEl.textContent = "0";
        roundEl.textContent = "0/0";
        timerEl.textContent = "0s";
        setProgress(progressEl, 0, 1);
    }

    function stopAllListening() {
        stopNBackListening();
    }

    // ========== 键盘事件 ==========

    function onGlobalKeyDown(e) {
        if (e.key === "r" || e.key === "R") {
            const cfg = DIFF[mode][difficulty] || DIFF[mode]["普通"];
            resetGame();
            startMode(mode, cfg);
            return;
        }
        // N-Back 键盘
        if (mode === "nback") handleNBackKey(e);
    }

    // ========== 启动 ==========

    function startMode(m, cfg) {
        switch (m) {
            case "nback": initNBack(cfg); break;
            case "digitSpan": initDigitSpan(cfg); break;
            case "spatial": initSpatial(cfg); break;
            case "sequence": initSequence(cfg); break;
        }
    }

    function boot(data) {
        mode = data.mode || "nback";
        difficulty = data.difficulty || "普通";
        applyTheme(data.theme || "light");

        const cfg = DIFF[mode][difficulty] || DIFF[mode]["普通"];
        modeHint.textContent = MODE_NAMES[mode] + " · " + difficulty;
        resetGame();
        startMode(mode, cfg);
        timer.start();
        document.addEventListener("keydown", onGlobalKeyDown);
    }

    listenInit((data, theme, meta) => {
        boot(data || {});
    });

    if (isMock()) {
        boot({ mode: "nback", difficulty: "普通", totalQuestions: 10 });
    }

})();