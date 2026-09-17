(function () {
    "use strict";

    const { $, toast, sendToHost, listenInit, isMock, applyTheme, shuffle, createTimer, formatTime, renderStars, setProgress } = window.GameUI;

    // ========== 常量 ==========
    const SHAPES = ["●", "■", "▲"];
    const COLORS = ["#E8463A", "#3B82F6", "#1DC981"];
    const COLOR_NAMES = ["红", "蓝", "绿"];
    const SHAPE_NAMES = ["圆", "方", "三角"];

    const MODE_LABELS = {
        dualtask: "双任务反应",
        mot: "多重目标追踪",
        dualcount: "双任务计数"
    };

    // ========== 状态 ==========
    let mode = "dualtask";
    let difficulty = "普通";
    let totalQuestions = 12;
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

    // Audio context for beeps
    let audioCtx = null;

    function getAudioCtx() {
        if (!audioCtx) {
            audioCtx = new (window.AudioContext || window.webkitAudioContext)();
        }
        return audioCtx;
    }

    function playBeep(freq, duration, volume) {
        try {
            var ctx = getAudioCtx();
            var osc = ctx.createOscillator();
            var gain = ctx.createGain();
            osc.connect(gain);
            gain.connect(ctx.destination);
            osc.frequency.value = freq;
            osc.type = "sine";
            gain.gain.setValueAtTime(volume || 0.3, ctx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + duration);
            osc.start(ctx.currentTime);
            osc.stop(ctx.currentTime + duration);
        } catch (e) { /* ignore audio errors */ }
    }

    // ========== 通用 ==========

    function updateStats() {
        scoreEl.textContent = score;
        correctEl.textContent = correctCount;
        roundEl.textContent = currentRound + "/" + totalQuestions;
        setProgress(progressEl, currentRound, totalQuestions);
    }

    // ========== 双任务反应 ==========

    let dtStimuli = [];
    let dtStimulusIdx = 0;
    let dtTargetLabel = null; // e.g. "红圆"
    let dtAudioBeeps = 0; // actual high beeps
    let dtAudioReported = 0; // user's report
    let dtAudioAnswered = false;
    let dtBlockSize = 10;
    let dtBlocksDone = 0;
    let dtHighBeepCount = 0;

    function initDualTask() {
        dtStimuli = [];
        dtStimulusIdx = 0;
        dtBlocksDone = 0;
        dtAudioAnswered = false;
        dtHighBeepCount = 0;

        // 确定目标：随机选一个颜色+形状组合
        var tc = Math.floor(Math.random() * 3);
        var ts = Math.floor(Math.random() * 3);
        dtTargetLabel = { colorIdx: tc, shapeIdx: ts };
        $("dtVisual").style.borderColor = "#6366F1";
        hintEl.textContent = "当看到" + COLOR_NAMES[tc] + SHAPE_NAMES[ts] + "时点击"目标"，否则点击"非目标"；同时数高音哔声次数";

        startDualTaskBlock();
    }

    function startDualTaskBlock() {
        dtStimuli = [];
        dtStimulusIdx = 0;
        dtAudioAnswered = false;

        // 生成刺激序列
        for (var i = 0; i < dtBlockSize; i++) {
            var colorIdx = Math.floor(Math.random() * 3);
            var shapeIdx = Math.floor(Math.random() * 3);
            var isTarget = colorIdx === dtTargetLabel.colorIdx && shapeIdx === dtTargetLabel.shapeIdx;
            var isBeep = Math.random() < 0.4; // 40% 概率有高音哔声
            dtStimuli.push({ colorIdx: colorIdx, shapeIdx: shapeIdx, isTarget: isTarget, isBeep: isBeep });
        }

        dtHighBeepCount = dtStimuli.filter(function(s) { return s.isBeep; }).length;
        $("dtAudioCount").textContent = "计数值：? / ?";
        $("dtFeedback").textContent = "";
        $("dtBtnYes").className = "dt-btn";
        $("dtBtnNo").className = "dt-btn";

        showDualTaskStimulus();
    }

    function showDualTaskStimulus() {
        if (dtStimulusIdx >= dtStimuli.length) {
            // 块结束，询问音频计数
            dtAudioAnswered = true;
            $("dtVisual").style.borderColor = "#F59E0B";
            $("dtStimulus").textContent = "?";
            hintEl.textContent = "本轮结束！高音哔声出现了几次？点击"报告音频数"";
            $("dtFeedback").textContent = "任务A完成！请报告音频任务结果";
            return;
        }

        var s = dtStimuli[dtStimulusIdx];
        $("dtStimulus").textContent = SHAPES[s.shapeIdx];
        $("dtStimulus").style.color = COLORS[s.colorIdx];
        $("dtVisual").style.borderColor = "#6366F1";

        // 播放音频（高音或低音）
        if (s.isBeep) {
            playBeep(1200, 0.2, 0.25);
        } else {
            playBeep(400, 0.2, 0.15);
        }

        // 计时器首次点击时启动
        if (!timer.isRunning()) timer.start();
    }

    function handleDualTaskClick(isTarget) {
        if (questionLocked || finished) return;
        if (dtAudioAnswered) return;
        if (dtStimulusIdx >= dtStimuli.length) return;

        questionLocked = true;
        var s = dtStimuli[dtStimulusIdx];
        var correct = (isTarget && s.isTarget) || (!isTarget && !s.isTarget);

        if (correct) {
            score += 10;
            correctCount++;
            (isTarget ? $("dtBtnYes") : $("dtBtnNo")).classList.add("correct");
            $("dtFeedback").textContent = "✅ 正确！";
        } else {
            score = Math.max(0, score - 5);
            wrongCount++;
            (isTarget ? $("dtBtnYes") : $("dtBtnNo")).classList.add("wrong");
            $("dtFeedback").textContent = "❌ 错误！";
        }

        dtStimulusIdx++;
        currentRound++;
        updateStats();

        setTimeout(function() {
            questionLocked = false;
            $("dtBtnYes").className = "dt-btn";
            $("dtBtnNo").className = "dt-btn";
            if (dtStimulusIdx >= dtStimuli.length) {
                // 等待音频报告
                dtAudioAnswered = true;
                $("dtVisual").style.borderColor = "#F59E0B";
                $("dtStimulus").textContent = "?";
                hintEl.textContent = "本轮结束！高音哔声出现了几次？";
                $("dtFeedback").textContent = "任务A完成！请报告音频计数";
            } else {
                showDualTaskStimulus();
            }
        }, 400);
    }

    function handleDualTaskAudioReport() {
        if (!dtAudioAnswered || finished) return;
        // 模拟用户输入计数（用prompt太干扰，用简单+1/-1按钮+确认）
        // 打开一个简单计数输入
        var reported = prompt("高音哔声出现了几次？（输入数字）", "0");
        if (reported === null) return;
        reported = parseInt(reported) || 0;
        dtAudioReported = reported;

        var audioCorrect = reported === dtHighBeepCount;
        if (audioCorrect) {
            score += 15;
            correctCount++;
            $("dtFeedback").textContent = "🎵 音频正确！共 " + dtHighBeepCount + " 次高音哔声";
        } else {
            score = Math.max(0, score - 5);
            wrongCount++;
            $("dtFeedback").textContent = "🎵 音频错误，实际共 " + dtHighBeepCount + " 次高音哔声，你报了 " + reported;
        }
        $("dtAudioCount").textContent = "实际：" + dtHighBeepCount + " 次";
        updateStats();

        dtBlocksDone++;
        if (currentRound >= totalQuestions || dtBlocksDone >= totalQuestions / dtBlockSize) {
            finishGame();
        } else {
            setTimeout(function() {
                dtAudioAnswered = false;
                startDualTaskBlock();
            }, 1500);
        }
    }

    // ========== 多重目标追踪 ==========

    let motCanvas = null;
    let motCtx = null;
    let motCircles = [];
    let motTargetCount = 3;
    let motSelected = [];
    let motPhase = "init"; // init | highlight | track | select | result
    let motAnimId = null;
    let motTotalCircles = 10;

    function initMOT() {
        motCanvas = $("motCanvas");
        motCtx = motCanvas.getContext("2d");
        motCircles = [];
        motSelected = [];
        motPhase = "init";
        if (motAnimId) { cancelAnimationFrame(motAnimId); motAnimId = null; }

        var size = resizeMotCanvas();
        var margin = 50;
        var w = size.w - margin * 2;
        var h = size.h - margin * 2;

        // 难度调整
        if (difficulty === "简单") { motTotalCircles = 8; motTargetCount = 3; }
        else if (difficulty === "困难") { motTotalCircles = 12; motTargetCount = 5; }
        else { motTotalCircles = 10; motTargetCount = 4; }

        // 生成均匀分布的位置
        var cols = Math.ceil(Math.sqrt(motTotalCircles));
        var rows = Math.ceil(motTotalCircles / cols);
        var cellW = w / cols;
        var cellH = h / rows;
        var positions = [];
        for (var r = 0; r < rows; r++) {
            for (var c = 0; c < cols; c++) {
                if (positions.length >= motTotalCircles) break;
                var cx = margin + c * cellW + cellW / 2 + (Math.random() - 0.5) * cellW * 0.5;
                var cy = margin + r * cellH + cellH / 2 + (Math.random() - 0.5) * cellH * 0.5;
                positions.push({ x: cx, y: cy });
            }
        }
        positions = shuffle(positions);

        // 创建圆圈，前N个为目标
        for (var i = 0; i < motTotalCircles; i++) {
            var speed = 0.5 + Math.random() * 1.5;
            var angle = Math.random() * Math.PI * 2;
            motCircles.push({
                x: positions[i].x,
                y: positions[i].y,
                vx: Math.cos(angle) * speed,
                vy: Math.sin(angle) * speed,
                radius: 14,
                isTarget: i < motTargetCount,
                selected: false,
                matched: false
            });
        }

        // 随机打乱显示顺序但不标记目标，用flash标记
        motCircles = shuffle(motCircles);
        // 前targetCount个设为目标
        for (var i = 0; i < motTargetCount; i++) {
            motCircles[i].isTarget = true;
        }

        $("motTargets").textContent = "目标：" + motTargetCount + " 个";
        $("motSelectInfo").textContent = "观看目标闪烁...";
        $("motActions").classList.add("hidden");
        $("motBtnStart").classList.remove("hidden");
        $("motBtnStart").textContent = "开始追踪";

        if (!timer.isRunning()) timer.start();
        renderMOT();
    }

    function resizeMotCanvas() {
        var wrap = motCanvas.parentElement;
        var w = Math.min(wrap.clientWidth - 4, 700);
        var h = Math.min(wrap.clientHeight - 4, Math.max(400, 380));
        var dpr = window.devicePixelRatio || 1;
        motCanvas.style.width = w + "px";
        motCanvas.style.height = h + "px";
        motCanvas.width = w * dpr;
        motCanvas.height = h * dpr;
        motCtx.setTransform(dpr, 0, 0, dpr, 0, 0);
        return { w: w, h: h };
    }

    function renderMOT() {
        var ctx = motCtx;
        var canvas = motCanvas;
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        for (var i = 0; i < motCircles.length; i++) {
            var c = motCircles[i];
            ctx.save();

            var fillColor = "#fff";
            var strokeColor = "#9CA3AF";
            var lineWidth = 2;

            if (motPhase === "select" || motPhase === "result") {
                if (c.selected) {
                    fillColor = "#6366F1";
                    strokeColor = "#6366F1";
                    lineWidth = 3;
                } else {
                    strokeColor = "#9CA3AF";
                }
                if (motPhase === "result" && c.isTarget) {
                    strokeColor = "#1DC981";
                    lineWidth = 3;
                }
            } else if (motPhase === "highlight" && c.isTarget) {
                fillColor = "#F59E0B";
                strokeColor = "#F59E0B";
                lineWidth = 3;
            }

            ctx.fillStyle = fillColor;
            ctx.strokeStyle = strokeColor;
            ctx.lineWidth = lineWidth;
            ctx.beginPath();
            ctx.arc(c.x, c.y, c.radius, 0, Math.PI * 2);
            ctx.fill();
            ctx.stroke();

            ctx.restore();
        }
    }

    function startMOTTracking() {
        motPhase = "highlight";
        // 闪烁显示目标
        var flashCount = 0;
        var flashInterval = setInterval(function() {
            flashCount++;
            if (flashCount > 5) {
                clearInterval(flashInterval);
                // 开始追踪
                motPhase = "track";
                $("motSelectInfo").textContent = "追踪中...记住目标位置！";
                $("motBtnStart").classList.add("hidden");
                animateMOT();
                return;
            }
            // 闪烁：交替显示目标
            for (var i = 0; i < motCircles.length; i++) {
                if (motCircles[i].isTarget) {
                    motCircles[i].visible = flashCount % 2 === 1;
                }
            }
            renderMOT();
        }, 300);

        // 初始显示
        for (var i = 0; i < motCircles.length; i++) {
            motCircles[i].visible = true;
        }
        renderMOT();
    }

    function animateMOT() {
        var size = resizeMotCanvas();
        var margin = 30;
        var duration = 5000; // 5秒追踪
        if (difficulty === "简单") duration = 6000;
        if (difficulty === "困难") duration = 4000;

        var startTime = Date.now();

        function frame() {
            var elapsed = Date.now() - startTime;

            // 移动所有圆圈
            for (var i = 0; i < motCircles.length; i++) {
                var c = motCircles[i];
                c.x += c.vx;
                c.y += c.vy;

                // 边界反弹
                if (c.x < margin || c.x > size.w - margin) { c.vx *= -1; c.x = Math.max(margin, Math.min(size.w - margin, c.x)); }
                if (c.y < margin || c.y > size.h - margin) { c.vy *= -1; c.y = Math.max(margin, Math.min(size.h - margin, c.y)); }
            }

            renderMOT();

            if (elapsed < duration) {
                motAnimId = requestAnimationFrame(frame);
            } else {
                // 停止，进入选择阶段
                motPhase = "select";
                $("motSelectInfo").textContent = "点击选中你认为是目标的圆圈，然后点击"确认选择"";
                $("motActions").classList.remove("hidden");
                $("motSelected").textContent = "已选 0 个";
                renderMOT();
            }
        }

        motAnimId = requestAnimationFrame(frame);
    }

    function handleMOTClick(e) {
        if (motPhase !== "select") return;
        var rect = motCanvas.getBoundingClientRect();
        var pos = { x: e.clientX - rect.left, y: e.clientY - rect.top };

        for (var i = 0; i < motCircles.length; i++) {
            var c = motCircles[i];
            var dx = pos.x - c.x;
            var dy = pos.y - c.y;
            if (dx * dx + dy * dy < (c.radius + 8) * (c.radius + 8)) {
                c.selected = !c.selected;
                var count = 0;
                for (var j = 0; j < motCircles.length; j++) {
                    if (motCircles[j].selected) count++;
                }
                $("motSelected").textContent = "已选 " + count + " 个";
                renderMOT();
                return;
            }
        }
    }

    function handleMOTConfirm() {
        if (motPhase !== "select") return;
        motPhase = "result";

        // 计算得分
        var truePositives = 0;
        var falsePositives = 0;
        var falseNegatives = 0;
        for (var i = 0; i < motCircles.length; i++) {
            var c = motCircles[i];
            if (c.selected && c.isTarget) truePositives++;
            else if (c.selected && !c.isTarget) falsePositives++;
            else if (!c.selected && c.isTarget) falseNegatives++;
        }

        var tpScore = truePositives * 15;
        var fpPenalty = falsePositives * 5;
        var fnPenalty = falseNegatives * 5;
        var roundScore = Math.max(0, tpScore - fpPenalty - fnPenalty);

        score += roundScore;
        correctCount += truePositives;
        wrongCount += falsePositives + falseNegatives;
        currentRound++;

        $("motSelectInfo").textContent = "正确命中 " + truePositives + "/" + motTargetCount + " 个目标，误选 " + falsePositives + " 个";
        $("motActions").classList.add("hidden");
        renderMOT();
        updateStats();

        if (currentRound >= totalQuestions) {
            setTimeout(finishGame, 1200);
        } else {
            setTimeout(function() {
                motPhase = "init";
                $("motBtnStart").classList.remove("hidden");
                $("motBtnStart").textContent = "下一轮";
                // 重新布局
                initMOT();
            }, 1500);
        }
    }

    // ========== 双任务计数 ==========

    let dcNumbers = [];
    let dcNumIdx = 0;
    let dcEvenPresses = 0;
    let dcActualCount7 = 0;
    let dcLocked = false;
    let dcSequenceLen = 20;

    function initDualCount() {
        dcNumbers = [];
        dcNumIdx = 0;
        dcEvenPresses = 0;
        dcActualCount7 = 0;
        dcLocked = false;

        if (difficulty === "简单") dcSequenceLen = 15;
        else if (difficulty === "困难") dcSequenceLen = 25;
        else dcSequenceLen = 20;

        // 生成数字序列
        for (var i = 0; i < dcSequenceLen; i++) {
            dcNumbers.push(Math.floor(Math.random() * 9) + 1);
        }
        dcActualCount7 = dcNumbers.filter(function(n) { return n === 7; }).length;

        $("dcStimulus").textContent = "准备...";
        $("dcFeedback").textContent = "";
        $("dcReport").classList.add("hidden");
        $("dcBtnClick").classList.remove("hidden");
        $("dcBtnClick").className = "dc-btn";
        $("dcBtnClick").textContent = "等待开始...";
        $("dcBtnClick").disabled = true;

        hintEl.textContent = "任务A：看到偶数时点击按钮；任务B：记住数字7出现了几次";
        dcNumIdx = 0;

        // 3秒倒计时后开始
        var countdown = 3;
        var cdInterval = setInterval(function() {
            $("dcStimulus").textContent = countdown;
            countdown--;
            if (countdown < 0) {
                clearInterval(cdInterval);
                startDualCountSequence();
            }
        }, 700);
    }

    function startDualCountSequence() {
        $("dcBtnClick").disabled = false;
        $("dcBtnClick").textContent = "点击（偶数时按下）";
        showDualCountNumber();
    }

    function showDualCountNumber() {
        if (dcNumIdx >= dcNumbers.length) {
            // 序列结束，显示报告
            $("dcBtnClick").classList.add("hidden");
            $("dcReport").classList.remove("hidden");
            $("dcInputCount").value = "0";
            hintEl.textContent = "输入数字7出现的次数并确认";
            return;
        }

        var num = dcNumbers[dcNumIdx];
        $("dcStimulus").textContent = num;
        $("dcStimulus").style.color = "#1F2937";

        if (!timer.isRunning()) timer.start();
        dcLocked = false;
    }

    function handleDualCountClick() {
        if (dcLocked || finished) return;
        if (dcNumIdx >= dcNumbers.length) return;

        dcLocked = true;
        var num = dcNumbers[dcNumIdx];
        var isEven = num % 2 === 0;

        if (isEven) {
            score += 8;
            correctCount++;
            dcEvenPresses++;
            $("dcStimulus").style.color = "#1DC981";
            $("dcFeedback").textContent = "✅ " + num + " 是偶数";
        } else {
            score = Math.max(0, score - 3);
            wrongCount++;
            $("dcStimulus").style.color = "#E8463A";
            $("dcFeedback").textContent = "❌ " + num + " 不是偶数";
        }

        dcNumIdx++;
        currentRound++;
        updateStats();

        setTimeout(function() {
            if (dcNumIdx >= dcNumbers.length) {
                showDualCountNumber();
            } else {
                showDualCountNumber();
            }
        }, 350);
    }

    function handleDualCountReport() {
        var input = $("dcInputCount");
        var reported = parseInt(input.value) || 0;
        var correct = reported === dcActualCount7;

        if (correct) {
            score += 12;
            correctCount++;
            $("dcFeedback").textContent = "✅ 计数正确！数字7出现了 " + dcActualCount7 + " 次";
        } else {
            score = Math.max(0, score - 5);
            wrongCount++;
            $("dcFeedback").textContent = "❌ 计数错误，实际 " + dcActualCount7 + " 次，你报了 " + reported;
        }

        $("dcReport").classList.add("hidden");
        $("dcBtnClick").classList.remove("hidden");
        $("dcBtnClick").textContent = "完成";
        $("dcBtnClick").disabled = true;
        updateStats();

        if (currentRound >= totalQuestions) {
            setTimeout(finishGame, 1000);
        } else {
            setTimeout(function() {
                initDualCount();
            }, 1500);
        }
    }

    // ========== 结算 ==========

    function finishGame() {
        if (finished) return;
        finished = true;
        timer.stop();
        if (motAnimId) { cancelAnimationFrame(motAnimId); motAnimId = null; }

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

        // 清理事件
        if (motCanvas) {
            motCanvas.removeEventListener("click", handleMOTClick);
        }

        sendToHost({
            type: "gameEnd", mode: "attention", subMode: mode,
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
        if (motAnimId) { cancelAnimationFrame(motAnimId); motAnimId = null; }
        timer.reset();
        $("resultOverlay").classList.add("hidden");
        scoreEl.textContent = "0";
        correctEl.textContent = "0";
        roundEl.textContent = "0";
        timerEl.textContent = "0s";
        setProgress(progressEl, 0, 1);

        var areas = document.querySelectorAll(".mode-area");
        for (var i = 0; i < areas.length; i++) {
            areas[i].classList.add("hidden");
        }
    }

    // ========== 启动 ==========

    function boot(data) {
        mode = data.mode || "dualtask";
        difficulty = data.difficulty || "普通";
        totalQuestions = data.totalQuestions || 12;
        applyTheme(data.theme || "light");

        // 调整块的参数
        if (difficulty === "简单") dtBlockSize = 8;
        else if (difficulty === "困难") dtBlockSize = 14;
        else dtBlockSize = 10;

        // 调整总题数（MOT每个block算一轮，双任务看块数）
        if (mode === "mot") {
            totalQuestions = Math.max(4, Math.min(8, totalQuestions));
        } else if (mode === "dualtask") {
            totalQuestions = Math.max(6, totalQuestions);
        } else if (mode === "dualcount") {
            totalQuestions = Math.max(3, Math.min(6, totalQuestions));
        }

        resetGame();

        modeHint.textContent = "分配注意力 · " + MODE_LABELS[mode] + " · " + difficulty;

        if (mode === "dualtask") {
            $("dualtaskArea").classList.remove("hidden");
            $("dtBtnYes").addEventListener("click", function() { handleDualTaskClick(true); });
            $("dtBtnNo").addEventListener("click", function() { handleDualTaskClick(false); });
            $("dtAudioReport").addEventListener("click", handleDualTaskAudioReport);
            initDualTask();
        } else if (mode === "mot") {
            $("motArea").classList.remove("hidden");
            motCanvas = $("motCanvas");
            motCanvas.addEventListener("click", handleMOTClick);
            $("motBtnStart").addEventListener("click", startMOTTracking);
            $("motBtnConfirm").addEventListener("click", handleMOTConfirm);
            initMOT();
        } else if (mode === "dualcount") {
            $("dualcountArea").classList.remove("hidden");
            $("dcBtnClick").addEventListener("click", handleDualCountClick);
            $("dcBtnReport").addEventListener("click", handleDualCountReport);
            initDualCount();
        }
    }

    listenInit(function(data, theme, meta) { boot(data || {}); });
    if (isMock()) { boot({ mode: "dualtask", difficulty: "普通", totalQuestions: 6 }); }

})();