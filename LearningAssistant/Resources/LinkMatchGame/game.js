(() => {
    "use strict";

    // 公共工具
    const { toast, sendToHost, speak, esc, listenInit, isMock, applyTheme, shuffle } = window.GameUI;

    // ---------- 常量 ----------
    const ROWS = 5, COLS = 8;          // 内层盘面
    const ER = ROWS + 2, EC = COLS + 2; // 扩展网格（带一圈空边界，允许绕边）

    // ---------- 状态 ----------
    let items = [];      // 原始数据 [{id, word, meaning, phonetic}]
    let cards = [];      // 全部卡 2N [{key, type, item}]
    let grid = [];       // ERxEC，元素 = card | 0
    let selected = -1;   // 第一张选中卡的全局索引
    let matchedCount = 0, pairsCount = 0;
    let score = 0, combo = 0, maxCombo = 0, moves = 0, errors = 0;
    let startTimerMs = 0, seconds = 0, timerId = null;
    let started = false, finished = false;
    let wrongKeys = new Set();
    let results = [];    // {id, correct}

    // ---------- DOM ----------
    const $ = (id) => document.getElementById(id);
    const board = $("board"), hint = $("hint");
    const scoreEl = $("score"), comboEl = $("combo"), movesEl = $("moves"),
          remainingEl = $("remaining"), timerEl = $("timer"), progressEl = $("progress");

    const MOCK_ITEMS = [
        { id: "m1", word: "apple", meaning: "苹果", phonetic: "/ˈæpl/" },
        { id: "m2", word: "banana", meaning: "香蕉", phonetic: "/bəˈnɑːnə/" },
        { id: "m3", word: "cat", meaning: "猫", phonetic: "/kæt/" },
        { id: "m4", word: "dog", meaning: "狗", phonetic: "/dɒɡ/" },
        { id: "m5", word: "elephant", meaning: "大象", phonetic: "/ˈelɪfənt/" },
        { id: "m6", word: "fish", meaning: "鱼", phonetic: "/fɪʃ/" },
        { id: "m7", word: "grape", meaning: "葡萄", phonetic: "/ɡreɪp/" },
        { id: "m8", word: "house", meaning: "房子", phonetic: "/haʊs/" },
        { id: "m9", word: "lemon", meaning: "柠檬", phonetic: "/ˈlemən/" },
        { id: "m10", word: "moon", meaning: "月亮", phonetic: "/muːn/" },
    ];

    // ---------- 配牌 ----------
    function buildDeck() {
        const deck = [];
        items.forEach((it, i) => {
            deck.push({ key: i, type: "word", item: it });
            deck.push({ key: i, type: "meaning", item: it });
        });
        return shuffle(deck);
    }

    // 初始化网格：把所有卡随机放入内层格，返回 grid。
    // 若无可消对则重排（最多尝试 SHUFFLE_TRIES 次）。
    const SHUFFLE_TRIES = 60;
    function initGrid(deck) {
        const innerCells = [];
        for (let r = 1; r <= ROWS; r++) for (let c = 1; c <= COLS; c++) innerCells.push([r, c]);
        const cells = shuffle(innerCells.slice());
        for (let attempt = 0; attempt < SHUFFLE_TRIES; attempt++) {
            const g = Array.from({ length: ER }, () => new Array(EC).fill(0));
            shuffle(cells);
            deck.forEach((card, i) => {
                const [r, c] = cells[i];
                card.r = r; card.c = c;
                g[r][c] = card;
            });
            if (hasSolvablePair(g)) return g;
        }
        // 兜底：直接返回最后一次布局，死局交给自动洗牌处理
        const g = Array.from({ length: ER }, () => new Array(EC).fill(0));
        deck.forEach((card, i) => {
            const [r, c] = cells[i];
            card.r = r; card.c = c;
            g[r][c] = card;
        });
        return g;
    }

    // ---------- 路径判定（核心） ----------
    function lineClear(row, c1, c2) {
        const step = c1 < c2 ? 1 : -1;
        for (let c = c1 + step; c !== c2; c += step) if (grid[row][c] !== 0) return false;
        return true;
    }
    function colClear(r1, r2, col) {
        const step = r1 < r2 ? 1 : -1;
        for (let r = r1 + step; r !== r2; r += step) if (grid[r][col] !== 0) return false;
        return true;
    }
    /** 寻找 (sr,sc)->(tr,tc) 之间 ≤2 次转弯的空路径；返回拐点列表，找不到返回 null。 */
    function findPath(sr, sc, tr, tc) {
        // 0 转
        if (sr === tr && lineClear(sr, sc, tc)) return [[sr, sc], [tr, tc]];
        if (sc === tc && colClear(sr, tr, sc)) return [[sr, sc], [tr, tc]];
        // 1 转
        if (grid[sr][tc] === 0 && lineClear(sr, sc, tc) && colClear(sr, tr, tc))
            return [[sr, sc], [sr, tc], [tr, tc]];
        if (grid[tr][sc] === 0 && colClear(sr, tr, sc) && lineClear(tr, sc, tc))
            return [[sr, sc], [tr, sc], [tr, tc]];
        // 2 转：按行扫描
        for (let r = 0; r < ER; r++) {
            if (r === sr || r === tr) continue;
            if (grid[r][sc] === 0 && grid[r][tc] === 0 &&
                colClear(sr, r, sc) && lineClear(r, sc, tc) && colClear(r, tr, tc))
                return [[sr, sc], [r, sc], [r, tc], [tr, tc]];
        }
        // 2 转：按列扫描
        for (let c = 0; c < EC; c++) {
            if (c === sc || c === tc) continue;
            if (grid[sr][c] === 0 && grid[tr][c] === 0 &&
                lineClear(sr, sc, c) && colClear(sr, tr, c) && lineClear(tr, c, tc))
                return [[sr, sc], [sr, c], [tr, c], [tr, tc]];
        }
        return null;
    }

    // 是否存在至少一对可消（用于开局可解 / 死局检测）
    function hasSolvablePair(g) {
        const save = grid;
        grid = g;
        let ok = false;
        for (let i = 0; i < cards.length && !ok; i++) {
            const a = cards[i];
            if (grid[a.r][a.c] === 0) continue;
            for (let j = i + 1; j < cards.length; j++) {
                const b = cards[j];
                if (grid[b.r][b.c] === 0) continue;
                if (a.key === b.key && a.type !== b.type &&
                    findPath(a.r, a.c, b.r, b.c)) { ok = true; break; }
            }
        }
        grid = save;
        return ok;
    }

    function anySolvable() { return hasSolvablePair(grid); }

    // ---------- 渲染 ----------
    const WORD_GRADS = [["#6366f1", "#8b5cf6"], ["#06b6d4", "#3b82f6"], ["#2563eb", "#06b6d4"], ["#8b5cf6", "#d946ef"], ["#0ea5e9", "#6366f1"]];
    const MEANING_GRADS = [["#ec4899", "#f43f5e"], ["#f97316", "#f43f5e"], ["#fb7185", "#f97316"], ["#db2777", "#9333ea"], ["#f43f5e", "#ec4899"]];

    function renderBoard() {
        board.innerHTML = "";
        // 铺 8 列空格子，保持 grid 布局稳定（卡片 absolute 于格内）
        for (let r = 0; r < ROWS; r++) {
            for (let c = 0; c < COLS; c++) {
                const cell = document.createElement("div");
                cell.className = "cell";
                const card = grid[r + 1][c + 1];
                if (card && card !== 0) {
                    cell.appendChild(buildCardEl(card));
                }
                board.appendChild(cell);
            }
        }
        remainingEl.textContent = pairsCount - matchedCount;
        updateProgress();
    }

    function buildCardEl(card) {
        const el = document.createElement("div");
        el.className = "card";
        el.dataset.key = card.key;
        el.dataset.type = card.type;
        const isWord = card.type === "word";
        const grad = isWord
            ? WORD_GRADS[Math.floor(Math.random() * WORD_GRADS.length)]
            : MEANING_GRADS[Math.floor(Math.random() * MEANING_GRADS.length)];
        el.style.setProperty("--grad-a", grad[0]);
        el.style.setProperty("--grad-b", grad[1]);

        const content = isWord
            ? `<span class="word-text">${esc(card.item.word)}</span>` +
              (card.item.phonetic ? `<span class="phonetic">${esc(card.item.phonetic)}</span>` : "") +
              `<button class="speak">🔊</button>`
            : `<span class="meaning-text">${esc(card.item.meaning)}</span>`;
        el.innerHTML = content;

        if (isWord) {
            el.querySelector(".speak").addEventListener("click", (e) => {
                e.stopPropagation();
                speak(card.item.word);
            });
        }
        el.addEventListener("click", () => onCardClick(card, el));
        return el;
    }

    // ---------- 点选逻辑 ----------
    function onCardClick(card, el) {
        if (finished) return;
        if (!started) { started = true; startTimer(); }

        if (selected === -1) {
            selected = card;
            el.classList.add("selected");
            hint.textContent = "再点一张与之配对且路径可通的卡片";
            return;
        }

        const first = selected;
        const firstEl = board.querySelector(`.card[data-key="${first.key}"][data-type="${first.type}"]`);
        selected = -1;
        if (first === card) {
            firstEl.classList.remove("selected");
            el.classList.remove("selected");
            hint.textContent = "点击一张卡片，再点它的配对卡片（单词 ↔ 释义）";
            return;
        }

        moves++;
        movesEl.textContent = moves;
        if (first.type === card.type || first.key !== card.key) {
            // 点错配对（同类型 或 不同词条）
            markWeak(first); markWeak(card);
            combo = 0; comboEl.textContent = combo;
            errors++;
            const penalty = first.type === card.type ? 4 : 8;
            score = Math.max(0, score - penalty);
            scoreEl.textContent = score;
            firstEl.classList.add("invalid");
            el.classList.add("invalid");
            toast(first.type === card.type ? "❌ 需单词+释义各一张" : "❌ 不是同一词条的配对");
            hint.textContent = "这两张无法消除：需要互为配对（单词 ↔ 释义）";
            setTimeout(() => { firstEl.classList.remove("invalid"); el.classList.remove("invalid"); }, 600);
            return;
        }

        // 互为配对：检查路径
        const path = findPath(first.r, first.c, card.r, card.c);
        if (!path) {
            markWeak(first); markWeak(card);
            combo = 0;
            score = Math.max(0, score - 2);
            scoreEl.textContent = score;
            firstEl.classList.add("invalid");
            el.classList.add("invalid");
            toast("🚧 两点之间没有 ≤2 次转弯的空路");
            hint.textContent = "尝试预留空格或绕开卡片，再来一次";
            setTimeout(() => { firstEl.classList.remove("invalid"); el.classList.remove("invalid"); }, 600);
            return;
        }

        eliminate(first, card, path);
    }

    // 薄弱词角标（词条 key 对应两张卡各标一次）
    function markWeak(card) {
        wrongKeys.add(card.key);
        board.querySelectorAll(`.card[data-key="${card.key}"]`).forEach((d) => d.classList.add("weak"));
    }

    // ---------- 消除与计分 ----------
    function eliminate(a, b, path) {
        const firstEl = board.querySelector(`.card[data-key="${a.key}"][data-type="${a.type}"]`);
        const secondEl = board.querySelector(`.card[data-key="${b.key}"][data-type="${b.type}"]`);

        combo++;
        maxCombo = Math.max(maxCombo, combo);
        const gain = 10 + combo * 5;
        score += gain;
        scoreEl.textContent = score;
        comboEl.textContent = combo;

        const correct = !wrongKeys.has(a.key);
        results.push({ id: a.item.id, correct });
        if (wrongKeys.has(a.key)) wrongKeys.delete(a.key);

        matchedCount++;
        remainingEl.textContent = pairsCount - matchedCount;
        firstEl.classList.remove("selected");
        firstEl.classList.add("matched");
        secondEl.classList.add("matched");

        grid[a.r][a.c] = 0;
        grid[b.r][b.c] = 0;

        showPath(path);
        toast(`✅ 正确 +${gain}  连击 x${combo}`);
        updateProgress();

        setTimeout(() => {
            renderBoard();
            // 死局自动洗牌
            if (matchedCount < pairsCount && !anySolvable()) {
                const deck = cards.filter((c) => grid[c.r][c.c] !== 0);
                grid = initGrid(deck);
                renderBoard();
                toast("🃏 无可消配对，已自动洗牌");
            }
            if (matchedCount >= pairsCount) finishGame();
        }, 430);
    }

    // 在 SVG 上绘制路径（基于格的相对坐标 → 屏幕坐标）
    let lastPathSvg = null;
    function showPath(path) {
        const svg = $("pathSvg");
        if (lastPathSvg) { lastPathSvg.remove(); lastPathSvg = null; }
        const svgRect = svg.getBoundingClientRect();
        const pts = path.map(([r, c]) => {
            const cell = board.querySelectorAll(".cell")[(r - 1) * COLS + (c - 1)];
            if (!cell) return null;
            const rect = cell.getBoundingClientRect();
            return { x: rect.left + rect.width / 2 - svgRect.left, y: rect.top + rect.height / 2 - svgRect.top };
        });
        if (pts.some((p) => !p)) return;
        let d = `M ${pts[0].x} ${pts[0].y}`;
        for (let i = 1; i < pts.length; i++) d += ` L ${pts[i].x} ${pts[i].y}`;
        const line = document.createElementNS("http://www.w3.org/2000/svg", "path");
        line.setAttribute("d", d);
        line.classList.add("path-line");
        svg.appendChild(line);
        lastPathSvg = line;
        setTimeout(() => { if (line.parentNode) line.parentNode.removeChild(line); if (lastPathSvg === line) lastPathSvg = null; }, 700);
    }

    function updateProgress() {
        const total = cards.length;
        const done = matchedCount * 2;
        progressEl.style.width = (total ? (done / total) * 100 : 0) + "%";
    }

    // ---------- 计时 ----------
    function startTimer() {
        if (timerId) return;
        startTimerMs = Date.now();
        timerId = setInterval(() => {
            seconds = Math.floor((Date.now() - startTimerMs) / 1000);
            timerEl.textContent = seconds + "s";
        }, 500);
    }
    function stopTimer() {
        if (timerId) clearInterval(timerId);
        timerId = null;
    }

    // ---------- 结算 ----------
    function finishGame() {
        if (finished) return;
        finished = true;
        stopTimer();

        const eff = moves / (pairsCount || 1);
        let stars = 3;
        if (errors > 0 || eff > 1.5) stars = 2;
        if (eff > 2.2 || errors >= 4) stars = 1;
        const emoji = stars >= 3 ? "🏆" : stars === 2 ? "🎉" : "😊";

        $("resultEmoji").textContent = emoji;
        $("resultTitle").textContent = stars === 1 ? "加油，再来一次！" : "通关啦！";
        $("resultStars").innerHTML =
            `<span class="${stars >= 1 ? "lit" : "dim"}">★</span>` +
            `<span class="${stars >= 2 ? "lit" : "dim"}">★</span>` +
            `<span class="${stars >= 3 ? "lit" : "dim"}">★</span>`;
        $("resultScore").textContent = score;
        $("resultMoves").textContent = moves + " 步";
        $("resultErrors").textContent = errors;
        $("resultTime").textContent = seconds + "s";
        $("resultOverlay").classList.remove("hidden");

        sendToHost({ type: "gameEnd", results, score, moves, errors, combo: maxCombo, seconds });
    }

    // ---------- 初始化 ----------
    function boot(data, themeName) {
        items = (data || []).filter((d) => d && d.word && d.meaning);
        applyTheme(themeName);
        resetGame();
        if (items.length < 2) {
            board.innerHTML = `<div style="grid-column:1/-1;text-align:center;color:var(--muted);padding:40px;">词库内容不足，请先在「内容编辑」中添加单词</div>`;
            return;
        }
        cards = buildDeck();
        grid = initGrid(cards);
        renderBoard();
    }

    function resetGame() {
        score = 0; combo = 0; maxCombo = 0; moves = 0; errors = 0;
        seconds = 0; started = false; finished = false;
        matchedCount = 0; pairsCount = items.length;
        wrongKeys = new Set(); results = []; selected = -1;
        grid = []; lastPathSvg = null;
        scoreEl.textContent = "0";
        comboEl.textContent = "0";
        movesEl.textContent = "0";
        timerEl.textContent = "0s";
        progressEl.style.width = "0%";
        stopTimer();
    }

    function loadData() {
        listenInit((data, theme) => boot(data, theme));
        if (isMock()) boot(MOCK_ITEMS, "light");
    }

    // ---------- 按钮 ----------
    $("btnShuffle").addEventListener("click", () => {
        if (items.length === 0 || finished) return;
        const deck = cards.filter((c) => grid[c.r][c.c] !== 0);
        if (deck.length < 2) return;
        grid = initGrid(deck);
        renderBoard();
        toast("🃏 已重新洗牌");
    });

    $("btnRestart").addEventListener("click", () => {
        if (window.GameUI.bridge()) sendToHost({ type: "restart" });
        else if (items.length > 0) { resetGame(); cards = buildDeck(); grid = initGrid(cards); renderBoard(); }
    });

    $("btnAgain").addEventListener("click", () => {
        $("resultOverlay").classList.add("hidden");
        if (items.length === 0) return;
        resetGame();
        cards = buildDeck();
        grid = initGrid(cards);
        renderBoard();
    });
    $("btnClose").addEventListener("click", () => $("resultOverlay").classList.add("hidden"));

    loadData();
})();