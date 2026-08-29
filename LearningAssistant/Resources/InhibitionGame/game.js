(function () {
    "use strict";

    const { $, toast, sendToHost, listenInit, isMock, applyTheme, shuffle, createTimer, formatTime, renderStars, setProgress } = window.GameUI;

    // ========== 常量 ==========
    const MODE_NAMES = { goNoGo: "Go-NoGo", reverse: "反向反应", simon: "西蒙说" };
    const SIMON_ACTIONS = ["拍手", "跺脚", "举手", "点头", "摇头"];
    const ARROW_CHARS = { ArrowUp: "↑", ArrowDown: "↓", ArrowLeft: "←", ArrowRight: "→" };
    const OPPOSITE_ARROW = { ArrowUp: "ArrowDown", ArrowDown: "ArrowUp", ArrowLeft: "ArrowRight", ArrowRight: "ArrowLeft" };

    // 难度配置：{ 响应窗口(ms), Go比例, 方向集 }
    const DIFF_GO = { 简单: { window: 2000, goRatio: 0.8 }, 普通: { window: 1500, goRatio: 0.7 }, 困难: { window: 1000, goRatio: 0.6 } };
    const DIFF_REV = { 简单: { window: 2000, dirs: ["ArrowUp", "ArrowDown"] }, 普通: { window: 1500, dirs: ["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight"] }, 困难: { window: 1000, dirs: ["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight"] } };
    const DIFF_SIM = { 简单: { window: 2500, simonRatio: 0.7 }, 普通: { window: 2000, simonRatio: 0.5 }, 困难: { window: 1500, simonRatio: 0.4 } };

    // ========== 状态 ==========
    let mode = "goNoGo";
    let difficulty = "普通";
    let totalQuestions = 20;
    let questions = [];
    let currentIndex = 0;
    let score = 0;
    let correctCount = 0;
    let wrongCount = 0;
    let finished = false;
    let answering = false;
    let responded = false;
    let responseTimer = null;
    let listening = false;

    // ========== DOM ==========
    const stimulusEl = $("stimulus");
    const modeHint = $("modeHint");
    const hintEl = $("hint");
    const keyHint = $("keyHint");
    const simonActions = $("simonActions");
    const scoreEl = $("score");
    const correctEl = $("correct");
    const remainEl = $("remain");
    const timerEl = $("timer");
    const progressEl = $("progress");

    const timer = createTimer({ element: timerEl, interval: 200 });

    // ========== 题目生成 ==========

    function generateGoNoGo() {
        const cfg = DIFF_GO[difficulty] || DIFF_GO.普通;
        const goCount = Math.round(totalQuestions * cfg.goRatio);
        const noGoCount = totalQuestions - goCount;
        const qs = [];
        for (let i = 0; i < goCount; i++) qs.push({ type: "go", expected: "press" });
        for (let i = 0; i < noGoCount; i++) qs.push({ type: "noGo", expected: "noPress" });
        shuffle(qs);
        for (const q of qs) {
            q.stimulus = q.type === "go" ? "✅" : "❌";
            q.cssClass = q.type === "go" ? "go-sign" : "no-sign";
        }
        return qs;
    }

    function generateReverse() {
        const cfg = DIFF_REV[difficulty] || DIFF_REV.普通;
        const dirs = cfg.dirs;
        const qs = [];
        for (let i = 0; i < totalQuestions; i++) {
            const dir = dirs[Math.floor(Math.random() * dirs.length)];
            const opp = OPPOSITE_ARROW[dir];
            qs.push({
                stimulus: ARROW_CHARS[dir],
                expected: ARROW_CHARS[opp],
                expectedKey: opp,
                cssClass: "arrow-" + (dir === "ArrowUp" || dir === "ArrowDown" ? "up" : "left")
            });
        }
        return qs;
    }

    function generateSimon() {
        const cfg = DIFF_SIM[difficulty] || DIFF_SIM.普通;
        const simonCount = Math.round(totalQuestions * cfg.simonRatio);
        const nonSimonCount = totalQuestions - simonCount;
        const qs = [];
        for (let i = 0; i < simonCount; i++) {
            const action = SIMON_ACTIONS[Math.floor(Math.random() * SIMON_ACTIONS.length)];
            qs.push({ type: "simon", action, stimulus: "西蒙说：" + action, expected: "do" });
        }
        for (let i = 0; i < nonSimonCount; i++) {
            const action = SIMON_ACTIONS[Math.floor(Math.random() * SIMON_ACTIONS.length)];
            qs.push({ type: "nonSimon", action, stimulus: action, expected: "noDo" });
        }
        shuffle(qs);
        for (const q of qs) q.cssClass = "simon-cmd";
        return qs;
    }

    const GENERATORS = { goNoGo: generateGoNoGo, reverse: generateReverse, simon: generateSimon };

    // ========== 渲染 ==========

    function renderQuestion() {
        if (finished || currentIndex >= questions.length) {
            finishGame();
            return;
        }

        const q = questions[currentIndex];
        answering = true;
        responded = false;

        // 更新统计
        scoreEl.textContent = score;
        correctEl.textContent = correctCount;
        remainEl.textContent = questions.length - currentIndex;
        setProgress(progressEl, currentIndex, questions.length);

        // 清除响应计时器
        if (responseTimer) { clearTimeout(responseTimer); responseTimer = null; }

        // 设置刺激
        stimulusEl.textContent = q.stimulus;
        stimulusEl.className = "stimulus " + (q.cssClass || "");
        stimulusEl.classList.remove("feedback-correct", "feedback-wrong");

        // 模式提示
        modeHint.textContent = MODE_NAMES[mode] + " · " + difficulty + " · 第 " + (currentIndex + 1) + "/" + questions.length + " 题";

        // 操作提示
        if (mode === "goNoGo") {
            hintEl.textContent = q.type === "go" ? "✅ 请按【空格键】" : "❌ 不要按！";
            keyHint.classList.remove("hidden");
            simonActions.classList.add("hidden");
        } else if (mode === "reverse") {
            hintEl.textContent = "看见 " + q.stimulus + " 请按【" + (q.expected === "↑" ? "上" : q.expected === "↓" ? "下" : q.expected === "←" ? "左" : "右") + "方向键】";
            keyHint.classList.remove("hidden");
            simonActions.classList.add("hidden");
        } else {
            hintEl.textContent = q.type === "simon" ? "听指令！" : "⚠️ 没有"西蒙说"，不要执行！";
            keyHint.classList.add("hidden");
            simonActions.classList.remove("hidden");
            renderSimonButtons(q.action);
        }

        // 启动响应窗口计时器
        const windowMs = getWindowMs();
        if (windowMs > 0) {
            responseTimer = setTimeout(() => {
                if (!responded && answering) {
                    // 超时未响应
                    if (q.expected === "press" || q.expected === "do") {
                        // 需要响应的没响应 → 错误
                        handleResponse(false);
                    } else {
                        // 不需要响应的没响应 → 正确
                        handleResponse(true);
                    }
                }
            }, windowMs);
        }
    }

    function getWindowMs() {
        if (mode === "goNoGo") return (DIFF_GO[difficulty] || DIFF_GO.普通).window;
        if (mode === "reverse") return (DIFF_REV[difficulty] || DIFF_REV.普通).window;
        return (DIFF_SIM[difficulty] || DIFF_SIM.普通).window;
    }

    function renderSimonButtons(activeAction) {
        simonActions.innerHTML = "";
        for (const action of SIMON_ACTIONS) {
            const btn = document.createElement("button");
            btn.className = "simon-btn" + (action === activeAction ? " active" : "");
            btn.textContent = action;
            btn.disabled = !answering;
            btn.addEventListener("click", () => {
                if (!answering || responded) return;
                const q = questions[currentIndex];
                if (q.type === "simon" && action === activeAction) {
                    handleResponse(true);
                } else if (q.type === "nonSimon" && action === activeAction) {
                    // 执行了非西蒙说指令 → 错误
                    handleResponse(false);
                } else if (q.type === "nonSimon") {
                    // 执行了其他动作，但实际也不该执行任何动作
                    handleResponse(false);
                } else if (q.type === "simon" && action !== activeAction) {
                    // 西蒙说但做了错误动作
                    handleResponse(false);
                }
            });
            simonActions.appendChild(btn);
        }
    }

    // ========== 响应处理 ==========

    function handleResponse(isCorrect) {
        if (!answering || responded || finished) return;
        responded = true;
        answering = false;

        if (responseTimer) { clearTimeout(responseTimer); responseTimer = null; }

        if (isCorrect) {
            score += 10;
            correctCount++;
            stimulusEl.classList.add("feedback-correct");
            toast("✅ 正确！");
        } else {
            wrongCount++;
            score = Math.max(0, score - 5);
            stimulusEl.classList.add("feedback-wrong");
            const q = questions[currentIndex];
            let feedback = "❌ ";
            if (mode === "goNoGo") {
                feedback += q.type === "go" ? "需要按空格！" : "不该按空格！";
            } else if (mode === "reverse") {
                feedback += "应该按 " + q.expected;
            } else {
                feedback += q.type === "simon" ? "应该执行指令！" : "没有"西蒙说"，不该执行！";
            }
            toast(feedback);
        }

        scoreEl.textContent = score;
        correctEl.textContent = correctCount;

        // 禁用西蒙说按钮
        if (mode === "simon") {
            simonActions.querySelectorAll(".simon-btn").forEach(b => b.disabled = true);
        }

        // 延迟后进入下一题
        setTimeout(() => {
            currentIndex++;
            renderQuestion();
        }, isCorrect ? 600 : 1200);
    }

    // ========== 键盘事件 ==========

    function onKeyDown(e) {
        if (finished) return;

        if (e.key === "r" || e.key === "R") {
            resetGame();
            renderQuestion();
            return;
        }

        if (!answering || responded) return;

        if (mode === "goNoGo") {
            if (e.key === " ") {
                e.preventDefault();
                const q = questions[currentIndex];
                if (q.type === "go") {
                    handleResponse(true);
                } else {
                    handleResponse(false); // 不该按却按了
                }
            }
        } else if (mode === "reverse") {
            const q = questions[currentIndex];
            if (e.key === q.expectedKey) {
                handleResponse(true);
            } else if (["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight"].includes(e.key)) {
                handleResponse(false); // 按错了方向
            }
        }
    }

    // ========== 结算 ==========

    function finishGame() {
        if (finished) return;
        finished = true;
        answering = false;
        timer.stop();
        stopListening();
        if (responseTimer) { clearTimeout(responseTimer); responseTimer = null; }

        const total = questions.length;
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
            type: "gameEnd",
            mode: "inhibition",
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
        currentIndex = 0;
        score = 0;
        correctCount = 0;
        wrongCount = 0;
        finished = false;
        answering = false;
        responded = false;
        if (responseTimer) { clearTimeout(responseTimer); responseTimer = null; }

        scoreEl.textContent = "0";
        correctEl.textContent = "0";
        remainEl.textContent = questions.length;
        timerEl.textContent = "0s";
        setProgress(progressEl, 0, 1);
        timer.reset();
        stimulusEl.className = "stimulus";
        stimulusEl.textContent = "—";
        $("resultOverlay").classList.add("hidden");
        simonActions.classList.add("hidden");
        keyHint.classList.remove("hidden");
    }

    // ========== 启动 ==========

    function boot(data) {
        mode = data.mode || "goNoGo";
        difficulty = data.difficulty || "普通";
        totalQuestions = data.totalQuestions || 20;
        applyTheme(data.theme || "light");

        modeHint.textContent = MODE_NAMES[mode] + " · " + difficulty;

        const gen = GENERATORS[mode];
        if (gen) questions = gen();
        resetGame();
        startListening();
        renderQuestion();
        timer.start();
    }

    function startListening() {
        if (listening) return;
        listening = true;
        document.addEventListener("keydown", onKeyDown);
    }

    function stopListening() {
        listening = false;
        document.removeEventListener("keydown", onKeyDown);
    }

    // 监听宿主初始化
    listenInit((data, theme, meta) => {
        boot(data || {});
    });

    // 浏览器调试模式
    if (isMock()) {
        boot({
            mode: "goNoGo",
            difficulty: "普通",
            totalQuestions: 10
        });
    }

})();