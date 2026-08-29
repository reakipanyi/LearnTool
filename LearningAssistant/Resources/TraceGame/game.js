(function () {
    "use strict";

    const { $, toast, sendToHost, listenInit, isMock, applyTheme, shuffle, createTimer, formatTime, renderStars, setProgress } = window.GameUI;

    // ========== 常量 ==========
    const LINE_COLORS = ["#E8463A", "#1DC981", "#4B3FE3", "#F5A623", "#9B59B6", "#1ABC9C", "#E67E22", "#2ECC71", "#3498DB", "#E84393"];
    const DIFF = {
        简单: { lines: 6, colorMode: "distinct" },
        普通: { lines: 8, colorMode: "grouped" },
        困难: { lines: 10, colorMode: "same" }
    };

    // ========== 状态 ==========
    let difficulty = "普通";
    let lineCount = 6;
    let lines = [];
    let leftPoints = [];
    let rightPoints = [];
    let selectedLeft = null;
    let matchedCount = 0;
    let score = 0;
    let correctCount = 0;
    let wrongCount = 0;
    let finished = false;
    let hoveredLine = -1;
    let animating = false;

    // DOM
    const canvas = $("traceCanvas");
    const ctx = canvas.getContext("2d");
    const modeHint = $("modeHint");
    const hintEl = $("hint");
    const scoreEl = $("score");
    const correctEl = $("correct");
    const remainEl = $("remain");
    const timerEl = $("timer");
    const progressEl = $("progress");
    const timer = createTimer({ element: timerEl, interval: 200 });

    // ========== Canvas 尺寸 ==========

    function resizeCanvas() {
        const wrap = canvas.parentElement;
        const w = Math.min(wrap.clientWidth - 4, 740);
        const h = Math.min(wrap.clientHeight - 4, Math.max(400, lineCount * 55));
        const dpr = window.devicePixelRatio || 1;
        canvas.style.width = w + "px";
        canvas.style.height = h + "px";
        canvas.width = w * dpr;
        canvas.height = h * dpr;
        ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
        return { w, h };
    }

    // ========== 线条生成 ==========

    function generateLines(count, colorMode) {
        const { w, h } = resizeCanvas();
        const marginX = 55;
        const marginY = 35;
        const spacing = (h - 2 * marginY) / (count + 1);

        // 生成左右端点
        const left = [];
        const right = [];
        for (let i = 0; i < count; i++) {
            const y = marginY + (i + 1) * spacing;
            left.push({ x: marginX, y, label: i + 1, lineIdx: -1, matched: false });
            right.push({ x: w - marginX, y, label: i + 1, lineIdx: -1, matched: false });
        }

        // 打乱右端点顺序
        const shuffledRight = shuffle([...right]);
        shuffledRight.forEach((rp, i) => rp.matchIdx = i);

        // 分配颜色
        const colors = [];
        for (let i = 0; i < count; i++) {
            if (colorMode === "distinct") colors.push(LINE_COLORS[i % LINE_COLORS.length]);
            else if (colorMode === "grouped") colors.push(LINE_COLORS[i % 3]);
            else colors.push("#888"); // 全部灰色
        }
        // 困难模式用相近颜色
        if (colorMode === "same") {
            const baseHue = Math.floor(Math.random() * 360);
            for (let i = 0; i < count; i++) {
                colors[i] = "hsl(" + baseHue + ", 30%, 55%)";
            }
        }

        // 生成路径
        const ls = [];
        for (let i = 0; i < count; i++) {
            const start = left[i];
            const end = shuffledRight[i];
            const waypoints = generateWaypoints(start, end, w, count);
            ls.push({ start, end, waypoints, color: colors[i], matched: false, index: i });
            start.lineIdx = i;
            end.lineIdx = i;
        }

        return { lines: ls, leftPoints: left, rightPoints: shuffledRight };
    }

    function generateWaypoints(start, end, canvasW, totalLines) {
        const num = 3 + Math.floor(Math.random() * 2); // 3-4 个中间点
        const pts = [];
        const xStep = (end.x - start.x) / (num + 1);
        const maxOffset = Math.max(80, canvasW / totalLines * 1.8);

        for (let i = 1; i <= num; i++) {
            const x = start.x + i * xStep + (Math.random() - 0.5) * 20;
            const y = start.y + (Math.random() - 0.5) * maxOffset * 2;
            pts.push({ x, y });
        }
        return pts;
    }

    // ========== 渲染 ==========

    function render() {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        const { w, h } = { w: canvas.width, h: canvas.height };

        // 绘制未匹配的线条
        for (const line of lines) {
            if (line.matched) continue;
            const isHovered = line.index === hoveredLine;
            const isSelected = selectedLeft !== null && leftPoints[selectedLeft]?.lineIdx === line.index;
            drawLine(line, isHovered || isSelected ? 4 : 2.5, isHovered || isSelected ? 1 : 0.7);
        }

        // 绘制已匹配的线条（最上层，绿色）
        for (const line of lines) {
            if (!line.matched) continue;
            drawLine(line, 4, 1, "#1DC981");
        }

        // 绘制端点
        for (const lp of leftPoints) drawEndpoint(lp, "left");
        for (const rp of rightPoints) drawEndpoint(rp, "right");
    }

    function drawLine(line, width, alpha, overrideColor) {
        const color = overrideColor || line.color;
        const pts = [line.start, ...line.waypoints, line.end];

        ctx.save();
        ctx.globalAlpha = alpha;
        ctx.strokeStyle = color;
        ctx.lineWidth = width;
        ctx.lineCap = "round";
        ctx.lineJoin = "round";
        ctx.beginPath();
        ctx.moveTo(pts[0].x, pts[0].y);

        for (let i = 1; i < pts.length; i++) {
            const prev = pts[i - 1];
            const curr = pts[i];
            const cp1x = prev.x + (curr.x - prev.x) * 0.3;
            const cp1y = prev.y;
            const cp2x = curr.x - (curr.x - prev.x) * 0.3;
            const cp2y = curr.y;
            ctx.bezierCurveTo(cp1x, cp1y, cp2x, cp2y, curr.x, curr.y);
        }

        ctx.stroke();
        ctx.restore();
    }

    function drawEndpoint(pt, side) {
        if (pt.matched) {
            // 已匹配 - 绿色
            ctx.save();
            ctx.fillStyle = "#1DC981";
            ctx.strokeStyle = "#1DC981";
            ctx.lineWidth = 3;
            ctx.beginPath();
            ctx.arc(pt.x, pt.y, 15, 0, Math.PI * 2);
            ctx.fill();
            ctx.stroke();
            ctx.fillStyle = "#fff";
            ctx.font = "bold 12px sans-serif";
            ctx.textAlign = "center";
            ctx.textBaseline = "middle";
            ctx.fillText(pt.label, pt.x, pt.y);
            ctx.restore();
            return;
        }

        const isSelected = side === "left" && selectedLeft !== null && leftPoints[selectedLeft] === pt;
        const line = lines[pt.lineIdx];
        const color = line ? line.color : "#888";

        ctx.save();
        if (isSelected) {
            ctx.fillStyle = color;
            ctx.strokeStyle = color;
            ctx.lineWidth = 3;
        } else {
            ctx.fillStyle = "#fff";
            ctx.strokeStyle = color;
            ctx.lineWidth = 2.5;
        }
        ctx.beginPath();
        ctx.arc(pt.x, pt.y, 14, 0, Math.PI * 2);
        ctx.fill();
        ctx.stroke();

        // 标签
        ctx.fillStyle = isSelected ? "#fff" : color;
        ctx.font = "bold 13px sans-serif";
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";
        ctx.fillText(pt.label, pt.x, pt.y);
        ctx.restore();
    }

    // ========== 交互 ==========

    function getMousePos(e) {
        const rect = canvas.getBoundingClientRect();
        return { x: e.clientX - rect.left, y: e.clientY - rect.top };
    }

    function dist(a, b) {
        return Math.sqrt((a.x - b.x) ** 2 + (a.y - b.y) ** 2);
    }

    function hitTestEndpoint(pos, pt) {
        return dist(pos, pt) < 20;
    }

    function hitTestLine(pos, line) {
        // 检查点是否在线条附近
        const pts = [line.start, ...line.waypoints, line.end];
        for (let i = 1; i < pts.length; i++) {
            const a = pts[i - 1];
            const b = pts[i];
            // 点 to 线段距离
            const dx = b.x - a.x;
            const dy = b.y - a.y;
            const len = Math.sqrt(dx * dx + dy * dy);
            if (len < 1) continue;
            const t = Math.max(0, Math.min(1, ((pos.x - a.x) * dx + (pos.y - a.y) * dy) / (len * len)));
            const projX = a.x + t * dx;
            const projY = a.y + t * dy;
            if (dist(pos, { x: projX, y: projY }) < 12) return true;
        }
        return false;
    }

    function handleCanvasClick(e) {
        if (finished || animating) return;
        const pos = getMousePos(e);

        // 检查是否点击了左端点
        for (const lp of leftPoints) {
            if (lp.matched) continue;
            if (hitTestEndpoint(pos, lp)) {
                selectedLeft = leftPoints.indexOf(lp);
                if (!timer.isRunning()) timer.start();
                render();
                hintEl.textContent = "已选择起点 " + lp.label + "，请点击右侧对应终点";
                return;
            }
        }

        // 检查是否点击了右端点（需要先选择起点）
        if (selectedLeft === null) {
            toast("请先选择一个左侧起点");
            return;
        }
        for (const rp of rightPoints) {
            if (rp.matched) continue;
            if (hitTestEndpoint(pos, rp)) {
                handleMatch(rp);
                return;
            }
        }

        // 点击空白区域取消选择
        if (selectedLeft !== null) {
            selectedLeft = null;
            render();
            hintEl.textContent = "点击左侧起点，再点击右侧对应终点";
        }
    }

    function handleMatch(rp) {
        const lp = leftPoints[selectedLeft];
        const expectedLineIdx = lp.lineIdx;
        const actualLineIdx = rp.lineIdx;

        if (expectedLineIdx === actualLineIdx) {
            // 正确匹配
            lines[expectedLineIdx].matched = true;
            lp.matched = true;
            rp.matched = true;
            matchedCount++;
            score += 10;
            correctCount++;
            selectedLeft = null;
            updateStats();
            render();
            toast("✅ 正确！");
            hintEl.textContent = "匹配成功！继续";

            if (matchedCount >= lineCount) {
                setTimeout(() => finishGame(), 500);
            }
        } else {
            // 错误匹配
            animating = true;
            wrongCount++;
            score = Math.max(0, score - 5);
            updateStats();
            // 闪烁错误
            ctx.save();
            ctx.strokeStyle = "#E8463A";
            ctx.lineWidth = 4;
            const pts = [lines[actualLineIdx].start, ...lines[actualLineIdx].waypoints, lines[actualLineIdx].end];
            ctx.beginPath();
            ctx.moveTo(pts[0].x, pts[0].y);
            for (let i = 1; i < pts.length; i++) {
                const prev = pts[i - 1];
                const curr = pts[i];
                ctx.bezierCurveTo(prev.x + (curr.x - prev.x) * 0.3, prev.y, curr.x - (curr.x - prev.x) * 0.3, curr.y, curr.x, curr.y);
            }
            ctx.stroke();
            ctx.restore();

            toast("❌ 不匹配！正确答案是 #" + (lines[expectedLineIdx].end.label));
            setTimeout(() => {
                animating = false;
                render();
            }, 600);
        }
    }

    function handleCanvasMove(e) {
        if (finished) return;
        const pos = getMousePos(e);
        let found = -1;

        // 检查悬停在线条上
        for (const line of lines) {
            if (line.matched) continue;
            if (hitTestLine(pos, line)) {
                found = line.index;
                break;
            }
        }

        // 检查悬停在端点上
        for (const lp of leftPoints) {
            if (lp.matched) continue;
            if (hitTestEndpoint(pos, lp)) { canvas.style.cursor = "pointer"; render(); return; }
        }
        for (const rp of rightPoints) {
            if (rp.matched) continue;
            if (hitTestEndpoint(pos, rp)) { canvas.style.cursor = "pointer"; render(); return; }
        }

        canvas.style.cursor = found >= 0 ? "pointer" : "default";
        if (found !== hoveredLine) {
            hoveredLine = found;
            render();
        }
    }

    // ========== 统计 ==========

    function updateStats() {
        scoreEl.textContent = score;
        correctEl.textContent = correctCount;
        remainEl.textContent = lineCount - matchedCount;
        setProgress(progressEl, matchedCount, lineCount);
    }

    // ========== 结算 ==========

    function finishGame() {
        if (finished) return;
        finished = true;
        timer.stop();
        const elapsed = timer.getElapsed();
        const rate = lineCount ? Math.round(correctCount / lineCount * 100) : 0;
        let stars = 3;
        if (rate < 80) stars = 2;
        if (rate < 60) stars = 1;
        if (rate < 40) stars = 0;
        const emoji = stars >= 3 ? "🏆" : stars >= 2 ? "🎉" : stars >= 1 ? "😊" : "💪";

        $("resultEmoji").textContent = emoji;
        $("resultTitle").textContent = stars >= 1 ? "完成！" : "继续加油！";
        $("resultStars").innerHTML = renderStars(stars);
        $("resultScore").textContent = score;
        $("resultRate").textContent = rate + "%";
        $("resultRight").textContent = correctCount + "/" + lineCount;
        $("resultTime").textContent = formatTime(elapsed);
        $("resultMode").textContent = difficulty;
        $("resultOverlay").classList.remove("hidden");

        sendToHost({
            type: "gameEnd", mode: "trace", difficulty,
            timeMs: Math.round(elapsed * 1000), score,
            correct: correctCount, errors: wrongCount,
            total: lineCount, star: stars
        });
    }

    // ========== 重置 ==========

    function resetGame() {
        finished = false;
        score = 0;
        correctCount = 0;
        wrongCount = 0;
        matchedCount = 0;
        selectedLeft = null;
        hoveredLine = -1;
        animating = false;
        lines = [];
        leftPoints = [];
        rightPoints = [];
        timer.reset();
        $("resultOverlay").classList.add("hidden");
        scoreEl.textContent = "0";
        correctEl.textContent = "0";
        remainEl.textContent = "0";
        timerEl.textContent = "0s";
        setProgress(progressEl, 0, 1);
    }

    // ========== 启动 ==========

    function boot(data) {
        difficulty = data.difficulty || "普通";
        lineCount = data.lineCount || (DIFF[difficulty] ? DIFF[difficulty].lines : 8);
        applyTheme(data.theme || "light");

        const cfg = DIFF[difficulty] || DIFF["普通"];
        modeHint.textContent = "线条追踪 · " + difficulty + " · " + cfg.lines + " 条线";

        resetGame();
        const result = generateLines(cfg.lines, cfg.colorMode);
        lines = result.lines;
        leftPoints = result.leftPoints;
        rightPoints = result.rightPoints;
        matchedCount = 0;
        updateStats();
        hintEl.textContent = "点击左侧起点，再点击右侧对应终点";
        render();

        canvas.addEventListener("click", handleCanvasClick);
        canvas.addEventListener("mousemove", handleCanvasMove);
    }

    listenInit((data, theme, meta) => { boot(data || {}); });
    if (isMock()) { boot({ difficulty: "普通", lineCount: 6 }); }

})();