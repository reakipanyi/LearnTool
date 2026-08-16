(() => {
    "use strict";

    const { toast, sendToHost, speak, esc, listenInit, isMock, applyTheme, shuffle } = window.GameUI;

    // ---------- 常量 ----------
    const HOLE_COUNT = 12;       // 4 列 × 3 行
    const DISTRACTORS = 4;       // 干扰词数量
    const ROUND_STAY = 2000;     // 目标滞留时长(ms)
    const TOTAL_SECONDS = 60;    // 总限时(s)
    const MAX_MISSES = 5;        // 失误上限，提前结束

    // ---------- 状态 ----------
    let items = [];
    let target = null;           // 当前目标词条
    let options = [];            // [{item, hole}]
    let locked = false;
    let nextRoundTimer = null;
    let roundTimer = null;
    let totalTimer = null;
    let timeLeft = TOTAL_SECONDS;
    let started = false, finished = false;
    let score = 0, combo = 0, maxCombo = 0, correctCount = 0, missCount = 0, leakCount = 0;
    let lastTargetId = null;
    let resultMap = new Map();   // id -> 最终 correct

    // ---------- DOM ----------
    const $ = (id) => document.getElementById(id);
    const holesEl = $("holes"), targetText = $("targetText"),
          scoreEl = $("score"), comboEl = $("combo"), rightEl = $("right"),
          missesEl = $("misses"), timeEl = $("time"), hintEl = $("hint");

    const MOCK_ITEMS = [
        { id: "m1", word: "apple", meaning: "苹果", phonetic: "/ˈæpl/" },
        { id: "m2", word: "banana", meaning: "香蕉", phonetic: "/bəˈnɑːnə/" },
        { id: "m3", word: "cat", meaning: "猫", phonetic: "/kæt/" },
        { id: "m4", word: "dog", meaning: "狗", phonetic: "/dɒɡ/" },
        { id: "m5", word: "fish", meaning: "鱼", phonetic: "/fɪʃ/" },
        { id: "m6", word: "grape", meaning: "葡萄", phonetic: "/ɡreɪp/" },
        { id: "m7", word: "house", meaning: "房子", phonetic: "/haʊs/" },
        { id: "m8", word: "moon", meaning: "月亮", phonetic: "/muːn/" },
    ];

    // ---------- 洞盘面 ----------
    const HOLE_GRADS = [
        ["#6366f1", "#8b5cf6"], ["#06b6d4", "#3b82f6"], ["#0ea5e9", "#6366f1"],
        ["#8b5cf6", "#d946ef"], ["#f59e0b", "#f97316"], ["#ec4899", "#f43f5e"],
        ["#10b981", "#22c55e"], ["#2563eb", "#06b6d4"],
    ];
    function buildHoles() {
        holesEl.innerHTML = "";
        for (let i = 0; i < HOLE_COUNT; i++) {
            const hole = document.createElement("div");
            hole.className = "hole";
            const wh = document.createElement("div");
            wh.className = "whack hidden";
            wh.innerHTML = `<span class="w-word"></span><span class="w-phon"></span>`;
            const g = HOLE_GRADS[Math.floor(Math.random() * HOLE_GRADS.length)];
            wh.style.setProperty("--grad-a", g[0]);
            wh.style.setProperty("--grad-b", g[1]);
            wh.addEventListener("click", () => onHoleClick(wh, i));
            hole.appendChild(wh);
            holesEl.appendChild(hole);
        }
    }

    function hideAllWhacks() {
        holesEl.querySelectorAll(".whack").forEach((w) => {
            w.classList.add("hidden");
            w.classList.remove("hit", "miss", "escaped");
        });
    }

    // ---------- 出题 ----------
    function pickTarget() {
        // 错误加权：优先从未答对(unplayed 或已答错)的词条中选
        const priority = items.filter((it) => !resultMap.has(it.id) || resultMap.get(it.id) === false);
        let pool = priority.length ? priority : items;
        if (pool.length > 1) {
            pool = pool.filter((it) => it.id !== lastTargetId);
            if (pool.length === 0) pool = priority.length ? priority : items;
        }
        const t = pool[Math.floor(Math.random() * pool.length)];
        lastTargetId = t.id;
        return t;
    }

    function startRound() {
        if (finished) return;
        hideAllWhacks();
        target = pickTarget();
        targetText.textContent = target.meaning;

        // 选项：正确词 + 干扰词
        const distractors = shuffle(items.filter((it) => it.id !== target.id)).slice(0, DISTRACTORS);
        const opts = distractors.map((it) => ({ item: it, correct: false }))
            .concat([{ item: target, correct: true }]);
        shuffle(opts);

        // 放入随机洞
        const idxs = shuffle(Array.from({ length: HOLE_COUNT }, (_, i) => i)).slice(0, opts.length);
        options = [];
        const allWhacks = holesEl.querySelectorAll(".whack");
        opts.forEach((o, i) => {
            const idx = idxs[i];
            const wh = allWhacks[idx];
            wh.querySelector(".w-word").textContent = o.item.word;
            wh.querySelector(".w-phon").textContent = o.item.phonetic || "";
            wh.classList.remove("hidden");
            options.push({ item: o.item, correct: o.correct, wh, idx });
        });

        locked = false;
        roundTimer = setTimeout(() => onRoundTimeout(), ROUND_STAY);
    }

    // ---------- 点击 ----------
    function onHoleClick(wh, idx) {
        if (finished || locked) return;
        const opt = options.find((o) => o.idx === idx && o.wh === wh);
        if (!opt) return;
        locked = true;
        if (roundTimer) clearTimeout(roundTimer);

        if (opt.correct) {
            // 命中
            combo++;
            maxCombo = Math.max(maxCombo, combo);
            const gain = 10 + combo * 5;
            score += gain;
            scoreEl.textContent = score;
            comboEl.textContent = combo;
            correctCount++;
            rightEl.textContent = correctCount;
            resultMap.set(target.id, true);
            wh.classList.add("hit");
            speak(target.word);
            showFloatOn(wh, `+${gain}`);
            toast(`✅ 打中 +${gain}  连击 x${combo}`);
            nextRoundTimer = setTimeout(startRound, 550);
        } else {
            // 点错
            combo = 0;
            comboEl.textContent = combo;
            missCount++;
            missesEl.textContent = missCount;
            resultMap.set(target.id, false);
            resultMap.set(opt.item.id, false);
            wh.classList.add("miss");
            toast("❌ 这张不配对");
            checkFail();
            nextRoundTimer = setTimeout(startRound, 700);
        }
    }

    // 超时漏点
    function onRoundTimeout() {
        if (finished || locked) return;
        locked = true;
        combo = 0;
        comboEl.textContent = combo;
        missCount++;
        leakCount++;
        missesEl.textContent = missCount;
        resultMap.set(target.id, false);
        hideAllWhacks();
        AllEscaped();
        toast("⏳ 目标漏走了");
        checkFail();
        nextRoundTimer = setTimeout(startRound, 500);
    }

    function AllEscaped() {
        holesEl.querySelectorAll(".whack:not(.hidden)").forEach((w) => w.classList.add("escaped"));
    }

    function showFloatOn(wh, text) {
        const f = document.createElement("div");
        f.className = "float-score";
        f.textContent = text;
        wh.appendChild(f);
        setTimeout(() => f.remove(), 800);
    }

    function checkFail() {
        if (missCount >= MAX_MISSES) {
            toast("💔 失误过多，本局结束");
            setTimeout(finishGame, 600);
        }
    }

    // ---------- 计时 ----------
    function startTotalTimer() {
        if (totalTimer) return;
        totalTimer = setInterval(() => {
            timeLeft--;
            timeEl.textContent = Math.max(0, timeLeft);
            if (timeLeft <= 0) finishGame();
        }, 1000);
    }
    function stopTotalTimer() { if (totalTimer) clearInterval(totalTimer); totalTimer = null; }

    // ---------- 结算 ----------
    function finishGame() {
        if (finished) return;
        finished = true;
        locked = true;
        stopTotalTimer();
        if (roundTimer) clearTimeout(roundTimer);
        if (nextRoundTimer) clearTimeout(nextRoundTimer);
        hideAllWhacks();

        const totalAsked = correctCount + missCount;
        const accuracy = totalAsked ? Math.round(correctCount / totalAsked * 100) : 0;
        let stars = 3;
        if (missCount > 3 || accuracy < 70) stars = 2;
        if (missCount >= MAX_MISSES || accuracy < 40) stars = 1;
        const emoji = stars >= 3 ? "🏆" : stars === 2 ? "🎉" : "😊";

        $("resultEmoji").textContent = emoji;
        $("resultTitle").textContent = stars === 1 ? "继续加油！" : "时间到！";
        $("resultStars").innerHTML =
            `<span class="${stars >= 1 ? "lit" : "dim"}">★</span>` +
            `<span class="${stars >= 2 ? "lit" : "dim"}">★</span>` +
            `<span class="${stars >= 3 ? "lit" : "dim"}">★</span>`;
        $("resultScore").textContent = score;
        $("resultRight").textContent = correctCount;
        $("resultMisses").textContent = missCount;
        $("resultCombo").textContent = maxCombo;
        $("resultOverlay").classList.remove("hidden");

        const results = Array.from(resultMap.keys())
            .map((id) => ({ id, correct: resultMap.get(id) }));
        sendToHost({ type: "gameEnd", results, score, right: correctCount, misses: missCount, combo: maxCombo, seconds: TOTAL_SECONDS - timeLeft });
    }

    // ---------- 初始化 ----------
    function boot(data, themeName) {
        items = (data || []).filter((d) => d && d.word && d.meaning);
        applyTheme(themeName);
        resetGame();
        if (items.length < 3) {
            hintEl.textContent = "词库内容不足，请先在「内容编辑」中添加单词";
            return;
        }
        startTotalTimer();
        startRound();
    }

    function resetGame() {
        target = null; options = []; locked = false;
        timeLeft = TOTAL_SECONDS; started = false; finished = false;
        score = 0; combo = 0; maxCombo = 0; correctCount = 0; missCount = 0; leakCount = 0;
        lastTargetId = null; resultMap = new Map();
        scoreEl.textContent = "0";
        comboEl.textContent = "0";
        rightEl.textContent = "0";
        missesEl.textContent = "0";
        timeEl.textContent = String(TOTAL_SECONDS);
        stopTotalTimer();
        if (roundTimer) clearTimeout(roundTimer);
        if (nextRoundTimer) clearTimeout(nextRoundTimer);
        hideAllWhacks();
    }

    function loadData() {
        listenInit((data, theme) => boot(data, theme));
        if (isMock()) boot(MOCK_ITEMS, "light");
    }

    // ---------- 按钮 ----------
    $("btnSpeak").addEventListener("click", () => {
        if (target) speak(target.word, "en-US");
    });
    $("btnRestart").addEventListener("click", () => {
        if (window.GameUI.bridge()) sendToHost({ type: "restart" });
        else { resetGame(); startTotalTimer(); startRound(); }
    });
    $("btnAgain").addEventListener("click", () => {
        $("resultOverlay").classList.add("hidden");
        resetGame(); startTotalTimer(); startRound();
    });
    $("btnClose").addEventListener("click", () => $("resultOverlay").classList.add("hidden"));

    buildHoles();
    loadData();
})();