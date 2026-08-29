(function () {
    "use strict";

    const { $, toast, sendToHost, listenInit, isMock, applyTheme, shuffle, createTimer, formatTime, renderStars, setProgress } = window.GameUI;

    // ========== 常量 ==========
    const MODE_LABELS = {
        pathplan: "路径规划",
        hanoi: "汉诺塔",
        timeest: "时间估计"
    };

    // ========== 音效系统 ==========
    let audioCtx = null;

    function getAudioCtx() {
        if (!audioCtx) {
            try { audioCtx = new (window.AudioContext || window.webkitAudioContext)(); } catch (e) {}
        }
        return audioCtx;
    }

    function playTone(freq, duration, type, volume) {
        try {
            var ctx = getAudioCtx();
            if (!ctx) return;
            var osc = ctx.createOscillator();
            var gain = ctx.createGain();
            osc.connect(gain);
            gain.connect(ctx.destination);
            osc.frequency.value = freq;
            osc.type = type || "sine";
            gain.gain.setValueAtTime(volume || 0.2, ctx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + duration);
            osc.start(ctx.currentTime);
            osc.stop(ctx.currentTime + duration);
        } catch (e) {}
    }

    function playCorrect() {
        playTone(523, 0.12, "sine", 0.2);
        setTimeout(function() { playTone(659, 0.12, "sine", 0.2); }, 80);
        setTimeout(function() { playTone(784, 0.18, "sine", 0.22); }, 160);
    }

    function playWrong() {
        playTone(400, 0.15, "square", 0.12);
        setTimeout(function() { playTone(300, 0.25, "square", 0.12); }, 120);
    }

    function playClick() {
        playTone(800, 0.06, "sine", 0.08);
    }

    function playStep() {
        playTone(600, 0.05, "sine", 0.06);
    }

    function playBacktrack() {
        playTone(350, 0.08, "sine", 0.06);
    }

    function playMove() {
        playTone(440, 0.08, "triangle", 0.1);
    }

    function playPlace() {
        playTone(550, 0.1, "sine", 0.12);
    }

    function playComplete() {
        var notes = [523, 587, 659, 698, 784, 880, 988, 1047];
        for (var i = 0; i < notes.length; i++) {
            (function(n, d) {
                setTimeout(function() { playTone(n, 0.15, "sine", 0.18); }, d);
            })(notes[i], i * 70);
        }
    }

    function playDistract() {
        playTone(1000, 0.08, "sawtooth", 0.05);
    }

    function playTimerTick() {
        playTone(1200, 0.03, "sine", 0.04);
    }

    // ========== 动画系统 ==========

    function animateElement(el, className, duration) {
        if (!el) return;
        el.classList.add(className);
        setTimeout(function() {
            el.classList.remove(className);
        }, duration || 500);
    }

    function animateCanvasFlash(ctx, x, y, radius, color, duration) {
        var start = performance.now();
        function frame() {
            var elapsed = performance.now() - start;
            var progress = elapsed / (duration || 300);
            if (progress >= 1) return;
            ctx.save();
            ctx.globalAlpha = 1 - progress;
            ctx.strokeStyle = color || "#6366F1";
            ctx.lineWidth = 3 * (1 - progress);
            ctx.beginPath();
            ctx.arc(x, y, radius * (1 + progress * 0.5), 0, Math.PI * 2);
            ctx.stroke();
            ctx.restore();
            requestAnimationFrame(frame);
        }
        requestAnimationFrame(frame);
    }

    function animatePathLine(ctx, path, cs, color, duration, callback) {
        var start = performance.now();
        var totalLen = path.length - 1;
        function frame() {
            var elapsed = performance.now() - start;
            var progress = Math.min(1, elapsed / (duration || 400));
            var drawTo = Math.floor(progress * totalLen);
            var subProgress = (progress * totalLen) - drawTo;

            ctx.save();
            ctx.strokeStyle = color || "#6366F1";
            ctx.lineWidth = 4;
            ctx.lineCap = "round";
            ctx.lineJoin = "round";
            ctx.beginPath();
            ctx.moveTo(path[0].c * cs + cs / 2, path[0].r * cs + cs / 2);
            for (var i = 1; i <= drawTo && i < path.length; i++) {
                ctx.lineTo(path[i].c * cs + cs / 2, path[i].r * cs + cs / 2);
            }
            if (drawTo < totalLen && drawTo + 1 < path.length) {
                var p1 = path[drawTo];
                var p2 = path[drawTo + 1];
                var px = p1.c * cs + cs / 2 + (p2.c - p1.c) * cs * subProgress;
                var py = p1.r * cs + cs / 2 + (p2.r - p1.r) * cs * subProgress;
                ctx.lineTo(px, py);
            }
            ctx.stroke();
            ctx.restore();

            if (progress < 1) {
                requestAnimationFrame(frame);
            } else if (callback) {
                callback();
            }
        }
        requestAnimationFrame(frame);
    }

    function animateScorePopup(x, y, text, color) {
        var el = document.createElement("div");
        el.className = "score-popup";
        el.textContent = text;
        el.style.left = x + "px";
        el.style.top = y + "px";
        el.style.color = color || "#1DC981";
        document.body.appendChild(el);
        setTimeout(function() {
            el.classList.add("score-popup-fade");
            setTimeout(function() { el.remove(); }, 400);
        }, 50);
        setTimeout(function() { el.remove(); }, 600);
    }

    // ========== 状态 ==========
    let mode = "pathplan";
    let difficulty = "普通";
    let totalQuestions = 8;
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

    function updateStats() {
        scoreEl.textContent = score;
        correctEl.textContent = correctCount;
        roundEl.textContent = currentRound + "/" + totalQuestions;
        setProgress(progressEl, currentRound, totalQuestions);
    }

    // ========== 路径规划 ==========

    let ppCanvas = null;
    let ppCtx = null;
    let ppGrid = [];
    let ppRows = 8;
    let ppCols = 8;
    let ppStart = { r: 0, c: 0 };
    let ppEnd = { r: 0, c: 0 };
    let ppPath = [];
    let ppCellSize = 0;
    let ppObstacleRatio = 0.25;
    let ppPhase = "build"; // build | result
    let ppAnimating = false;

    function initPathPlan() {
        ppCanvas = $("ppCanvas");
        ppCtx = ppCanvas.getContext("2d");
        ppPath = [];
        ppPhase = "build";
        ppAnimating = false;

        if (difficulty === "简单") { ppRows = 6; ppCols = 6; ppObstacleRatio = 0.2; }
        else if (difficulty === "困难") { ppRows = 10; ppCols = 10; ppObstacleRatio = 0.3; }
        else { ppRows = 8; ppCols = 8; ppObstacleRatio = 0.25; }

        resizePPCanvas();
        generateGrid();
        $("ppSteps").textContent = "步数：0";
        $("ppBtnClear").classList.remove("hidden");
        $("ppBtnConfirm").classList.remove("hidden");
        $("ppBtnConfirm").textContent = "确认路径";
        renderPP();
    }

    function resizePPCanvas() {
        var wrap = ppCanvas.parentElement;
        var maxW = Math.min(wrap.clientWidth - 4, 520);
        var maxH = Math.min(wrap.clientHeight - 4, 520);
        var size = Math.min(maxW, maxH);
        var dpr = window.devicePixelRatio || 1;
        ppCanvas.style.width = size + "px";
        ppCanvas.style.height = size + "px";
        ppCanvas.width = size * dpr;
        ppCanvas.height = size * dpr;
        ppCtx.setTransform(dpr, 0, 0, dpr, 0, 0);
        ppCellSize = size / Math.max(ppRows, ppCols);
        return { w: size, h: size };
    }

    function generateGrid() {
        ppGrid = [];
        for (var r = 0; r < ppRows; r++) {
            ppGrid[r] = [];
            for (var c = 0; c < ppCols; c++) {
                ppGrid[r][c] = 0;
            }
        }

        ppStart = { r: 0, c: 0 };
        ppEnd = { r: ppRows - 1, c: ppCols - 1 };

        var totalCells = ppRows * ppCols;
        var obstacleCount = Math.floor(totalCells * ppObstacleRatio);
        var placed = 0;
        while (placed < obstacleCount) {
            var r = Math.floor(Math.random() * ppRows);
            var c = Math.floor(Math.random() * ppCols);
            if ((r === ppStart.r && c === ppStart.c) || (r === ppEnd.r && c === ppEnd.c)) continue;
            if (Math.abs(r - ppStart.r) <= 1 && Math.abs(c - ppStart.c) <= 1) continue;
            if (Math.abs(r - ppEnd.r) <= 1 && Math.abs(c - ppEnd.c) <= 1) continue;
            if (ppGrid[r][c] === 0) {
                ppGrid[r][c] = 1;
                placed++;
            }
        }

        if (!hasPathBFS()) {
            var obstacles = [];
            for (var r = 0; r < ppRows; r++) {
                for (var c = 0; c < ppCols; c++) {
                    if (ppGrid[r][c] === 1) obstacles.push({ r: r, c: c });
                }
            }
            obstacles = shuffle(obstacles);
            for (var i = 0; i < obstacles.length; i++) {
                ppGrid[obstacles[i].r][obstacles[i].c] = 0;
                if (hasPathBFS()) break;
            }
        }
    }

    function hasPathBFS() {
        var visited = {};
        var queue = [{ r: ppStart.r, c: ppStart.c }];
        var key = ppStart.r + "," + ppStart.c;
        visited[key] = true;
        while (queue.length > 0) {
            var cur = queue.shift();
            if (cur.r === ppEnd.r && cur.c === ppEnd.c) return true;
            var dirs = [[-1,0],[1,0],[0,-1],[0,1]];
            for (var d = 0; d < dirs.length; d++) {
                var nr = cur.r + dirs[d][0], nc = cur.c + dirs[d][1], nk = nr + "," + nc;
                if (nr >= 0 && nr < ppRows && nc >= 0 && nc < ppCols && !visited[nk] && ppGrid[nr][nc] === 0) {
                    visited[nk] = true; queue.push({ r: nr, c: nc });
                }
            }
        }
        return false;
    }

    function renderPP() {
        var ctx = ppCtx;
        var size = ppCanvas.width;
        ctx.clearRect(0, 0, size, size);

        var cs = ppCellSize;

        // 绘制网格
        for (var r = 0; r < ppRows; r++) {
            for (var c = 0; c < ppCols; c++) {
                var x = c * cs;
                var y = r * cs;

                if (r === ppStart.r && c === ppStart.c) {
                    ctx.fillStyle = "#1DC981";
                } else if (r === ppEnd.r && c === ppEnd.c) {
                    ctx.fillStyle = "#E8463A";
                } else if (ppGrid[r][c] === 1) {
                    ctx.fillStyle = "#374151";
                } else {
                    ctx.fillStyle = (r + c) % 2 === 0 ? "#F9FAFB" : "#F3F4F6";
                }
                ctx.fillRect(x, y, cs, cs);
                ctx.strokeStyle = "#E5E7EB";
                ctx.lineWidth = 1;
                ctx.strokeRect(x, y, cs, cs);
            }
        }

        // 路径线（动画绘制）
        if (ppPath.length > 0) {
            ctx.save();
            ctx.strokeStyle = "#6366F1";
            ctx.lineWidth = 4;
            ctx.lineCap = "round";
            ctx.lineJoin = "round";
            ctx.beginPath();
            ctx.moveTo(ppPath[0].c * cs + cs / 2, ppPath[0].r * cs + cs / 2);
            for (var i = 1; i < ppPath.length; i++) {
                ctx.lineTo(ppPath[i].c * cs + cs / 2, ppPath[i].r * cs + cs / 2);
            }
            ctx.stroke();
            ctx.restore();

            // 路径点发光效果
            for (var i = 0; i < ppPath.length; i++) {
                var p = ppPath[i];
                ctx.save();
                var isLast = i === ppPath.length - 1;
                // 发光圈
                var grad = ctx.createRadialGradient(p.c * cs + cs / 2, p.r * cs + cs / 2, 0, p.c * cs + cs / 2, p.r * cs + cs / 2, cs * 0.25);
                grad.addColorStop(0, isLast ? "rgba(99,102,241,0.3)" : "rgba(99,102,241,0.15)");
                grad.addColorStop(1, "rgba(99,102,241,0)");
                ctx.fillStyle = grad;
                ctx.beginPath();
                ctx.arc(p.c * cs + cs / 2, p.r * cs + cs / 2, cs * 0.25, 0, Math.PI * 2);
                ctx.fill();
                // 实心点
                ctx.fillStyle = isLast ? "#6366F1" : "rgba(99,102,241,0.5)";
                ctx.beginPath();
                ctx.arc(p.c * cs + cs / 2, p.r * cs + cs / 2, cs * 0.12, 0, Math.PI * 2);
                ctx.fill();
                ctx.restore();
            }
        }

        // 起点/终点标记
        ctx.save();
        ctx.fillStyle = "#fff";
        ctx.font = "bold " + (cs * 0.4) + "px sans-serif";
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";
        ctx.fillText("S", ppStart.c * cs + cs / 2, ppStart.r * cs + cs / 2);
        ctx.fillText("E", ppEnd.c * cs + cs / 2, ppEnd.r * cs + cs / 2);
        ctx.restore();
    }

    function handlePPClick(e) {
        if (ppPhase !== "build" || finished || ppAnimating) return;
        var rect = ppCanvas.getBoundingClientRect();
        var cs = ppCellSize;
        var c = Math.floor((e.clientX - rect.left) / cs);
        var r = Math.floor((e.clientY - rect.top) / cs);

        if (r < 0 || r >= ppRows || c < 0 || c >= ppCols) return;
        if (ppGrid[r][c] === 1) { playWrong(); return; }

        if (!timer.isRunning()) timer.start();

        // 起点——清空
        if (r === ppStart.r && c === ppStart.c) {
            ppPath = [{ r: ppStart.r, c: ppStart.c }];
            $("ppSteps").textContent = "步数：0";
            renderPP();
            playClick();
            return;
        }

        // 终点——自动确认
        if (r === ppEnd.r && c === ppEnd.c) {
            if (ppPath.length > 0) {
                var last = ppPath[ppPath.length - 1];
                if (Math.abs(last.r - r) + Math.abs(last.c - c) === 1) {
                    ppPath.push({ r: r, c: c });
                    $("ppSteps").textContent = "步数：" + (ppPath.length - 1);
                    renderPP();
                    playPlace();
                    handlePPConfirm();
                    return;
                }
            }
            toast("请先点击与终点相邻的格子");
            playWrong();
            return;
        }

        // 回溯
        var existingIdx = -1;
        for (var i = 0; i < ppPath.length; i++) {
            if (ppPath[i].r === r && ppPath[i].c === c) { existingIdx = i; break; }
        }
        if (existingIdx >= 0) {
            ppPath = ppPath.slice(0, existingIdx + 1);
            $("ppSteps").textContent = "步数：" + (ppPath.length - 1);
            renderPP();
            playBacktrack();
            return;
        }

        // 添加新格子
        if (ppPath.length === 0) {
            if (Math.abs(r - ppStart.r) + Math.abs(c - ppStart.c) <= 1) {
                ppPath.push({ r: ppStart.r, c: ppStart.c });
                ppPath.push({ r: r, c: c });
                $("ppSteps").textContent = "步数：" + (ppPath.length - 1);
                renderPP();
                playStep();
            } else {
                toast("请从起点附近开始");
                playWrong();
            }
        } else {
            var last = ppPath[ppPath.length - 1];
            if (Math.abs(last.r - r) + Math.abs(last.c - c) === 1) {
                ppPath.push({ r: r, c: c });
                $("ppSteps").textContent = "步数：" + (ppPath.length - 1);
                renderPP();
                playStep();
            } else {
                toast("只能移动到相邻格子");
                playWrong();
            }
        }
    }

    function handlePPClear() {
        ppPath = [];
        $("ppSteps").textContent = "步数：0";
        renderPP();
        playClick();
    }

    function handlePPConfirm() {
        if (ppPhase !== "build" || finished) return;
        if (ppPath.length < 2) { toast("请先规划一条路径"); playWrong(); return; }

        var last = ppPath[ppPath.length - 1];
        if (last.r !== ppEnd.r || last.c !== ppEnd.c) { toast("路径未到达终点！"); playWrong(); return; }

        for (var i = 1; i < ppPath.length; i++) {
            var prev = ppPath[i - 1], cur = ppPath[i];
            var dist = Math.abs(prev.r - cur.r) + Math.abs(prev.c - cur.c);
            if (dist !== 1) { toast("路径不连续！"); playWrong(); return; }
            if (ppGrid[cur.r][cur.c] === 1) { toast("路径穿过障碍物！"); playWrong(); return; }
        }

        ppPhase = "result";
        ppAnimating = true;

        var optimalLen = bfsShortestPath();
        var pathLen = ppPath.length - 1;
        var efficiency = optimalLen > 0 ? Math.round(optimalLen / pathLen * 100) : 100;

        // 动画路径完成
        var cs = ppCellSize;
        animatePathLine(ppCtx, ppPath, cs, "#1DC981", 600, function() {
            ppAnimating = false;

            if (efficiency >= 100) {
                score += 20;
                correctCount++;
                $("ppInstruction").textContent = "✅ 最优路径！" + pathLen + "步（最优" + optimalLen + "步）";
                playCorrect();
            } else if (efficiency >= 70) {
                score += 15;
                correctCount++;
                $("ppInstruction").textContent = "✅ 可行路径！" + pathLen + "步（最优" + optimalLen + "步，效率" + efficiency + "%）";
                playCorrect();
            } else {
                score += 10;
                wrongCount++;
                $("ppInstruction").textContent = "⚠️ 路径偏长，" + pathLen + "步（最优" + optimalLen + "步，效率" + efficiency + "%）";
                playWrong();
            }

            currentRound++;
            updateStats();

            // 显示最优路径对比
            var ctx = ppCtx;
            var size = ppCanvas.width;
            // 用半透明覆盖层显示最优路径
            ctx.save();
            ctx.globalAlpha = 0.3;
            ctx.strokeStyle = "#1DC981";
            ctx.lineWidth = 2;
            ctx.setLineDash([4, 4]);
            ctx.beginPath();
            // 简单BFS回溯可视化最优路径
            var optimalPath = bfsGetPath();
            if (optimalPath) {
                ctx.moveTo(optimalPath[0].c * cs + cs / 2, optimalPath[0].r * cs + cs / 2);
                for (var i = 1; i < optimalPath.length; i++) {
                    ctx.lineTo(optimalPath[i].c * cs + cs / 2, optimalPath[i].r * cs + cs / 2);
                }
                ctx.stroke();
            }
            ctx.restore();

            $("ppBtnConfirm").textContent = "下一关";
            $("ppBtnConfirm").onclick = function() {
                if (currentRound >= totalQuestions) {
                    finishGame();
                } else {
                    $("ppBtnConfirm").onclick = handlePPConfirm;
                    initPathPlan();
                }
            };
        });

        // 先重新渲染路径，然后触发动画覆盖
        renderPP();
    }

    function bfsShortestPath() {
        var visited = {};
        var queue = [{ r: ppStart.r, c: ppStart.c, dist: 0 }];
        visited[ppStart.r + "," + ppStart.c] = true;
        while (queue.length > 0) {
            var cur = queue.shift();
            if (cur.r === ppEnd.r && cur.c === ppEnd.c) return cur.dist;
            var dirs = [[-1,0],[1,0],[0,-1],[0,1]];
            for (var d = 0; d < dirs.length; d++) {
                var nr = cur.r + dirs[d][0], nc = cur.c + dirs[d][1], nk = nr + "," + nc;
                if (nr >= 0 && nr < ppRows && nc >= 0 && nc < ppCols && !visited[nk] && ppGrid[nr][nc] === 0) {
                    visited[nk] = true;
                    queue.push({ r: nr, c: nc, dist: cur.dist + 1 });
                }
            }
        }
        return 0;
    }

    function bfsGetPath() {
        var visited = {};
        var prev = {};
        var queue = [{ r: ppStart.r, c: ppStart.c }];
        var sk = ppStart.r + "," + ppStart.c;
        visited[sk] = true;
        while (queue.length > 0) {
            var cur = queue.shift();
            if (cur.r === ppEnd.r && cur.c === ppEnd.c) {
                // 回溯路径
                var path = [];
                var node = cur;
                while (node) {
                    path.unshift({ r: node.r, c: node.c });
                    var nk = node.r + "," + node.c;
                    if (prev[nk]) node = prev[nk];
                    else node = null;
                }
                return path;
            }
            var dirs = [[-1,0],[1,0],[0,-1],[0,1]];
            for (var d = 0; d < dirs.length; d++) {
                var nr = cur.r + dirs[d][0], nc = cur.c + dirs[d][1], nk = nr + "," + nc;
                if (nr >= 0 && nr < ppRows && nc >= 0 && nc < ppCols && !visited[nk] && ppGrid[nr][nc] === 0) {
                    visited[nk] = true;
                    prev[nk] = cur;
                    queue.push({ r: nr, c: nc });
                }
            }
        }
        return null;
    }

    // ========== 汉诺塔 ==========

    let hnCanvas = null;
    let hnCtx = null;
    let hnPegs = [];
    let hnDiskCount = 3;
    let hnSelectedPeg = -1;
    let hnMoves = 0;
    let hnMinMoves = 0;
    let hnSolved = false;
    let hnPegX = [];
    let hnPegW = 0;
    let hnPegH = 0;
    let hnBaseY = 0;
    let hnAnimating = false;

    // 动画状态：盘子移动动画
    let hnAnimDisk = null; // { fromPeg, toPeg, size, progress }
    let hnAnimId = null;

    function initHanoi() {
        hnCanvas = $("hnCanvas");
        hnCtx = hnCanvas.getContext("2d");
        hnSelectedPeg = -1;
        hnMoves = 0;
        hnSolved = false;
        hnAnimating = false;
        hnAnimDisk = null;
        if (hnAnimId) { cancelAnimationFrame(hnAnimId); hnAnimId = null; }

        if (difficulty === "简单") hnDiskCount = 3;
        else if (difficulty === "困难") hnDiskCount = 5;
        else hnDiskCount = 4;

        hnMinMoves = Math.pow(2, hnDiskCount) - 1;
        hnPegs = [[], [], []];

        for (var i = hnDiskCount; i >= 1; i--) {
            hnPegs[0].push(i);
        }

        resizeHNCanvas();
        $("hnMoves").textContent = "移动次数：0";
        $("hnMin").textContent = "最少：" + hnMinMoves;
        $("hnStatus").textContent = "将所有盘子从A移到C";
        renderHN();
    }

    function resizeHNCanvas() {
        var wrap = hnCanvas.parentElement;
        var w = Math.min(wrap.clientWidth - 4, 560);
        var h = Math.min(wrap.clientHeight - 4, 380);
        var dpr = window.devicePixelRatio || 1;
        hnCanvas.style.width = w + "px";
        hnCanvas.style.height = h + "px";
        hnCanvas.width = w * dpr;
        hnCanvas.height = h * dpr;
        hnCtx.setTransform(dpr, 0, 0, dpr, 0, 0);

        hnPegW = w / 3;
        hnPegX = [hnPegW * 0.5, hnPegW * 1.5, hnPegW * 2.5];
        hnPegH = h * 0.65;
        hnBaseY = h * 0.85;
        return { w: w, h: h };
    }

    function renderHN() {
        var ctx = hnCtx;
        var canvas = hnCanvas;
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        var dpr = window.devicePixelRatio || 1;
        var w = canvas.width / dpr;
        var h = canvas.height / dpr;

        // 底座
        ctx.save();
        ctx.fillStyle = "#8B5CF6";
        ctx.shadowColor = "rgba(139,92,246,0.3)";
        ctx.shadowBlur = 8;
        ctx.fillRect(hnPegX[0] - hnPegW * 0.4, hnBaseY - 4, hnPegW * 2.8, 8);
        ctx.restore();

        // 柱子
        for (var p = 0; p < 3; p++) {
            ctx.save();
            var grad = ctx.createLinearGradient(hnPegX[p] - 4, 0, hnPegX[p] + 4, 0);
            grad.addColorStop(0, "#C4B5FD");
            grad.addColorStop(0.5, "#A78BFA");
            grad.addColorStop(1, "#C4B5FD");
            ctx.fillStyle = grad;
            ctx.fillRect(hnPegX[p] - 4, hnBaseY - hnPegH, 8, hnPegH);

            // 柱子标签
            ctx.fillStyle = "#9CA3AF";
            ctx.font = "bold 16px sans-serif";
            ctx.textAlign = "center";
            ctx.textBaseline = "top";
            ctx.fillText(String.fromCharCode(65 + p), hnPegX[p], hnBaseY + 10);

            // 选中高亮（动画脉冲）
            if (hnSelectedPeg === p) {
                var pulse = Math.sin(Date.now() / 300) * 0.3 + 0.7;
                ctx.strokeStyle = "rgba(99,102,241," + pulse + ")";
                ctx.lineWidth = 3;
                ctx.shadowColor = "rgba(99,102,241,0.4)";
                ctx.shadowBlur = 12;
                ctx.strokeRect(hnPegX[p] - hnPegW * 0.4, hnBaseY - hnPegH - 10, hnPegW * 0.8, hnPegH + 20);
            }

            ctx.restore();
        }

        // 盘子
        var diskH = Math.min(28, hnPegH / (hnDiskCount + 1));
        var colors = ["#E8463A", "#F59E0B", "#3B82F6", "#1DC981", "#8B5CF6"];
        var borderColors = ["#C62828", "#F59E0B", "#2563EB", "#059669", "#7C3AED"];

        for (var p = 0; p < 3; p++) {
            var peg = hnPegs[p];
            for (var i = 0; i < peg.length; i++) {
                var size = peg[i];
                // 如果正在动画该盘子，跳过（由动画层绘制）
                if (hnAnimDisk && hnAnimDisk.fromPeg === p && i === peg.length - 1 && hnAnimDisk.size === size) continue;

                var diskW = hnPegW * 0.35 * (size / hnDiskCount);
                var x = hnPegX[p] - diskW / 2;
                var y = hnBaseY - (i + 1) * diskH;

                drawDisk(ctx, x, y, diskW, diskH, colors[(size - 1) % colors.length], borderColors[(size - 1) % borderColors.length], size, diskH);
            }
        }

        // 绘制动画中盘子
        if (hnAnimDisk) {
            var d = hnAnimDisk;
            var diskW = hnPegW * 0.35 * (d.size / hnDiskCount);
            // 从源柱子顶部到目标柱子上方的贝塞尔曲线
            var fromX = hnPegX[d.fromPeg];
            var toX = hnPegX[d.toPeg];
            var fromY = hnBaseY - d.fromStackHeight * diskH;
            var toY = hnBaseY - (d.toStackHeight + 1) * diskH;
            var midY = hnBaseY - hnPegH - 40;

            var t = d.progress;
            // 贝塞尔曲线：先上后下
            var px, py;
            if (t < 0.5) {
                var tt = t * 2;
                px = fromX + (toX - fromX) * tt * 0.5;
                py = fromY + (midY - fromY) * tt;
            } else {
                var tt = (t - 0.5) * 2;
                px = fromX + (toX - fromX) * (0.5 + tt * 0.5);
                py = midY + (toY - midY) * tt;
            }

            var x = px - diskW / 2;
            var y = py - diskH / 2;
            drawDisk(ctx, x, y, diskW, diskH, colors[(d.size - 1) % colors.length], borderColors[(d.size - 1) % borderColors.length], d.size, diskH);
        }
    }

    function drawDisk(ctx, x, y, w, h, color, borderColor, size, diskH) {
        ctx.save();
        var radius = 6;

        // 阴影
        ctx.shadowColor = "rgba(0,0,0,0.15)";
        ctx.shadowBlur = 6;
        ctx.shadowOffsetY = 2;

        // 渐变
        var grad = ctx.createLinearGradient(x, y, x, y + h);
        grad.addColorStop(0, lightenColor(color, 20));
        grad.addColorStop(0.5, color);
        grad.addColorStop(1, darkenColor(color, 20));
        ctx.fillStyle = grad;

        ctx.beginPath();
        ctx.moveTo(x + radius, y);
        ctx.lineTo(x + w - radius, y);
        ctx.quadraticCurveTo(x + w, y, x + w, y + radius);
        ctx.lineTo(x + w, y + h - radius);
        ctx.quadraticCurveTo(x + w, y + h, x + w - radius, y + h);
        ctx.lineTo(x + radius, y + h);
        ctx.quadraticCurveTo(x, y + h, x, y + h - radius);
        ctx.lineTo(x, y + radius);
        ctx.quadraticCurveTo(x, y, x + radius, y);
        ctx.closePath();
        ctx.fill();

        ctx.shadowColor = "transparent";
        ctx.strokeStyle = borderColor;
        ctx.lineWidth = 1.5;
        ctx.stroke();

        // 数字
        ctx.fillStyle = "#fff";
        ctx.font = "bold " + Math.min(14, h * 0.5) + "px sans-serif";
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";
        ctx.shadowColor = "rgba(0,0,0,0.3)";
        ctx.shadowBlur = 2;
        ctx.fillText(size, x + w / 2, y + h / 2);

        ctx.restore();
    }

    function lightenColor(hex, percent) {
        var num = parseInt(hex.replace("#", ""), 16);
        var r = Math.min(255, (num >> 16) + percent);
        var g = Math.min(255, ((num >> 8) & 0x00FF) + percent);
        var b = Math.min(255, (num & 0x0000FF) + percent);
        return "rgb(" + r + "," + g + "," + b + ")";
    }

    function darkenColor(hex, percent) {
        var num = parseInt(hex.replace("#", ""), 16);
        var r = Math.max(0, (num >> 16) - percent);
        var g = Math.max(0, ((num >> 8) & 0x00FF) - percent);
        var b = Math.max(0, (num & 0x0000FF) - percent);
        return "rgb(" + r + "," + g + "," + b + ")";
    }

    function handleHNClick(e) {
        if (hnSolved || finished || hnAnimating) return;
        var rect = hnCanvas.getBoundingClientRect();
        var mx = e.clientX - rect.left;
        var my = e.clientY - rect.top;

        var dpr = window.devicePixelRatio || 1;
        var w = (hnCanvas.width / dpr);

        var pegIdx = -1;
        for (var p = 0; p < 3; p++) {
            if (mx >= hnPegX[p] - hnPegW * 0.4 && mx <= hnPegX[p] + hnPegW * 0.4) {
                pegIdx = p;
                break;
            }
        }
        if (pegIdx < 0) return;

        if (!timer.isRunning()) timer.start();

        if (hnSelectedPeg < 0) {
            // 选择柱子
            if (hnPegs[pegIdx].length > 0) {
                hnSelectedPeg = pegIdx;
                playClick();
                // 启动脉冲动画
                function pulseLoop() {
                    if (hnSelectedPeg === pegIdx) {
                        renderHN();
                        hnAnimId = requestAnimationFrame(pulseLoop);
                    }
                }
                if (hnAnimId) cancelAnimationFrame(hnAnimId);
                hnAnimId = requestAnimationFrame(pulseLoop);
                renderHN();
                hintEl.textContent = "已选" + String.fromCharCode(65 + pegIdx) + "柱，点击目标柱子移动";
            } else {
                toast("该柱子上没有盘子");
                playWrong();
            }
        } else {
            // 移动盘子
            if (pegIdx === hnSelectedPeg) {
                hnSelectedPeg = -1;
                if (hnAnimId) { cancelAnimationFrame(hnAnimId); hnAnimId = null; }
                renderHN();
                hintEl.textContent = "点击柱子选择顶部盘子";
                playClick();
                return;
            }

            var fromPeg = hnPegs[hnSelectedPeg];
            var toPeg = hnPegs[pegIdx];
            var topDisk = fromPeg[fromPeg.length - 1];

            if (toPeg.length === 0 || toPeg[toPeg.length - 1] > topDisk) {
                // 合法移动——动画
                hnAnimating = true;
                var diskH = Math.min(28, hnPegH / (hnDiskCount + 1));
                hnAnimDisk = {
                    fromPeg: hnSelectedPeg,
                    toPeg: pegIdx,
                    size: topDisk,
                    progress: 0,
                    fromStackHeight: fromPeg.length,
                    toStackHeight: toPeg.length
                };

                playMove();
                fromPeg.pop();

                var animStart = performance.now();
                var animDuration = 250;

                function animateMove() {
                    var elapsed = performance.now() - animStart;
                    hnAnimDisk.progress = Math.min(1, elapsed / animDuration);
                    renderHN();

                    if (hnAnimDisk.progress < 1) {
                        requestAnimationFrame(animateMove);
                    } else {
                        // 动画完成
                        toPeg.push(topDisk);
                        hnAnimDisk = null;
                        hnAnimating = false;
                        hnSelectedPeg = -1;
                        if (hnAnimId) { cancelAnimationFrame(hnAnimId); hnAnimId = null; }
                        hnMoves++;
                        $("hnMoves").textContent = "移动次数：" + hnMoves;
                        playPlace();
                        renderHN();

                        // 检查是否完成
                        if (hnPegs[2].length === hnDiskCount) {
                            hnSolved = true;
                            var efficiency = hnMinMoves / hnMoves;
                            if (efficiency >= 1) {
                                score += 25;
                                correctCount++;
                                playComplete();
                            } else if (efficiency >= 0.8) {
                                score += 20;
                                correctCount++;
                                playCorrect();
                            } else {
                                score += 10;
                                wrongCount++;
                                playWrong();
                            }
                            currentRound++;
                            updateStats();
                            $("hnStatus").textContent = "🎉 完成！用了" + hnMoves + "步（最少" + hnMinMoves + "步）";
                            hintEl.textContent = "太棒了！";

                            if (currentRound >= totalQuestions) {
                                setTimeout(finishGame, 1500);
                            } else {
                                setTimeout(function() {
                                    hnMoves = 0;
                                    initHanoi();
                                }, 2000);
                            }
                        } else {
                            hintEl.textContent = "点击柱子选择顶部盘子";
                        }
                    }
                }
                requestAnimationFrame(animateMove);
            } else {
                toast("不能将大盘子放在小盘子上");
                playWrong();
                hnSelectedPeg = -1;
                if (hnAnimId) { cancelAnimationFrame(hnAnimId); hnAnimId = null; }
                renderHN();
                hintEl.textContent = "点击柱子选择顶部盘子";
            }
        }
    }

    // ========== 时间估计 ==========

    let tePhase = "idle"; // idle | waiting | result
    let teTargetTime = 5;
    let teStartTime = 0;
    let teActualElapsed = 0;
    let teDistractionTimer = null;
    let tePulseAnimId = null;

    function initTimeEst() {
        tePhase = "idle";
        teTargetTime = difficulty === "简单" ? 3 : (difficulty === "困难" ? 8 : 5);
        $("teTarget").textContent = "目标：" + teTargetTime + " 秒";
        $("teResult").textContent = "—";
        $("teFeedback").textContent = "准备好后点击"开始估计"";
        $("teBtnStart").textContent = "开始估计";
        $("teBtnStart").disabled = false;
        var circle = $("teCircle");
        circle.className = "te-circle";
        $("teBtnStart").onclick = handleTEStart;
    }

    function handleTEStart() {
        if (tePhase === "idle") {
            tePhase = "waiting";
            teStartTime = Date.now();
            $("teBtnStart").textContent = "点击！(估计时间到)";
            var circle = $("teCircle");
            circle.className = "te-circle active";
            $("teFeedback").textContent = "当你觉得" + teTargetTime + "秒到了，点击按钮";

            if (!timer.isRunning()) timer.start();

            // 脉冲动画——随着时间接近目标，脉冲加快
            function pulseAnim() {
                if (tePhase !== "waiting") { tePulseAnimId = null; return; }
                var elapsed = (Date.now() - teStartTime) / 1000;
                var progress = Math.min(0.95, elapsed / teTargetTime);
                var scale = 1 + progress * 0.15;
                var opacity = 0.4 + progress * 0.6;
                var circle = $("teCircle");
                var pulse = Math.sin(Date.now() / (200 - progress * 100)) * 0.05 + 1;
                circle.style.transform = "scale(" + (scale * pulse) + ")";
                circle.style.opacity = opacity;

                // 接近目标时播放滴答声
                if (elapsed > teTargetTime * 0.7) {
                    playTimerTick();
                }

                tePulseAnimId = requestAnimationFrame(pulseAnim);
            }
            tePulseAnimId = requestAnimationFrame(pulseAnim);

            // 干扰闪烁
            var distractorCount = 0;
            var maxDistractors = difficulty === "简单" ? 1 : (difficulty === "困难" ? 3 : 2);
            teDistractionTimer = setInterval(function() {
                if (tePhase !== "waiting") {
                    clearInterval(teDistractionTimer);
                    return;
                }
                if (distractorCount < maxDistractors) {
                    var c = $("teCircle");
                    c.className = "te-circle ready";
                    playDistract();
                    // 抖动效果
                    var visual = $("teVisual");
                    visual.style.transform = "translateX(" + (Math.random() > 0.5 ? 5 : -5) + "px)";
                    setTimeout(function() {
                        visual.style.transform = "translateX(0)";
                        if (tePhase === "waiting") {
                            c.className = "te-circle active";
                        }
                    }, 200);
                    distractorCount++;
                }
            }, (teTargetTime * 1000) / (maxDistractors + 1));

            $("teBtnStart").onclick = handleTEResult;
        }
    }

    function handleTEResult() {
        if (tePhase !== "waiting") return;
        tePhase = "result";
        if (teDistractionTimer) { clearInterval(teDistractionTimer); teDistractionTimer = null; }
        if (tePulseAnimId) { cancelAnimationFrame(tePulseAnimId); tePulseAnimId = null; }

        teActualElapsed = (Date.now() - teStartTime) / 1000;
        var diff = Math.abs(teActualElapsed - teTargetTime);
        var accuracy = Math.max(0, 100 - (diff / teTargetTime) * 100);

        $("teResult").textContent = "实际：" + teActualElapsed.toFixed(1) + "s";
        var circle = $("teCircle");
        circle.style.transform = "scale(1)";
        circle.style.opacity = "1";

        // 结果动画
        if (accuracy >= 90) {
            circle.className = "te-circle";
            circle.style.backgroundColor = "#1DC981";
            circle.style.boxShadow = "0 0 40px rgba(29,201,129,0.6)";
            score += 20;
            correctCount++;
            $("teFeedback").textContent = "✅ 非常精确！误差仅" + diff.toFixed(1) + "秒";
            playCorrect();
        } else if (accuracy >= 70) {
            circle.className = "te-circle";
            circle.style.backgroundColor = "#3B82F6";
            circle.style.boxShadow = "0 0 30px rgba(59,130,246,0.5)";
            score += 15;
            correctCount++;
            $("teFeedback").textContent = "✅ 不错！误差" + diff.toFixed(1) + "秒";
            playCorrect();
        } else {
            circle.className = "te-circle";
            circle.style.backgroundColor = "#E8463A";
            circle.style.boxShadow = "0 0 30px rgba(232,70,58,0.5)";
            score += 5;
            wrongCount++;
            $("teFeedback").textContent = "⚠️ 误差" + diff.toFixed(1) + "秒，目标" + teTargetTime + "秒";
            playWrong();
        }

        // 缩放动画
        setTimeout(function() {
            circle.style.transform = "scale(1.15)";
            setTimeout(function() {
                circle.style.transform = "scale(1)";
            }, 200);
        }, 50);

        currentRound++;
        updateStats();

        $("teBtnStart").textContent = "继续";
        $("teBtnStart").onclick = function() {
            if (currentRound >= totalQuestions) {
                finishGame();
            } else {
                initTimeEst();
            }
        };
    }

    // ========== 结算 ==========

    function finishGame() {
        if (finished) return;
        finished = true;
        timer.stop();
        if (hnAnimId) { cancelAnimationFrame(hnAnimId); hnAnimId = null; }
        if (tePulseAnimId) { cancelAnimationFrame(tePulseAnimId); tePulseAnimId = null; }
        if (teDistractionTimer) { clearInterval(teDistractionTimer); teDistractionTimer = null; }

        playComplete();

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

        if (ppCanvas) ppCanvas.removeEventListener("click", handlePPClick);
        if (hnCanvas) hnCanvas.removeEventListener("click", handleHNClick);

        sendToHost({
            type: "gameEnd", mode: "plan", subMode: mode,
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
        if (teDistractionTimer) { clearInterval(teDistractionTimer); teDistractionTimer = null; }
        if (hnAnimId) { cancelAnimationFrame(hnAnimId); hnAnimId = null; }
        if (tePulseAnimId) { cancelAnimationFrame(tePulseAnimId); tePulseAnimId = null; }
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
        mode = data.mode || "pathplan";
        difficulty = data.difficulty || "普通";
        totalQuestions = data.totalQuestions || 8;
        applyTheme(data.theme || "light");

        if (mode === "hanoi") {
            totalQuestions = Math.max(2, Math.min(4, totalQuestions));
        } else if (mode === "timeest") {
            totalQuestions = Math.max(6, Math.min(10, totalQuestions));
        } else {
            totalQuestions = Math.max(5, Math.min(8, totalQuestions));
        }

        resetGame();

        modeHint.textContent = "计划预判 · " + MODE_LABELS[mode] + " · " + difficulty;

        if (mode === "pathplan") {
            $("pathplanArea").classList.remove("hidden");
            ppCanvas = $("ppCanvas");
            ppCanvas.addEventListener("click", handlePPClick);
            $("ppBtnClear").addEventListener("click", handlePPClear);
            $("ppBtnConfirm").addEventListener("click", handlePPConfirm);
            hintEl.textContent = "从🟢起点点击相邻格子，规划路径到🔴终点";
            initPathPlan();
        } else if (mode === "hanoi") {
            $("hanoiArea").classList.remove("hidden");
            hnCanvas = $("hnCanvas");
            hnCanvas.addEventListener("click", handleHNClick);
            hintEl.textContent = "点击柱子选择顶部盘子，再点击目标柱子移动";
            initHanoi();
        } else if (mode === "timeest") {
            $("timeestArea").classList.remove("hidden");
            hintEl.textContent = "准确估计经过的时间";
            initTimeEst();
        }
    }

    listenInit(function(data, theme, meta) { boot(data || {}); });
    if (isMock()) { boot({ mode: "pathplan", difficulty: "普通", totalQuestions: 5 }); }

})();