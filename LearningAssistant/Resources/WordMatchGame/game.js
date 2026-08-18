(() => {
    "use strict";

    // 公共工具从 GameUI 取：GameUI.sendToHost / toast / speak / esc / listenInit / isMock
    const { toast, sendToHost, speak, esc, listenInit, isMock, applyTheme, initSkipKnownRadios, metaTotalRemaining } = window.GameUI;

    // ---------- 状态 ----------
    let items = [];        // 原始数据 [{id, word, meaning, phonetic, example}]
    let cards = [];        // 生成的卡片 [{key, type, item}]
    let selected = null;   // 当前选中的单词卡索引
    let score = 0;         // 本组得分
    let totalScore = 0;    // 跨组累计总分（换一组不清零）
    let combo = 0;
    let maxCombo = 0;
    let remaining = 0;
    let timerSeconds = 0;
    let timerId = null;
    let started = false;
    let finished = false;
    let results = [];      // 每对结果 {id, correct}

    // ---------- DOM ----------
    const $ = (id) => document.getElementById(id);
    const board = $("board");
    const hint = $("hint");
    const scoreEl = $("score");
    const totalScoreEl = $("totalScore");
    const comboEl = $("combo");
    const remainingEl = $("remaining");
    const totalRemainingEl = $("totalRemaining");
    const timerEl = $("timer");
    const progressEl = $("progress");

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

    // 卡片多彩配色：单词走冷色系、释义走暖色系（浅色块块，降低视觉刺激）
    const WORD_COLORS = [
        "linear-gradient(135deg, #e0e7ff, #c7d2fe)",
        "linear-gradient(135deg, #cffafe, #a5f3fc)",
        "linear-gradient(135deg, #dbeafe, #bfdbfe)",
        "linear-gradient(135deg, #ede9fe, #ddd6fe)",
        "linear-gradient(135deg, #e0f2fe, #bae6fd)",
    ];
    const MEANING_COLORS = [
        "linear-gradient(135deg, #ffe4e6, #fecdd3)",
        "linear-gradient(135deg, #ffedd5, #fed7aa)",
        "linear-gradient(135deg, #fce7f3, #fbcfe8)",
        "linear-gradient(135deg, #fff7ed, #fed7aa)",
        "linear-gradient(135deg, #ffe4e6, #fda4af)",
    ];

    // ---------- 配牌 ----------
    function buildCards() {
        cards = [];
        const pairs = items.map((it, i) => ({ key: i, item: it }));
        const deck = [];
        pairs.forEach(({ key, item }) => {
            deck.push({ key, type: "word", item });
            deck.push({ key, type: "meaning", item });
        });
        cards = GameUI.shuffle(deck);
        remaining = deck.length;
        remainingEl.textContent = remaining;
    }

    function renderBoard() {
        board.innerHTML = "";
        cards.forEach((card, idx) => {
            const el = document.createElement("div");
            el.className = "card " + card.type;

            const palette = card.type === "word" ? WORD_COLORS : MEANING_COLORS;
            el.style.setProperty("--card-grad", palette[Math.floor(Math.random() * palette.length)]);
            el.style.setProperty("--tilt", (Math.random() * 4 - 2).toFixed(1) + "deg");
            el.style.setProperty("--card-radius", (14 + Math.floor(Math.random() * 9)) + "px");

            if (card.type === "word") {
                el.innerHTML = `<span class="word-text">${esc(card.item.word)}</span>` +
                    (card.item.phonetic ? `<span class="phonetic">${esc(card.item.phonetic)}</span>` : "") +
                    `<button class="speak" title="朗读">🔊</button>`;
                el.querySelector(".speak").addEventListener("click", (e) => {
                    e.stopPropagation();
                    speak(card.item.word);
                });
            } else {
                el.innerHTML = `<span class="meaning-text">${esc(card.item.meaning)}</span>`;
            }
            el.addEventListener("click", () => onCardClick(idx, el));
            board.appendChild(el);
        });
    }

    // ---------- 交互 ----------
    function onCardClick(idx, el) {
        if (finished) return;
        const card = cards[idx];
        if (el.classList.contains("eliminated")) return;

        if (!started) {
            started = true;
            startTimer();
        }

        if (selected !== null) {
            const selEl = board.children[selected];
            const selCard = cards[selected];

            if (selected === idx) { // 点同一个取消
                selEl.classList.remove("selected");
                selected = null;
                hint.textContent = "请先点一张卡片，再点它的配对卡片";
                return;
            }

            // 两张同类卡片：切换选中
            if (card.type === selCard.type) {
                selEl.classList.remove("selected");
                selected = idx;
                el.classList.add("selected");
                hint.textContent = "请点击该卡片的配对卡片";
                return;
            }

            // 一张单词 + 一张释义：按同一词条的局内 key 匹配（避免词库 Id 为空误判）
            const match = selCard.key === card.key;
            selEl.classList.remove("selected");
            selected = null;

            const wordEl = selCard.type === "word" ? selEl : el;
            const wordItem = selCard.type === "word" ? selCard.item : card.item;
            const meaningEl = selCard.type === "meaning" ? selEl : el;

            if (match) {
                combo++;
                maxCombo = Math.max(maxCombo, combo);
                const gain = 10 + combo * 5;
                score += gain;
                scoreEl.textContent = score;
                comboEl.textContent = combo;
                results.push({ id: wordItem.id, correct: true });
                resolvePair(wordEl, meaningEl);
                toast(`✅ 正确 +${gain}  连击 x${combo}`);
            } else {
                combo = 0;
                comboEl.textContent = combo;
                results.push({ id: wordItem.id, correct: false });
                wordEl.classList.add("wrong");
                meaningEl.classList.add("wrong");
                toast("❌ 不匹配，再试试");
                setTimeout(() => {
                    wordEl.classList.remove("wrong");
                    meaningEl.classList.remove("wrong");
                }, 500);
            }
            return;
        }

        selected = idx;
        el.classList.add("selected");
        hint.textContent = "请点击该卡片的配对卡片（单词↔释义）";
    }

    function resolvePair(wordEl, meaningEl) {
        wordEl.classList.add("eliminated");
        meaningEl.classList.add("eliminated");
        remaining -= 2;
        remainingEl.textContent = remaining;
        updateProgress();
        if (remaining <= 0) finishGame(true);
    }

    function updateProgress() {
        const total = cards.length;
        const done = total - remaining;
        progressEl.style.width = (total ? (done / total) * 100 : 0) + "%";
    }

    // ---------- 计时 ----------
    function startTimer() {
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
    function finishGame(won) {
        if (finished) return;
        finished = true;
        stopTimer();
        // 结算时总分把本组得分计入展示（累计到下一组由 resetGame 完成）
        totalScoreEl.textContent = totalScore + score;
        const total = results.length || 1;
        const correct = results.filter((r) => r.correct).length;
        const rate = Math.round((correct / total) * 100);
        $("resultEmoji").textContent = rate >= 90 ? "🏆" : rate >= 70 ? "🎉" : rate >= 50 ? "😊" : "💪";
        $("resultTitle").textContent = won ? "通关啦！" : "游戏结束";
        $("resultScore").textContent = score;
        $("resultTotal").textContent = totalScore + score;
        $("resultRate").textContent = rate + "%";
        $("resultCombo").textContent = maxCombo;
        $("resultTime").textContent = timerSeconds + "s";
        $("resultOverlay").classList.remove("hidden");
        sendToHost({ type: "gameEnd", results, score, combo: maxCombo, seconds: timerSeconds });
    }

    // ---------- 初始化 ----------
    function boot(data, themeName, meta) {
        items = (data || []).filter((d) => d && d.word && d.meaning);
        applyTheme(themeName);
        resetGame();
        if (items.length === 0) {
            board.innerHTML = `<div style="grid-column:1/-1;text-align:center;color:var(--muted);padding:40px;">词库为空或不足，请先在「内容编辑」中添加单词</div>`;
            return;
        }
        // 按顶部"列"设置应用盘面列数
        if (meta && meta.cols) {
            board.style.gridTemplateColumns = `repeat(${meta.cols}, minmax(150px, 1fr))`;
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
        score = 0; combo = 0; maxCombo = 0; timerSeconds = 0;
        started = false; finished = false; results = [];
        selected = null;
        scoreEl.textContent = "0";
        totalScoreEl.textContent = totalScore;
        comboEl.textContent = "0";
        timerEl.textContent = "0s";
        progressEl.style.width = "0%";
        stopTimer();
    }

    function loadData() {
        listenInit((data, theme, meta) => boot(data, theme, meta));
        if (isMock()) boot(MOCK_ITEMS, "light");
    }

    // 换一组：请求宿主重新抽词并排除已答对；无宿主（浏览器调试）时本地重新洗牌
    $("btnRestart").addEventListener("click", () => {
        if (window.GameUI.bridge()) { sendToHost({ type: "restart" }); return; }
        if (items.length === 0) return;
        resetGame();
        buildCards();
        renderBoard();
    });
    // 再来一局（=继续）：与"换一组"一致，请求宿主重新抽词并排除已答对
    $("btnAgain").addEventListener("click", () => {
        $("resultOverlay").classList.add("hidden");
        $("btnRestart").click();
    });
    $("btnClose").addEventListener("click", () => $("resultOverlay").classList.add("hidden"));

    loadData();
})();