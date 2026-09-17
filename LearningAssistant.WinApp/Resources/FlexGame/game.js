(function () {
    "use strict";

    const { $, toast, sendToHost, listenInit, isMock, applyTheme, shuffle, createTimer, formatTime, renderStars, setProgress } = window.GameUI;

    // ========== 常量 ==========
    const COLORS = ["#E8463A", "#3B82F6", "#1DC981"];
    const COLOR_NAMES = ["红色", "蓝色", "绿色"];
    const SHAPES = ["●", "■", "▲"];
    const SHAPE_NAMES = ["圆形", "方形", "三角"];
    const COUNTS = [1, 2, 3];
    const COUNT_NAMES = ["1个", "2个", "3个"];

    const MODE_LABELS = {
        cardsort: "卡片分类",
        taskswitch: "任务切换",
        trailmaking: "连线交替"
    };

    // ========== 状态 ==========
    let mode = "cardsort";
    let difficulty = "普通";
    let totalQuestions = 20;
    let score = 0;
    let correctCount = 0;
    let wrongCount = 0;
    let currentRound = 0;
    let finished = false;
    let questionLocked = false;

    // DOM
    const scoreEl = $("score");
    const correctEl = $("correct");
    const roundEl = $("round");
    const timerEl = $("timer");
    const progressEl = $("progress");
    const modeHint = $("modeHint");
    const hintEl = $("hint");
    const timer = createTimer({ element: timerEl, interval: 200 });

    // ========== 卡片分类状态 ==========
    let csRule = "color"; // color | shape | count
    let csTrialsSinceRuleChange = 0;
    let csTrialsPerRule = 5;
    let csCard = null; // { colorIdx, shapeIdx, count }
    let csAnswerIdx = -1;

    // ========== 任务切换状态 ==========
    let tsCueType = "circle"; // circle | square
    let tsTrialsSinceCueChange = 0;
    let tsTrialsPerCueMin = 2;
    let tsTrialsPerCueMax = 4;
    let tsCurrentCueTrials = 3;
    let tsStimulus = 0; // number 1-9 (excluding 5)
    let tsIsSwitchTrial = false;
    let tsCorrectAnswer = "left"; // left | right

    // ========== 连线交替状态 ==========
    let tmDots = [];
    let tmNextIdx = 0; // 0-based index in sequence
    let tmSequence = []; // ["1", "A", "2", "B", ...]
    let tmCanvas = null;
    let tmCtx = null;
    let tmConnectedLines = [];

    // ========== 通用 ==========

    function updateStats() {
        scoreEl.textContent = score;
        correctEl.textContent = correctCount;
        roundEl.textContent = currentRound + "/" + totalQuestions;
        setProgress(progressEl, currentRound, totalQuestions);
    }

    // ========== 卡片分类 ==========

    function generateCard() {
        const colorIdx = Math.floor(Math.random() * 3);
        const shapeIdx = Math.floor(Math.random() * 3);
        const count = COUNTS[Math.floor(Math.random() * 3)];
        return { colorIdx, shapeIdx, count };
    }

    function getRuleAttribute(card, rule) {
        if (rule === "color") return card.colorIdx;
        if (rule === "shape") return card.shapeIdx;
        return card.count - 1; // count 1-3 -> index 0-2
    }

    function getRuleName(rule) {
        if (rule === "color") return "颜色";
        if (rule === "shape") return "形状";
        return "数量";
    }

    function getOptionName(rule, idx) {
        if (rule === "color") return COLOR_NAMES[idx];
        if (rule === "shape") return SHAPE_NAMES[idx];
        return COUNT_NAMES[idx];
    }

    function getOptionPreview(rule, idx) {
        if (rule === "color") return "<span style='color:" + COLORS[idx] + ";font-size:40px'>●</span>";
        if (rule === "shape") return SHAPES[idx];
        // count: show circles
        var html = "";
        for (var i = 0; i < idx + 1; i++) {
            html += "●";
        }
        return html;
    }

    function nextCardSort() {
        if (finished) return;
        questionLocked = false;

        // 检查是否需要切换规则
        csTrialsSinceRuleChange++;
        if (csTrialsSinceRuleChange >= csTrialsPerRule) {
            var oldRule = csRule;
            var rules = ["color", "shape", "count"].filter(function(r) { return r !== oldRule; });
            csRule = rules[Math.floor(Math.random() * rules.length)];
            csTrialsSinceRuleChange = 0;
            csTrialsPerRule = 5 + Math.floor(Math.random() * 2); // 5-6 trials per rule
            var badge = $("csChangeBadge");
            badge.classList.remove("show");
            // Force reflow then show
            void badge.offsetWidth;
            badge.classList.add("show");
            setTimeout(function() { badge.classList.remove("show"); }, 1500);
        }

        // 生成新卡片
        csCard = generateCard();
        var correctAttr = getRuleAttribute(csCard, csRule);

        // 渲染卡片
        var cardHtml = "";
        for (var i = 0; i < csCard.count; i++) {
            cardHtml += "<span style='color:" + COLORS[csCard.colorIdx] + ";font-size:40px'>" + SHAPES[csCard.shapeIdx] + "</span>";
        }
        $("csCardPreview").innerHTML = cardHtml;

        // 生成选项（1 correct + 2 wrong）
        var options = [correctAttr];
        while (options.length < 3) {
            var r = Math.floor(Math.random() * 3);
            if (options.indexOf(r) === -1) options.push(r);
        }
        options = shuffle(options);

        var optionsContainer = $("csOptions");
        optionsContainer.innerHTML = "";
        for (var i = 0; i < options.length; i++) {
            var btn = document.createElement("button");
            btn.className = "cs-option";
            btn.dataset.idx = options[i];
            btn.innerHTML = getOptionName(csRule, options[i]) + " " + getOptionPreview(csRule, options[i]);
            btn.addEventListener("click", handleCardSortClick);
            optionsContainer.appendChild(btn);
        }

        csAnswerIdx = correctAttr;
        $("csRule").textContent = "规则：按" + getRuleName(csRule) + "分类";
        $("csFeedback").textContent = "";
    }

    function handleCardSortClick(e) {
        if (questionLocked || finished) return;
        var btn = e.currentTarget;
        var selected = parseInt(btn.dataset.idx);
        questionLocked = true;

        if (!timer.isRunning()) timer.start();

        if (selected === csAnswerIdx) {
            btn.classList.add("correct");
            score += 10;
            correctCount++;
            $("csFeedback").textContent = "✅ 正确！";
        } else {
            btn.classList.add("wrong");
            // Show correct answer
            var allBtns = $("csOptions").querySelectorAll(".cs-option");
            for (var i = 0; i < allBtns.length; i++) {
                if (parseInt(allBtns[i].dataset.idx) === csAnswerIdx) {
                    allBtns[i].classList.add("correct");
                    break;
                }
            }
            score = Math.max(0, score - 5);
            wrongCount++;
            $("csFeedback").textContent = "❌ 正确答案是 " + getOptionName(csRule, csAnswerIdx);
        }

        currentRound++;
        updateStats();

        setTimeout(function() {
            if (currentRound >= totalQuestions) {
                finishGame();
            } else {
                nextCardSort();
            }
        }, 800);
    }

    // ========== 任务切换 ==========

    function nextTaskSwitch() {
        if (finished) return;
        questionLocked = false;

        // 检查是否需要切换提示
        tsTrialsSinceCueChange++;
        if (tsTrialsSinceCueChange >= tsCurrentCueTrials) {
            tsCueType = tsCueType === "circle" ? "square" : "circle";
            tsTrialsSinceCueChange = 0;
            tsCurrentCueTrials = tsTrialsPerCueMin + Math.floor(Math.random() * (tsTrialsPerCueMax - tsTrialsPerCueMin + 1));
            tsIsSwitchTrial = true;
        } else {
            tsIsSwitchTrial = false;
        }

        // 生成刺激
        var nums = [1, 2, 3, 4, 6, 7, 8, 9];
        tsStimulus = nums[Math.floor(Math.random() * nums.length)];

        // 确定正确答案
        if (tsCueType === "circle") {
            // 奇偶判断
            $("tsTaskLabel").textContent = "判断奇偶";
            tsCorrectAnswer = tsStimulus % 2 === 1 ? "left" : "right";
            $("tsBtnLeft").textContent = "奇数";
            $("tsBtnRight").textContent = "偶数";
        } else {
            // 大小判断
            $("tsTaskLabel").textContent = "判断大小";
            tsCorrectAnswer = tsStimulus > 5 ? "right" : "left";
            $("tsBtnLeft").textContent = "小 (<5)";
            $("tsBtnRight").textContent = "大 (>5)";
        }

        // 更新提示符号
        var cueEl = $("tsCue");
        cueEl.textContent = tsCueType === "circle" ? "○" : "□";
        cueEl.className = "ts-cue " + tsCueType;

        // 更新刺激
        $("tsStimulus").textContent = tsStimulus;
        $("tsFeedback").textContent = "";

        // 重置按钮样式
        $("tsBtnLeft").className = "ts-btn";
        $("tsBtnRight").className = "ts-btn";

        // 显示切换提示
        if (tsIsSwitchTrial) {
            hintEl.textContent = "⚠️ 规则已切换！";
        } else {
            hintEl.textContent = "根据提示符号判断";
        }
    }

    function handleTaskSwitchClick(side) {
        if (questionLocked || finished) return;
        if (!timer.isRunning()) timer.start();
        questionLocked = true;

        var leftBtn = $("tsBtnLeft");
        var rightBtn = $("tsBtnRight");
        var isCorrect = side === tsCorrectAnswer;

        if (isCorrect) {
            var bonus = tsIsSwitchTrial ? 5 : 0;
            score += 10 + bonus;
            correctCount++;
            (side === "left" ? leftBtn : rightBtn).classList.add("correct");
            $("tsFeedback").textContent = tsIsSwitchTrial ? "✅ 正确！切换奖励 +5" : "✅ 正确！";
        } else {
            (side === "left" ? leftBtn : rightBtn).classList.add("wrong");
            score = Math.max(0, score - 5);
            wrongCount++;
            $("tsFeedback").textContent = "❌ 正确答案是" + (tsCorrectAnswer === "left" ? "左边" : "右边");
        }

        currentRound++;
        updateStats();

        setTimeout(function() {
            if (currentRound >= totalQuestions) {
                finishGame();
            } else {
                nextTaskSwitch();
            }
        }, 700);
    }

    // ========== 连线交替 ==========

    function initTrailMaking() {
        tmCanvas = $("trailCanvas");
        tmCtx = tmCanvas.getContext("2d");
        tmConnectedLines = [];
        tmNextIdx = 0;

        var pairCount = totalQuestions <= 12 ? 6 : totalQuestions <= 20 ? 8 : 10;
        tmSequence = [];
        for (var i = 1; i <= pairCount; i++) {
            tmSequence.push(String(i));
            tmSequence.push(String.fromCharCode(64 + i)); // A, B, C...
        }

        resizeTrailCanvas();
        generateTrailDots(pairCount);
        tmNextIdx = 0;
        $("tmNext").textContent = "下一个：1";
        $("tmSwitch").textContent = "模式：数字→字母";
        renderTrailCanvas();
    }

    function resizeTrailCanvas() {
        var wrap = tmCanvas.parentElement;
        var w = Math.min(wrap.clientWidth - 4, 700);
        var h = Math.min(wrap.clientHeight - 4, Math.max(400, 380));
        var dpr = window.devicePixelRatio || 1;
        tmCanvas.style.width = w + "px";
        tmCanvas.style.height = h + "px";
        tmCanvas.width = w * dpr;
        tmCanvas.height = h * dpr;
        tmCtx.setTransform(dpr, 0, 0, dpr, 0, 0);
        return { w: w, h: h };
    }

    function generateTrailDots(pairCount) {
        var total = pairCount * 2;
        var size = resizeTrailCanvas();
        var margin = 60;
        var w = size.w - margin * 2;
        var h = size.h - margin * 2;

        // 使用网格+随机偏移确保均匀分布
        var cols = Math.ceil(Math.sqrt(total));
        var rows = Math.ceil(total / cols);
        var cellW = w / cols;
        var cellH = h / rows;

        tmDots = [];
        var positions = [];
        for (var r = 0; r < rows; r++) {
            for (var c = 0; c < cols; c++) {
                if (positions.length >= total) break;
                var cx = margin + c * cellW + cellW / 2;
                var cy = margin + r * cellH + cellH / 2;
                // 加随机偏移，确保不重叠
                var ox = (Math.random() - 0.5) * cellW * 0.6;
                var oy = (Math.random() - 0.5) * cellH * 0.6;
                positions.push({ x: cx + ox, y: cy + oy });
            }
        }

        // 随机打乱位置
        positions = shuffle(positions);

        for (var i = 0; i < total; i++) {
            tmDots.push({
                label: tmSequence[i],
                x: positions[i].x,
                y: positions[i].y,
                isNumber: i % 2 === 0,
                index: i,
                connected: false
            });
        }
    }

    function renderTrailCanvas() {
        var ctx = tmCtx;
        var canvas = tmCanvas;
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        // 绘制已连接的线
        for (var i = 0; i < tmConnectedLines.length; i++) {
            var line = tmConnectedLines[i];
            ctx.save();
            ctx.strokeStyle = "#6366F1";
            ctx.lineWidth = 3;
            ctx.lineCap = "round";
            ctx.beginPath();
            ctx.moveTo(line.x1, line.y1);
            ctx.lineTo(line.x2, line.y2);
            ctx.stroke();
            ctx.restore();
        }

        // 绘制所有点
        for (var i = 0; i < tmDots.length; i++) {
            var dot = tmDots[i];
            var isNext = i === tmNextIdx;
            var isConnected = dot.connected;

            ctx.save();

            if (isConnected) {
                ctx.fillStyle = "#1DC981";
                ctx.strokeStyle = "#1DC981";
            } else if (isNext) {
                ctx.fillStyle = "#6366F1";
                ctx.strokeStyle = "#6366F1";
            } else {
                ctx.fillStyle = "#fff";
                ctx.strokeStyle = "#9CA3AF";
            }

            ctx.lineWidth = isNext ? 3 : 2;
            var radius = isNext ? 16 : 13;
            ctx.beginPath();
            ctx.arc(dot.x, dot.y, radius, 0, Math.PI * 2);
            ctx.fill();
            ctx.stroke();

            // 标签
            ctx.fillStyle = isConnected ? "#fff" : (isNext ? "#fff" : "#4B5563");
            ctx.font = "bold " + (isNext ? "14px" : "12px") + " sans-serif";
            ctx.textAlign = "center";
            ctx.textBaseline = "middle";
            ctx.fillText(dot.label, dot.x, dot.y);

            ctx.restore();
        }
    }

    function handleTrailCanvasClick(e) {
        if (finished) return;
        var rect = tmCanvas.getBoundingClientRect();
        var pos = { x: e.clientX - rect.left, y: e.clientY - rect.top };

        // 检查点击了哪个点
        for (var i = 0; i < tmDots.length; i++) {
            var dot = tmDots[i];
            if (dot.connected) continue;

            var dx = pos.x - dot.x;
            var dy = pos.y - dot.y;
            var dist = Math.sqrt(dx * dx + dy * dy);

            if (dist < 20) {
                if (i === tmNextIdx) {
                    // 正确点击
                    if (!timer.isRunning()) timer.start();
                    dot.connected = true;

                    // 记录连线
                    if (tmConnectedLines.length > 0) {
                        var last = tmConnectedLines[tmConnectedLines.length - 1];
                        // 连接上一个点和当前点
                        var prevDot = tmDots[tmNextIdx - 1];
                        tmConnectedLines.push({ x1: prevDot.x, y1: prevDot.y, x2: dot.x, y2: dot.y });
                    } else {
                        // 第一个点
                        tmConnectedLines.push({ x1: dot.x, y1: dot.y, x2: dot.x, y2: dot.y });
                    }

                    tmNextIdx++;
                    correctCount++;
                    score += 10;

                    // 更新提示
                    if (tmNextIdx < tmSequence.length) {
                        var nextLabel = tmSequence[tmNextIdx];
                        var isNextNum = tmNextIdx % 2 === 0;
                        $("tmNext").textContent = "下一个：" + nextLabel;
                        $("tmSwitch").textContent = "模式：" + (isNextNum ? "数字→字母" : "字母→数字");
                    }

                    currentRound++;
                    updateStats();
                    renderTrailCanvas();

                    // 检查是否完成
                    if (tmNextIdx >= tmSequence.length) {
                        finishGame();
                    }
                } else {
                    // 错误点击
                    wrongCount++;
                    score = Math.max(0, score - 5);
                    updateStats();

                    // 闪烁错误反馈
                    toast("❌ 请点击 " + tmSequence[tmNextIdx]);
                    ctx = tmCtx;
                    ctx.save();
                    ctx.strokeStyle = "#E8463A";
                    ctx.lineWidth = 3;
                    ctx.beginPath();
                    ctx.arc(dot.x, dot.y, 20, 0, Math.PI * 2);
                    ctx.stroke();
                    ctx.restore();
                    setTimeout(renderTrailCanvas, 400);
                }
                return;
            }
        }
    }

    function handleTrailMove(e) {
        if (finished) return;
        var rect = tmCanvas.getBoundingClientRect();
        var pos = { x: e.clientX - rect.left, y: e.clientY - rect.top };

        var found = false;
        for (var i = 0; i < tmDots.length; i++) {
            var dot = tmDots[i];
            if (dot.connected) continue;
            if (i !== tmNextIdx) continue;

            var dx = pos.x - dot.x;
            var dy = pos.y - dot.y;
            if (Math.sqrt(dx * dx + dy * dy) < 20) {
                found = true;
                break;
            }
        }
        tmCanvas.style.cursor = found ? "pointer" : "default";
    }

    // ========== 结算 ==========

    function finishGame() {
        if (finished) return;
        finished = true;
        timer.stop();
        var elapsed = timer.getElapsed();
        var rate = totalQuestions ? Math.round(correctCount / totalQuestions * 100) : 0;
        var stars = 3;
        if (rate < 80) stars = 2;
        if (rate < 60) stars = 1;
        if (rate < 40) stars = 0;
        var emoji = stars >= 3 ? "🏆" : stars >= 2 ? "🎉" : stars >= 1 ? "😊" : "💪";

        $("resultEmoji").textContent = emoji;
        $("resultTitle").textContent = stars >= 1 ? "完成！" : "继续加油！";
        $("resultStars").innerHTML = renderStars(stars);
        $("resultScore").textContent = score;
        $("resultRate").textContent = rate + "%";
        $("resultRight").textContent = correctCount + "/" + totalQuestions;
        $("resultTime").textContent = formatTime(elapsed);
        $("resultMode").textContent = MODE_LABELS[mode] || mode;
        $("resultOverlay").classList.remove("hidden");

        // 移除事件
        if (mode === "trailmaking") {
            tmCanvas.removeEventListener("click", handleTrailCanvasClick);
            tmCanvas.removeEventListener("mousemove", handleTrailMove);
        }

        sendToHost({
            type: "gameEnd", mode: "flex", subMode: mode,
            difficulty: difficulty,
            timeMs: Math.round(elapsed * 1000), score: score,
            correct: correctCount, errors: wrongCount,
            total: totalQuestions, star: stars
        });
    }

    // ========== 重置 ==========

    function resetGame() {
        finished = false;
        score = 0;
        correctCount = 0;
        wrongCount = 0;
        currentRound = 0;
        questionLocked = false;
        timer.reset();
        $("resultOverlay").classList.add("hidden");
        scoreEl.textContent = "0";
        correctEl.textContent = "0";
        roundEl.textContent = "0";
        timerEl.textContent = "0s";
        setProgress(progressEl, 0, 1);

        // 隐藏所有模式区域
        var areas = document.querySelectorAll(".mode-area");
        for (var i = 0; i < areas.length; i++) {
            areas[i].classList.add("hidden");
        }
    }

    // ========== 启动 ==========

    function boot(data) {
        mode = data.mode || "cardsort";
        difficulty = data.difficulty || "普通";
        totalQuestions = data.totalQuestions || 20;
        applyTheme(data.theme || "light");

        // 不同难度调整
        if (difficulty === "简单") {
            csTrialsPerRule = 6;
            tsTrialsPerCueMin = 3;
            tsTrialsPerCueMax = 5;
        } else if (difficulty === "困难") {
            csTrialsPerRule = 4;
            tsTrialsPerCueMin = 2;
            tsTrialsPerCueMax = 3;
        } else {
            csTrialsPerRule = 5;
            tsTrialsPerCueMin = 2;
            tsTrialsPerCueMax = 4;
        }

        resetGame();

        modeHint.textContent = "认知灵活性 · " + MODE_LABELS[mode] + " · " + difficulty;

        if (mode === "cardsort") {
            $("cardsortArea").classList.remove("hidden");
            csRule = "color";
            csTrialsSinceRuleChange = 0;
            hintEl.textContent = "根据上方的规则，选择正确的分类";
            nextCardSort();
        } else if (mode === "taskswitch") {
            $("taskswitchArea").classList.remove("hidden");
            tsCueType = "circle";
            tsTrialsSinceCueChange = 0;
            tsCurrentCueTrials = 3;
            tsIsSwitchTrial = false;
            hintEl.textContent = "根据提示符号判断，点击左右按钮";
            nextTaskSwitch();

            // 绑定按钮事件
            $("tsBtnLeft").addEventListener("click", function() { handleTaskSwitchClick("left"); });
            $("tsBtnRight").addEventListener("click", function() { handleTaskSwitchClick("right"); });
        } else if (mode === "trailmaking") {
            $("trailmakingArea").classList.remove("hidden");
            hintEl.textContent = "按照数字→字母交替顺序点击";
            initTrailMaking();

            tmCanvas.addEventListener("click", handleTrailCanvasClick);
            tmCanvas.addEventListener("mousemove", handleTrailMove);
        }
    }

    listenInit(function(data, theme, meta) { boot(data || {}); });
    if (isMock()) { boot({ mode: "cardsort", difficulty: "普通", totalQuestions: 12 }); }

})();