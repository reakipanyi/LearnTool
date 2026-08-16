(() => {
    "use strict";

    // ---------- 状态 ----------
    let items = [];        // 原始数据 [{id, word, meaning, phonetic, example}]
    let cards = [];        // 生成的卡片 [{key, type, item}]
    let selected = null;   // 当前选中的单词卡索引
    let score = 0;
    let combo = 0;
    let maxCombo = 0;
    let remaining = 0;
    let timerSeconds = 0;
    let timerId = null;
    let started = false;
    let finished = false;
    let results = [];      // 每对结果 {id, correct}
    let theme = "light";

    // ---------- DOM ----------
    const $ = (id) => document.getElementById(id);
    const board = $("board");
    const hint = $("hint");
    const toast = $("toast");
    const scoreEl = $("score");
    const comboEl = $("combo");
    const remainingEl = $("remaining");
    const timerEl = $("timer");
    const progressEl = $("progress");

    // ---------- 通信 ----------
    function bridge() {
        return window.chrome && window.chrome.webview ? window.chrome.webview : null;
    }
    function sendToHost(msg) {
        const wb = bridge();
        if (wb) wb.postMessage(msg); // 直接发送对象，WebView2 会自动序列化为 JSON
    }
    function toastMsg(text) {
        toast.textContent = text;
        toast.classList.add("show");
        clearTimeout(toast._t);
        toast._t = setTimeout(() => toast.classList.remove("show"), 1200);
    }
    function speakText(text) {
        try {
            if (!window.speechSynthesis) return;
            const u = new SpeechSynthesisUtterance(text);
            u.lang = "en-US";
            u.rate = 0.9;
            window.speechSynthesis.cancel();
            window.speechSynthesis.speak(u);
        } catch (e) { /* 静默降级 */ }
    }

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

    function isMock() {
        return new URLSearchParams(location.search).get("mock") === "1";
    }

    // 卡片多彩配色：单词走冷色系、释义走暖色系
    const WORD_COLORS = [
        "linear-gradient(135deg, #6366f1, #8b5cf6)",
        "linear-gradient(135deg, #06b6d4, #3b82f6)",
        "linear-gradient(135deg, #2563eb, #06b6d4)",
        "linear-gradient(135deg, #8b5cf6, #d946ef)",
        "linear-gradient(135deg, #3b82f6, #6366f1)",
    ];
    const MEANING_COLORS = [
        "linear-gradient(135deg, #ec4899, #f43f5e)",
        "linear-gradient(135deg, #f97316, #f43f5e)",
        "linear-gradient(135deg, #fb7185, #f97316)",
        "linear-gradient(135deg, #db2777, #9333ea)",
        "linear-gradient(135deg, #f43f5e, #ec4899)",
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
        // Fisher-Yates 洗牌
        for (let i = deck.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [deck[i], deck[j]] = [deck[j], deck[i]];
        }
        cards = deck;
        remaining = deck.length;
        remainingEl.textContent = remaining;
    }

    function renderBoard() {
        board.innerHTML = "";
        cards.forEach((card, idx) => {
            const el = document.createElement("div");
            el.className = "card " + card.type;

            // 随机多彩配色
            const palette = card.type === "word" ? WORD_COLORS : MEANING_COLORS;
            const colorIdx = Math.floor(Math.random() * palette.length);
            el.style.setProperty("--card-grad", palette[colorIdx]);
            // 随机轻微倾斜（-2deg ~ 2deg）
            const tilt = (Math.random() * 4 - 2).toFixed(1);
            el.style.setProperty("--tilt", tilt + "deg");
            // 随机圆角（14px ~ 22px）
            const radius = 14 + Math.floor(Math.random() * 9);
            el.style.setProperty("--card-radius", radius + "px");

            if (card.type === "word") {
                el.innerHTML = `<span class="word-text">${esc(card.item.word)}</span>` +
                    (card.item.phonetic ? `<span class="phonetic">${esc(card.item.phonetic)}</span>` : "") +
                    `<button class="speak" title="朗读">🔊</button>`;
                el.querySelector(".speak").addEventListener("click", (e) => {
                    e.stopPropagation();
                    speakText(card.item.word);
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

        // 已选中一张卡片
        if (selected !== null) {
            const selEl = board.children[selected];
            const selCard = cards[selected];

            if (selected === idx) { // 点同一个取消
                selEl.classList.remove("selected");
                selected = null;
                hint.textContent = "请先点一张卡片，再点它的配对卡片";
                return;
            }

            // 两张同类卡片（都是单词或都是释义）：切换选中
            if (card.type === selCard.type) {
                selEl.classList.remove("selected");
                selected = idx;
                el.classList.add("selected");
                hint.textContent = "请点击该卡片的配对卡片";
                return;
            }

            // 一张单词 + 一张释义：判断配对（按同一词条的唯一 key 匹配，避免词库 Id 为空导致误判）
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
                toastMsg(`✅ 正确 +${gain}  连击 x${combo}`);
            } else {
                combo = 0;
                comboEl.textContent = combo;
                results.push({ id: wordItem.id, correct: false });
                wordEl.classList.add("wrong");
                meaningEl.classList.add("wrong");
                toastMsg("❌ 不匹配，再试试");
                setTimeout(() => {
                    wordEl.classList.remove("wrong");
                    meaningEl.classList.remove("wrong");
                }, 500);
            }
            return;
        }

        // 未选中：选中当前卡片（单词或释义均可）
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
        const total = results.length || 1;
        const correct = results.filter((r) => r.correct).length;
        const rate = Math.round((correct / total) * 100);
        $("resultEmoji").textContent = rate >= 90 ? "🏆" : rate >= 70 ? "🎉" : rate >= 50 ? "😊" : "💪";
        $("resultTitle").textContent = won ? "通关啦！" : "游戏结束";
        $("resultScore").textContent = score;
        $("resultRate").textContent = rate + "%";
        $("resultCombo").textContent = maxCombo;
        $("resultTime").textContent = timerSeconds + "s";
        $("resultOverlay").classList.remove("hidden");
        sendToHost({ type: "gameEnd", results, score, combo: maxCombo, seconds: timerSeconds });
    }

    // ---------- 工具 ----------
    function esc(text) {
        return String(text == null ? "" : text)
            .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;").replace(/'/g, "&#39;");
    }

    // ---------- 初始化 ----------
    function boot(data, themeName) {
        items = (data || []).filter((d) => d && d.word && d.meaning);
        if (items.length === 0) {
            board.innerHTML = `<div style="grid-column:1/-1;text-align:center;color:var(--muted);padding:40px;">词库为空或不足，请先在「内容编辑」中添加单词</div>`;
            return;
        }
        theme = themeName || "light";
        document.body.setAttribute("data-theme", theme);
        resetGame();
        buildCards();
        renderBoard();
    }

    function resetGame() {
        score = 0; combo = 0; maxCombo = 0; timerSeconds = 0;
        started = false; finished = false; results = [];
        selected = null;
        scoreEl.textContent = "0";
        comboEl.textContent = "0";
        timerEl.textContent = "0s";
        progressEl.style.width = "0%";
        stopTimer();
    }

    function loadData() {
        const wb = bridge();
        if (wb) {
            wb.addEventListener("message", (e) => {
                // PostWebMessageAsJson 到达时 e.data 已是对象；偶发字符串时则解析。
                let msg = e.data;
                if (typeof msg === "string") {
                    try { msg = JSON.parse(msg); } catch (err) { return; }
                }
                if (msg && msg.type === "init") boot(msg.data, msg.theme);
            });
        }
        if (isMock()) boot(MOCK_ITEMS, "light");
    }

    // 重新开始/换一组：重新洗牌
    $("btnRestart").addEventListener("click", () => {
        if (items.length === 0) return;
        resetGame();
        buildCards();
        renderBoard();
    });
    // 结算页再来一局
    $("btnAgain").addEventListener("click", () => {
        $("resultOverlay").classList.add("hidden");
        $("btnRestart").click();
    });
    $("btnClose").addEventListener("click", () => $("resultOverlay").classList.add("hidden"));

    loadData();
})();