(() => {
    "use strict";

    const { toast, sendToHost, listenInit, isMock, applyTheme } = window.GameUI;
    const Engine = window.SudokuEngine;

    // ---------- 状态 ----------
    let grid = [];            // 9×9 当前盘面
    let solution = [];        // 9×9 完整解
    let given = [];           // 9×9 boolean: 是否为预填题
    let notes = [];           // 9×9 Set<number>[]: 笔记候选数
    let selectedRow = -1;
    let selectedCol = -1;
    let noteMode = false;
    let errors = 0;
    let score = 0;
    let timerSeconds = 0;
    let timerId = null;
    let started = false;
    let finished = false;
    let difficulty = "普通";
    let helpPenalty = 100;
    let hintsUsed = 0;
    let filledCount = 0;

    // ---------- DOM ----------
    const $ = (id) => document.getElementById(id);
    const board = $("sudokuBoard");
    const timerEl = $("timer");
    const errorsEl = $("errors");
    const progressTextEl = $("progressText");
    const scoreEl = $("score");
    const progressEl = $("progress");
    const hintEl = $("hint");
    const btnToggleNote = $("btnToggleNote");
    const btnHint = $("btnHint");
    const btnCheck = $("btnCheck");

    // ---------- Mock 数据 ----------
    const MOCK_CONFIG = { difficulty: "普通", clueCount: 30, helpPenalty: 100, theme: "light" };

    // ---------- 盘面渲染 ----------
    function renderBoard() {
        board.innerHTML = "";
        for (let r = 0; r < 9; r++) {
            const tr = document.createElement("tr");
            for (let c = 0; c < 9; c++) {
                const td = document.createElement("td");
                td.dataset.row = r;
                td.dataset.col = c;

                if (given[r][c]) {
                    td.className = "given";
                    td.textContent = grid[r][c];
                } else if (grid[r][c] !== 0) {
                    td.className = "player";
                    td.textContent = grid[r][c];
                } else if (notes[r][c] && notes[r][c].size > 0) {
                    td.className = "notes-cell";
                    renderNotes(td, r, c);
                }

                td.addEventListener("click", () => onCellClick(r, c));
                tr.appendChild(td);
            }
            board.appendChild(tr);
        }
        highlightSelected();
        updateStats();
    }

    function renderNotes(td, r, c) {
        const div = document.createElement("div");
        div.className = "notes";
        for (let n = 1; n <= 9; n++) {
            const span = document.createElement("span");
            span.textContent = notes[r][c].has(n) ? n : "";
            div.appendChild(span);
        }
        td.appendChild(div);
    }

    function updateCell(r, c) {
        const td = board.rows[r]?.cells[c];
        if (!td) return;
        td.className = "";
        td.innerHTML = "";

        if (given[r][c]) {
            td.className = "given";
            td.textContent = grid[r][c];
        } else if (grid[r][c] !== 0) {
            td.className = "player";
            td.textContent = grid[r][c];
        } else if (notes[r][c] && notes[r][c].size > 0) {
            td.className = "notes-cell";
            renderNotes(td, r, c);
        }
    }

    function highlightSelected() {
        // 清除所有高亮
        for (let r = 0; r < 9; r++) {
            for (let c = 0; c < 9; c++) {
                const td = board.rows[r]?.cells[c];
                if (!td) continue;
                td.classList.remove("selected", "highlighted", "same-box");
            }
        }

        if (selectedRow < 0 || selectedCol < 0) return;
        const selVal = grid[selectedRow][selectedCol];

        // 选中格
        const selTd = board.rows[selectedRow]?.cells[selectedCol];
        if (selTd) selTd.classList.add("selected");

        // 同行列高亮
        for (let i = 0; i < 9; i++) {
            const td1 = board.rows[selectedRow]?.cells[i];
            if (td1) td1.classList.add("highlighted");
            const td2 = board.rows[i]?.cells[selectedCol];
            if (td2) td2.classList.add("highlighted");
        }

        // 同宫格高亮
        const br = Math.floor(selectedRow / 3) * 3;
        const bc = Math.floor(selectedCol / 3) * 3;
        for (let r = br; r < br + 3; r++) {
            for (let c = bc; c < bc + 3; c++) {
                const td = board.rows[r]?.cells[c];
                if (td) td.classList.add("same-box");
            }
        }

        // 同数字高亮
        if (selVal !== 0) {
            for (let r = 0; r < 9; r++) {
                for (let c = 0; c < 9; c++) {
                    if (grid[r][c] === selVal) {
                        const td = board.rows[r]?.cells[c];
                        if (td) td.classList.add("highlighted");
                    }
                }
            }
        }
    }

    // ---------- 交互 ----------
    function onCellClick(r, c) {
        if (finished) return;
        selectedRow = r;
        selectedCol = c;
        highlightSelected();
        hintEl.textContent = given[r][c]
            ? "预填数字不可修改"
            : "使用数字按钮或键盘输入 1-9";
    }

    function placeNumber(num) {
        if (finished || selectedRow < 0 || selectedCol < 0) return;
        if (given[selectedRow][selectedCol]) return;

        if (!started) {
            started = true;
            startTimer();
        }

        if (noteMode) {
            // 笔记模式：切换候选数
            if (!notes[selectedRow][selectedCol]) {
                notes[selectedRow][selectedCol] = new Set();
            }
            if (num === 0) {
                notes[selectedRow][selectedCol].clear();
            } else if (notes[selectedRow][selectedCol].has(num)) {
                notes[selectedRow][selectedCol].delete(num);
            } else {
                notes[selectedRow][selectedCol].add(num);
            }
            grid[selectedRow][selectedCol] = 0;
            updateCell(selectedRow, selectedCol);
            highlightSelected();
            updateStats();
            return;
        }

        if (num === 0) {
            // 清除
            if (grid[selectedRow][selectedCol] !== 0) {
                grid[selectedRow][selectedCol] = 0;
                notes[selectedRow][selectedCol] = new Set();
                updateCell(selectedRow, selectedCol);
                highlightSelected();
                updateStats();
            }
            return;
        }

        // 检查是否正确
        const correct = solution[selectedRow][selectedCol] === num;
        if (!correct) {
            errors++;
            errorsEl.textContent = errors;
            const td = board.rows[selectedRow]?.cells[selectedCol];
            if (td) {
                td.classList.add("error");
                setTimeout(() => td.classList.remove("error"), 600);
            }
            playSound(false);
            toast("❌ 数字不正确");
            return;
        }

        // 正确填入
        grid[selectedRow][selectedCol] = num;
        notes[selectedRow][selectedCol] = new Set();
        updateCell(selectedRow, selectedCol);
        playSound(true);
        updateStats();

        // 检查是否完成
        if (checkCompletion()) {
            finishGame(true);
        }
    }

    function checkCompletion() {
        for (let r = 0; r < 9; r++) {
            for (let c = 0; c < 9; c++) {
                if (grid[r][c] === 0) return false;
            }
        }
        return Engine.checkComplete(grid);
    }

    // ---------- 计时 ----------
    function startTimer() {
        const startTime = performance.now();
        timerId = setInterval(() => {
            const elapsed = Math.floor((performance.now() - startTime) / 1000);
            timerSeconds = elapsed;
            timerEl.textContent = formatTime(elapsed);
        }, 1000);
    }

    function stopTimer() {
        if (timerId) clearInterval(timerId);
        timerId = null;
    }

    function formatTime(sec) {
        const m = Math.floor(sec / 60);
        const s = sec % 60;
        return m + ":" + (s < 10 ? "0" : "") + s;
    }

    // ---------- 统计 ----------
    function updateStats() {
        let filled = 0;
        for (let r = 0; r < 9; r++) {
            for (let c = 0; c < 9; c++) {
                if (grid[r][c] !== 0) filled++;
            }
        }
        filledCount = filled;
        progressTextEl.textContent = filled + "/81";
        progressEl.style.width = (filled / 81 * 100) + "%";
        scoreEl.textContent = score;
    }

    // ---------- 计分 ----------
    function calculateScore() {
        const base = 1000;
        const timeBonus = Math.max(0, Math.round((600 - timerSeconds) * 2));
        const errorPenalty = errors * 100;
        const hintPenalty = hintsUsed * helpPenalty;
        return Math.max(0, base + timeBonus - errorPenalty - hintPenalty);
    }

    function getStars() {
        const s = score;
        if (s >= 2500) return 3;
        if (s >= 1500) return 2;
        if (s >= 500) return 1;
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
            const tone = (freq, start, dur, vol = 0.1) => {
                const osc = ctx.createOscillator();
                const gain = ctx.createGain();
                osc.type = "sine";
                osc.frequency.value = freq;
                gain.gain.setValueAtTime(0.0001, now + start);
                gain.gain.exponentialRampToValueAtTime(vol, now + start + 0.02);
                gain.gain.exponentialRampToValueAtTime(0.0001, now + start + dur);
                osc.connect(gain).connect(ctx.destination);
                osc.start(now + start);
                osc.stop(now + start + dur + 0.05);
            };
            if (correct) {
                tone(523.25, 0, 0.1);
                tone(659.25, 0.08, 0.12);
            } else {
                tone(220, 0, 0.18, 0.06);
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
        $("resultTime").textContent = formatTime(timerSeconds);
        $("resultErrors").textContent = errors;
        $("resultHints").textContent = hintsUsed;
        $("resultOverlay").classList.remove("hidden");

        sendToHost({
            type: "gameEnd",
            mode: "sudoku",
            difficulty,
            timeMs: Math.round(timerSeconds * 1000),
            errors,
            score,
            star: stars,
            hintsUsed
        });
    }

    // ---------- 提示功能 ----------
    function useHint() {
        if (finished || selectedRow < 0 || selectedCol < 0) {
            toast("请先选中一个格子");
            return;
        }
        if (given[selectedRow][selectedCol]) {
            toast("预填数字无需提示");
            return;
        }
        if (grid[selectedRow][selectedCol] === solution[selectedRow][selectedCol]) {
            toast("该格已填正确");
            return;
        }

        hintsUsed++;
        const val = solution[selectedRow][selectedCol];
        grid[selectedRow][selectedCol] = val;
        notes[selectedRow][selectedCol] = new Set();
        updateCell(selectedRow, selectedCol);
        highlightSelected();
        updateStats();
        toast(`💡 提示：该格填入 ${val}`);
        playSound(true);

        if (checkCompletion()) {
            finishGame(true);
        }
    }

    // ---------- 检查功能 ----------
    function checkBoard() {
        if (finished) return;
        const errs = Engine.getErrors(grid, solution);
        if (errs.length === 0) {
            toast("✅ 当前无错误，继续加油！");
            return;
        }
        errs.forEach(([r, c]) => {
            const td = board.rows[r]?.cells[c];
            if (td) td.classList.add("error");
        });
        toast(`❌ 发现 ${errs.length} 处错误`);
        setTimeout(() => {
            errs.forEach(([r, c]) => {
                const td = board.rows[r]?.cells[c];
                if (td) td.classList.remove("error");
            });
        }, 1500);
    }

    // ---------- 键盘输入 ----------
    document.addEventListener("keydown", (e) => {
        if (finished) return;
        if (e.key >= "1" && e.key <= "9") {
            e.preventDefault();
            placeNumber(parseInt(e.key));
        } else if (e.key === "Backspace" || e.key === "Delete" || e.key === "0") {
            e.preventDefault();
            placeNumber(0);
        } else if (e.key === "n" || e.key === "N") {
            toggleNoteMode();
        }
    });

    // ---------- 笔记模式切换 ----------
    function toggleNoteMode() {
        if (finished) return;
        noteMode = !noteMode;
        btnToggleNote.classList.toggle("active", noteMode);
        hintEl.textContent = noteMode
            ? "✏️ 笔记模式：点击数字切换候选数标记"
            : "点击格子选中，用数字按钮或键盘填入数字 1-9";
    }

    // ---------- 初始化 ----------
    function boot(data, themeName, meta) {
        difficulty = data.difficulty || "普通";
        const clueCount = data.clueCount || 30;
        helpPenalty = data.helpPenalty || 100;
        applyTheme(themeName);

        // 生成题目
        const result = Engine.generate(clueCount);
        grid = result.puzzle.map(row => [...row]);
        solution = result.solution.map(row => [...row]);
        given = result.puzzle.map(row => row.map(v => v !== 0));
        notes = Array.from({ length: 9 }, () => Array.from({ length: 9 }, () => new Set()));

        resetGame();
        renderBoard();
    }

    function resetGame() {
        selectedRow = -1;
        selectedCol = -1;
        noteMode = false;
        errors = 0;
        score = 0;
        timerSeconds = 0;
        started = false;
        finished = false;
        hintsUsed = 0;
        filledCount = 0;

        btnToggleNote.classList.remove("active");
        timerEl.textContent = "0:00";
        errorsEl.textContent = "0";
        scoreEl.textContent = "0";
        progressTextEl.textContent = "0/81";
        progressEl.style.width = "0%";
        hintEl.textContent = "点击格子选中，用数字按钮或键盘填入数字 1-9";
        stopTimer();
    }

    function loadData() {
        listenInit((data, theme, meta) => boot(data, theme, meta));
        if (isMock()) boot(MOCK_CONFIG, "light");
    }

    // ---------- 事件绑定 ----------
    // 数字按钮
    document.querySelectorAll(".num-btn").forEach((btn) => {
        btn.addEventListener("click", () => {
            const num = parseInt(btn.dataset.num);
            placeNumber(num);
        });
    });

    // 控制按钮
    btnToggleNote.addEventListener("click", toggleNoteMode);

    btnHint.addEventListener("click", useHint);

    btnCheck.addEventListener("click", checkBoard);

    // 重新开始
    $("btnRestart").addEventListener("click", () => {
        if (window.GameUI.bridge()) { sendToHost({ type: "restart" }); return; }
        const result = Engine.generate(
            difficulty === "简单" ? 40 : difficulty === "困难" ? 24 : 30
        );
        grid = result.puzzle.map(row => [...row]);
        solution = result.solution.map(row => [...row]);
        given = result.puzzle.map(row => row.map(v => v !== 0));
        notes = Array.from({ length: 9 }, () => Array.from({ length: 9 }, () => new Set()));
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