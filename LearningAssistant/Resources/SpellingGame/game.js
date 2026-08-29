(() => {
    "use strict";

    const { toast, sendToHost, speak, esc, listenInit, isMock, applyTheme, shuffle, initSkipKnownRadios, metaTotalRemaining } = window.GameUI;

    // ---------- 状态 ----------
    let allItems = [];    // 原始数据
    let queue = [];       // 待做 [{item, wrongCount}]
    let current = null;   // {item, wrongCount}
    let typed = [];       // 已输入字母
    let judging = false;
    let totalItems = 0, completed = 0;
    let score = 0, totalScore = 0, combo = 0, maxCombo = 0, rightAttempts = 0, wrongAttempts = 0;
    let resultMap = new Map(); // id -> 最终 correct（一旦错过即 false）
    let seconds = 0, timerId = null, started = false, finished = false;
    let getIfRestart = false;

    // ---------- DOM ----------
    const $ = (id) => document.getElementById(id);
    const slotsEl = $("slots"), meaningEl = $("qMeaning"), hintEl = $("qHint"),
          scoreEl = $("score"), comboEl = $("combo"), progressEl = $("progress"),
          timerEl = $("timer"), progressBar = $("progressBar"),
          totalScoreEl = $("totalScore"), totalRemainingEl = $("totalRemaining"),
          continueBtn = $("btnContinue");

    const MOCK_ITEMS = [
        { id: "m1", word: "apple", meaning: "苹果", phonetic: "/ˈæpl/" },
        { id: "m2", word: "banana", meaning: "香蕉", phonetic: "/bəˈnɑːnə/" },
        { id: "m3", word: "cat", meaning: "猫", phonetic: "/kæt/" },
        { id: "m4", word: "dog", meaning: "狗", phonetic: "/dɒɡ/" },
        { id: "m5", word: "fish", meaning: "鱼", phonetic: "/fɪʃ/" },
        { id: "m6", word: "grape", meaning: "葡萄", phonetic: "/ɡreɪp/" },
        { id: "m7", word: "house", meaning: "房子", phonetic: "/haʊs/" },
        { id: "m8", word: "lemon", meaning: "柠檬", phonetic: "/ˈlemən/" },
        { id: "m9", word: "moon", meaning: "月亮", phonetic: "/muːn/" },
        { id: "m10", word: "peach", meaning: "桃子", phonetic: "/piːtʃ/" },
    ];

    // ---------- 出题 ----------
    function startQuestion() {
        if (queue.length === 0) { finishGame(); return; }
        current = queue.shift();
        typed = [];
        judging = false;
        const word = current.item.word;

        meaningEl.textContent = current.item.meaning;
        // 含空格/连词符的词：字母数不计空格与连词符，避免误导
        const letterCount = word.replace(/[\s-]/g, "").length;
        hintEl.textContent = current.item.phonetic ? `音标：${esc(current.item.phonetic)}　字母数：${letterCount}` : `字母数：${letterCount}`;
        renderSlots(word);
        updateProgress();
        // 隐藏继续按钮
        continueBtn.classList.add("hidden");
    }

    // 空格/连词符用窄槽展示，形成分隔并提示此处需输入对应字符
    function renderSlots(word) {
        slotsEl.innerHTML = "";
        for (let i = 0; i < word.length; i++) {
            const s = document.createElement("div");
            const ch = word[i];
            s.className = ch === " " ? "slot space" : ch === "-" ? "slot hyphen" : "slot";
            slotsEl.appendChild(s);
        }
    }

    // ---------- 输入 ----------
    function typeLetter(ch) {
        if (finished || judging || !current) return;
        if (!started) { started = true; startTimer(); }
        if (typed.length >= current.item.word.length) return;
        typed.push(ch);
        renderTyped(false);
        if (typed.length === current.item.word.length) judge();
    }

    function eraseLetter() {
        if (finished || judging || !current) return;
        typed.pop();
        renderTyped(false);
    }

    function clearAll() {
        if (finished || judging || !current) return;
        typed = [];
        renderTyped(false);
    }

    function renderTyped(mark) {
        const slots = slotsEl.children;
        const word = current.item.word;
        for (let i = 0; i < slots.length; i++) {
            if (word[i] === " ") {
                // 空格格位：不显示字符，仅以状态区分是否已输入
                slots[i].textContent = "";
                slots[i].className = "slot space";
                if (mark) slots[i].classList.add(typed[i] === word[i] ? "right" : "wrong");
                else if (i < typed.length) slots[i].classList.add("filled");
                continue;
            }
            if (word[i] === "-") {
                // 连词符格位：窄槽展示，需输入连词符
                slots[i].textContent = i < typed.length ? typed[i] : "";
                slots[i].className = "slot hyphen";
                if (mark) slots[i].classList.add(typed[i] === word[i] ? "right" : "wrong");
                else if (i < typed.length) slots[i].classList.add("filled");
                continue;
            }
            slots[i].textContent = i < typed.length ? typed[i] : "";
            slots[i].className = "slot";
            if (mark) {
                slots[i].classList.add(typed[i] === word[i] ? "right" : "wrong");
            } else if (i < typed.length) {
                slots[i].classList.add("filled");
            }
        }
    }

    // 将正确单词填入格子（用于拼错后展示正确答案）
    function renderCorrectAnswer() {
        const slots = slotsEl.children;
        const word = current.item.word;
        for (let i = 0; i < slots.length; i++) {
            const ch = word[i];
            if (ch === " ") {
                slots[i].textContent = "";
                slots[i].className = "slot space right";
            } else if (ch === "-") {
                slots[i].textContent = "-";
                slots[i].className = "slot hyphen right";
            } else {
                slots[i].textContent = ch;
                slots[i].className = "slot right";
            }
        }
    }

    // ---------- 判定 ----------
    function judge() {
        judging = true;
        const word = current.item.word;
        const input = typed.join("").toLowerCase();
        const correct = input === word.toLowerCase();

        if (correct) {
            combo++;
            maxCombo = Math.max(maxCombo, combo);
            const gain = 10 + combo * 5;
            score += gain;
            scoreEl.textContent = score;
            comboEl.textContent = combo;
            rightAttempts++;
            completed++;
            if (!resultMap.has(current.item.id)) resultMap.set(current.item.id, true);
            renderTyped(true);
            speak(word);
            toast(`✅ 正确 +${gain}  连击 x${combo}`);
            hintEl.textContent = `${esc(word)}　拼写正确！`;
            setTimeout(startQuestion, 900);
        } else {
            wrongAttempts++;
            combo = 0;
            comboEl.textContent = combo;
            score = Math.max(0, score - 5);
            scoreEl.textContent = score;
            resultMap.set(current.item.id, false);
            renderTyped(true);
            // 错误即时闭环：本词排到队尾稍后再现
            const redo = { item: current.item, wrongCount: (current.wrongCount || 0) + 1 };
            queue.push(redo);
            toast("❌ 拼错了，稍后这词会再次出现");
            hintEl.textContent = `正确答案：${word}`;
            // 先显示错误标记 700ms，再填充正确单词，然后显示"继续"按钮等待用户手动点击
            setTimeout(() => {
                renderCorrectAnswer();
                speak(word);
                continueBtn.classList.remove("hidden");
            }, 700);
        }
    }

    function updateProgress() {
        progressEl.textContent = `${completed}/${totalItems}`;
        progressBar.style.width = (totalItems ? (completed / totalItems) * 100 : 0) + "%";
    }

    // ---------- 计时 ----------
    function startTimer() {
        if (timerId) return;
        timerId = setInterval(() => { seconds++; timerEl.textContent = seconds + "s"; }, 1000);
    }
    function stopTimer() { if (timerId) clearInterval(timerId); timerId = null; }

    // ---------- 结算 ----------
    function finishGame() {
        if (finished) return;
        finished = true;
        stopTimer();

        const rate = totalItems ? Math.round((rightAttempts >= 0 ? completed : 0) / totalItems * 100) : 0;
        let stars = 3;
        if (wrongAttempts > 2 || rate < 80) stars = 2;
        if (wrongAttempts > 5 || rate < 50) stars = 1;
        const emoji = stars >= 3 ? "🏆" : stars === 2 ? "🎉" : "😊";

        $("resultEmoji").textContent = emoji;
        $("resultTitle").textContent = stars === 1 ? "继续加油！" : "完成啦！";
        $("resultStars").innerHTML =
            `<span class="${stars >= 1 ? "lit" : "dim"}">★</span>` +
            `<span class="${stars >= 2 ? "lit" : "dim"}">★</span>` +
            `<span class="${stars >= 3 ? "lit" : "dim"}">★</span>`;
        $("resultScore").textContent = score;
        totalScoreEl.textContent = totalScore + score;
        $("resultTotal").textContent = totalScore + score;
        $("resultRight").textContent = `${rightAttempts}/${totalItems}`;
        $("resultRate").textContent = rate + "%";
        $("resultTime").textContent = seconds + "s";
        $("resultOverlay").classList.remove("hidden");

        const results = Array.from(resultMap.keys())
            .map((id) => ({ id, correct: resultMap.get(id) }));
        sendToHost({ type: "gameEnd", results, score, right: rightAttempts, errors: wrongAttempts, combo: maxCombo, seconds });
    }

    // ---------- 初始化 ----------
    function boot(data, themeName, meta) {
        allItems = (data || []).filter((d) => d && d.word && d.meaning);
        applyTheme(themeName);
        resetGame();
        if (allItems.length === 0) {
            meaningEl.textContent = "词库为空";
            hintEl.textContent = "请先在「内容编辑」中添加单词";
            return;
        }
        // 总剩余条目数
        totalRemainingEl.textContent = GameUI.metaTotalRemaining(meta);
        // "跳过已知项/加载所有"单选，切换时通知宿主保存
        const skipRadio = GameUI.initSkipKnownRadios("skipKnownGroup", (skip) => {
            GameUI.sendToHost({ type: "setting", skipKnown: skip });
        });
        if (meta && typeof meta.skipKnown === "boolean") skipRadio.set(meta.skipKnown);
        queue = shuffle(allItems.map((it) => ({ item: it, wrongCount: 0 })));
        startQuestion();
    }

    function resetGame() {
        // 换一组时总分在上一次基础上累计，不清零
        totalScore += score;
        queue = []; current = null; typed = []; judging = false;
        totalItems = allItems.length; completed = 0;
        score = 0; combo = 0; maxCombo = 0; rightAttempts = 0; wrongAttempts = 0;
        resultMap = new Map();
        seconds = 0; started = false; finished = false;
        scoreEl.textContent = "0";
        totalScoreEl.textContent = totalScore;
        comboEl.textContent = "0";
        timerEl.textContent = "0s";
        getIfRestart = false;
        stopTimer();
    }

    function loadData() {
        listenInit((data, theme, meta) => boot(data, theme, meta));
        if (isMock()) boot(MOCK_ITEMS, "light");
    }

    // ---------- 虚拟键盘 ----------
    function buildKeyboard() {
        const kb = $("keyboard");
        kb.innerHTML = "";
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ".split("").forEach((letter) => {
            const k = document.createElement("button");
            k.className = "key";
            k.textContent = letter;
            k.addEventListener("click", () => typeLetter(letter.toLowerCase()));
            kb.appendChild(k);
        });
        const bs = document.createElement("button");
        bs.className = "key ctrl";
        bs.textContent = "⌫";
        bs.addEventListener("click", eraseLetter);
        kb.appendChild(bs);
        // 空格键：支持含空格的短语
        const sp = document.createElement("button");
        sp.className = "key ctrl wide";
        sp.textContent = "空格";
        sp.addEventListener("click", () => typeLetter(" "));
        kb.appendChild(sp);
        // 连词符键：支持含连词符的单词（如 well-known）
        const hy = document.createElement("button");
        hy.className = "key ctrl";
        hy.textContent = "-";
        hy.addEventListener("click", () => typeLetter("-"));
        kb.appendChild(hy);
        const cl = document.createElement("button");
        cl.className = "key ctrl";
        cl.textContent = "清空";
        cl.addEventListener("click", clearAll);
        kb.appendChild(cl);
    }

    // 硬件键盘
    window.addEventListener("keydown", (e) => {
        if (getIfRestart) return;
        if (/^[a-zA-Z]$/.test(e.key)) { e.preventDefault(); typeLetter(e.key.toLowerCase()); }
        else if (e.key === " ") { e.preventDefault(); typeLetter(" "); }
        else if (e.key === "-") { e.preventDefault(); typeLetter("-"); }
        else if (e.key === "Backspace") { e.preventDefault(); eraseLetter(); }
        else if (e.key === "Escape") { e.preventDefault(); clearAll(); }
    });

    // ---------- 按钮 ----------
    $("btnSpeak").addEventListener("click", () => { if (current) speak(current.item.word); });
    $("btnContinue").addEventListener("click", () => {
        if (!judging) return;
        continueBtn.classList.add("hidden");
        startQuestion();
    });
    $("btnRestart").addEventListener("click", () => {
        if (window.GameUI.bridge()) sendToHost({ type: "restart" });
        else { resetGame(); queue = shuffle(allItems.map((it) => ({ item: it, wrongCount: 0 }))); startQuestion(); }
    });
    $("btnAgain").addEventListener("click", () => {
        $("resultOverlay").classList.add("hidden");
        $("btnRestart").click();
    });
    $("btnClose").addEventListener("click", () => $("resultOverlay").classList.add("hidden"));

    buildKeyboard();
    loadData();
})();