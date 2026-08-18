(() => {
    "use strict";

    // 公共工具从 GameUI 取
    const { toast, sendToHost, speak, esc, listenInit, isMock, applyTheme, shuffle } = window.GameUI;

    // ---------- 状态 ----------
    let items = [];        // 原始数据 [{id, word, meaning, phonetic}]
    let cards = [];        // 12N 张卡 [{key, type, item}]
    let pairsCount = 0;    // 对数 N
    let flipped = [];      // 当前翻开的两张卡索引
    let locked = false;    // 判定期间锁定翻牌
    let matchedCount = 0;  // 已配对数量
    let score = 0;
    let totalScore = 0;    // 跨组累计总分（换一组不清零）
    let combo = 0;
    let maxCombo = 0;
    let moves = 0;         // 有效判定次数（配对成功/失败）
    let errors = 0;        // 配对失败次数
    let wrongKeys = new Set(); // 曾卷入错误配对的局内 key
    let results = [];      // 每对结果 {id, correct}
    let timerSeconds = 0;
    let timerId = null;
    let started = false;
    let finished = false;

    // ---------- DOM ----------
    const $ = (id) => document.getElementById(id);
    const board = $("board");
    const hint = $("hint");
    const scoreEl = $("score");
    const comboEl = $("combo");
    const movesEl = $("moves");
    const timerEl = $("timer");
    const progressEl = $("progress");
    const totalScoreEl = $("totalScore");
    const totalRemainingEl = $("totalRemaining");

    // ---------- 数据 ----------
    const MOCK_ITEMS = [
        { id: "m1", word: "apple", meaning: "苹果", phonetic: "/ˈæpl/" },
        { id: "m2", word: "banana", meaning: "香蕉", phonetic: "/bəˈnɑːnə/" },
        { id: "m3", word: "cat", meaning: "猫", phonetic: "/kæt/" },
        { id: "m4", word: "dog", meaning: "狗", phonetic: "/dɒɡ/" },
        { id: "m5", word: "elephant", meaning: "大象", phonetic: "/ˈelɪfənt/" },
        { id: "m6", word: "fish", meaning: "鱼", phonetic: "/fɪʃ/" },
        { id: "m7", word: "grape", meaning: "葡萄", phonetic: "/ɡreɪp/" },
        { id: "m8", word: "house", meaning: "房子", phonetic: "/haʊs/" },
    ];

    // ---------- 配牌 ----------
    function buildCards() {
        const deck = [];
        items.forEach((it, i) => {
            deck.push({ key: i, type: "word", item: it });
            deck.push({ key: i, type: "meaning", item: it });
        });
        cards = shuffle(deck);
        pairsCount = items.length;
    }

    function renderBoard() {
        board.innerHTML = "";
        cards.forEach((card, idx) => {
            const el = document.createElement("div");
            el.className = "card";

            const isWord = card.type === "word";
            // 喇叭作为视觉标识（右上角），翻开单词卡即自动朗读
            const frontInner = isWord
                ? `<span class="speak" aria-hidden="true">🔊</span>` +
                  `<span class="word-text">${esc(card.item.word)}</span>` +
                  (card.item.phonetic ? `<span class="phonetic">${esc(card.item.phonetic)}</span>` : "")
                : `<span class="meaning-text">${esc(card.item.meaning)}</span>`;

            el.innerHTML = `
                <div class="face face-back">🧠</div>
                <div class="face face-front ${isWord ? "word" : "meaning"}">${frontInner}</div>`;

            el.addEventListener("click", () => onCardClick(idx, el));
            el.style.animationDelay = (idx * 45) + "ms";
            board.appendChild(el);
        });
    }

    // ---------- 翻牌逻辑 ----------
    function onCardClick(idx, el) {
        if (finished || locked) return;
        if (el.classList.contains("flipped")) return; // 已翻开/已配对，不可再点

        if (!started) {
            started = true;
            startTimer();
        }

        el.classList.add("flipped");
        flipped.push(idx);
        // 翻开单词卡自动朗读（喇叭仅作视觉标识）
        const card = cards[idx];
        if (card.type === "word") speak(card.item.word);

        if (flipped.length === 2) {
            judge(idx);
        } else {
            hint.textContent = "再翻一张；翻到同一类型会自动退回，不计数";
        }
    }

    function judge(secondIdx) {
        const firstIdx = flipped[0];
        const a = cards[firstIdx], b = cards[secondIdx];
        const aEl = board.children[firstIdx];
        const bEl = board.children[secondIdx];

        // 同一类型（word+word 或 meaning+meaning）：快速退回，不计步数、不计错误
        if (a.type === b.type) {
            flipped = [];
            setTimeout(() => {
                aEl.classList.remove("flipped");
                bEl.classList.remove("flipped");
                hint.textContent = "请翻一张单词卡 + 一张释义卡进行配对";
            }, 380);
            return;
        }

        locked = true;
        moves++;
        movesEl.textContent = moves;
        updateProgress();

        if (a.key === b.key) {
            // 配对成功
            combo++;
            maxCombo = Math.max(maxCombo, combo);
            const gain = 10 + combo * 5;
            score += gain;
            scoreEl.textContent = score;
            comboEl.textContent = combo;

            const wordItem = a.type === "word" ? a.item : b.item;
            const correct = !wrongKeys.has(a.key);
            results.push({ id: wordItem.id, correct });
            if (!correct) wrongKeys.delete(a.key);

            matchedCount++;
            aEl.classList.add("matched");
            bEl.classList.add("matched");
            GameUI.playSound("match");
            toast(`✅ 正确 +${gain}  连击 x${combo}`);
            flipped = [];
            locked = false;

            if (matchedCount >= pairsCount) {
                finishGame();
            } else {
                hint.textContent = `配对成功！还剩 ${pairsCount - matchedCount} 对`;
            }
        } else {
            // 配对失败：记录卷入错误配对，抖动后翻回
            wrongKeys.add(a.key);
            wrongKeys.add(b.key);
            combo = 0;
            comboEl.textContent = combo;
            errors++;
            score = Math.max(0, score - 5);
            scoreEl.textContent = score;
            aEl.classList.add("wrong");
            bEl.classList.add("wrong");
            GameUI.playSound("error");
            toast("❌ 不配对，记住位置再试试");
            setTimeout(() => {
                aEl.classList.remove("wrong", "flipped");
                bEl.classList.remove("wrong", "flipped");
                flipped = [];
                locked = false;
                hint.textContent = "翻两张：一张单词 + 一张释义，配对成功即消除";
            }, 900);
        }
    }

    function updateProgress() {
        const total = cards.length;
        const done = matchedCount * 2;
        progressEl.style.width = (total ? (done / total) * 100 : 0) + "%";
    }

    // ---------- 计时 ----------
    function startTimer() {
        if (timerId) return;
        timerId = setInterval(() => {
            timerSeconds++;
            timerEl.textContent = timerSeconds + "s";
        }, 1000);
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

        // 星级：按效率（步数/对数）与错误次数综合评定
        const eff = moves / (pairsCount || 1);
        let stars = 3;
        if (errors > 0 || eff > 1.6) stars = 2;
        if (eff > 2.4 || errors >= 3) stars = 1;
        const emoji = stars >= 3 ? "🏆" : stars === 2 ? "🎉" : "😊";

        $("resultEmoji").textContent = emoji;
        $("resultTitle").textContent = stars === 1 ? "加油，再来一次！" : "通关啦！";
        $("resultStars").innerHTML =
            (stars >= 1 ? "★" : "☆") + (stars >= 2 ? "★" : "☆") + (stars >= 3 ? "★" : "☆");
        $("resultScore").textContent = score;
        totalScoreEl.textContent = totalScore + score;
        $("resultMoves").textContent = moves + " 步";
        $("resultOptimal").textContent = pairsCount + " 步";
        $("resultTime").textContent = timerSeconds + "s";
        $("resultOverlay").classList.remove("hidden");

        sendToHost({ type: "gameEnd", results, score, moves, errors, combo: maxCombo, seconds: timerSeconds });
    }

    // ---------- 初始化 ----------
    function boot(data, themeName, meta) {
        items = (data || []).filter((d) => d && d.word && d.meaning);
        applyTheme(themeName);
        // 按顶部"列"设置应用盘面列数
        if (meta && meta.cols) {
            board.style.gridTemplateColumns = `repeat(${meta.cols}, minmax(140px, 1fr))`;
        }
        resetGame();
        if (items.length === 0) {
            board.innerHTML = `<div style="grid-column:1/-1;text-align:center;color:var(--muted);padding:40px;">词库为空或不足，请先在「内容编辑」中添加单词</div>`;
            return;
        }
        // 总剩余条目数
        totalRemainingEl.textContent = GameUI.metaTotalRemaining(meta);
        // "跳过已知项/加载所有"单选，切换时通知宿主保存
        const skipRadio = GameUI.initSkipKnownRadios("skipKnownGroup", (skip) => {
            GameUI.sendToHost({ type: "setting", skipKnown: skip });
        });
        if (meta && typeof meta.skipKnown === "boolean") skipRadio.set(meta.skipKnown);

        buildCards();
        renderBoard();
    }

    function resetGame() {
        // 换一组时总分在上一次基础上累计，不清零
        totalScore += score;
        score = 0; combo = 0; maxCombo = 0; moves = 0; errors = 0;
        timerSeconds = 0; started = false; finished = false;
        matchedCount = 0; pairsCount = 0; wrongKeys = new Set(); results = [];
        flipped = []; locked = false;
        scoreEl.textContent = "0";
        totalScoreEl.textContent = totalScore;
        comboEl.textContent = "0";
        movesEl.textContent = "0";
        timerEl.textContent = "0s";
        progressEl.style.width = "0%";
        stopTimer();
    }

    function loadData() {
        listenInit((data, theme, meta) => boot(data, theme, meta));
        if (isMock()) boot(MOCK_ITEMS, "light");
    }

    // "换一组"：请求宿主重新抽词并下发新数据
    $("btnRestart").addEventListener("click", () => {
        if (bridge()) {
            sendToHost({ type: "restart" });
        } else if (items.length > 0) {
            resetGame();
            buildCards();
            renderBoard();
        }
    });
    // 结算页"再来一局"（=继续）：与"换一组"一致，请求宿主重新抽词并排除已答对
    $("btnAgain").addEventListener("click", () => {
        $("resultOverlay").classList.add("hidden");
        $("btnRestart").click();
    });
    $("btnClose").addEventListener("click", () => $("resultOverlay").classList.add("hidden"));

    // 桥接引用（仅 "换一组" 判断使用）
    function bridge() {
        return window.GameUI.bridge();
    }

    loadData();
})();