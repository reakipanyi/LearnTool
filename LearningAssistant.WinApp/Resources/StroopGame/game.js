(() => {
    "use strict";

    const { $, toast, sendToHost, listenInit, isMock, applyTheme, shuffle, createTimer, formatTime, renderStars, setProgress } = window.GameUI;

    // ---------- 颜色常量 ----------
    const COLOR_NAMES = ["红", "橙", "黄", "绿", "蓝", "紫"];
    const COLOR_MAP = {
        "红": "#ef4444",
        "橙": "#f97316",
        "黄": "#eab308",
        "绿": "#22c55e",
        "蓝": "#3b82f6",
        "紫": "#a855f7"
    };

    // ---------- 图形常量 ----------
    const SHAPE_NAMES = ["圆形", "方形", "三角形", "星形", "心形", "菱形"];
    // 每个形状的 SVG path（填充样式）
    const SHAPE_SVG = {
        "圆形": `<circle cx="50" cy="50" r="40" />`,
        "方形": `<rect x="12" y="12" width="76" height="76" rx="6" />`,
        "三角形": `<polygon points="50,8 92,88 8,88" />`,
        "星形": `<polygon points="50,5 63,35 95,38 70,60 76,92 50,75 24,92 30,60 5,38 37,35" />`,
        "心形": `<path d="M50,88 C20,60 5,40 15,22 C25,4 50,15 50,34 C50,15 75,4 85,22 C95,40 80,60 50,88Z" />`,
        "菱形": `<polygon points="50,5 90,50 50,95 10,50" />`
    };
    // 选项用简标
    const SHAPE_ICON = {
        "圆形": "●",
        "方形": "■",
        "三角形": "▲",
        "星形": "★",
        "心形": "♥",
        "菱形": "◆"
    };

    // ---------- 状态 ----------
    let mode = "colorWord";      // colorWord | shape | number
    let difficulty = "普通";
    let totalQuestions = 15;
    let questions = [];
    let currentIndex = 0;
    let score = 0;
    let correctCount = 0;
    let wrongCount = 0;
    let finished = false;
    let answering = false;

    // ---------- DOM ----------
    const stimulusEl = $("stimulus");
    const optionsEl = $("options");
    const modeHint = $("modeHint");
    const hintEl = $("hint");
    const scoreEl = $("score");
    const correctEl = $("correct");
    const remainEl = $("remain");
    const timerEl = $("timer");
    const progressEl = $("progress");

    // ---------- 计时器 ----------
    const timer = createTimer({ element: timerEl, interval: 200 });

    // ---------- Mock ----------
    const MOCK_CONFIG = {
        mode: "colorWord",
        difficulty: "普通",
        totalQuestions: 15,
        challengeTimeSec: 0,
        theme: "light"
    };

    // ========== 题目生成 ==========

    // 颜色-词语 Stroop
    function generateColorWordQuestions(count) {
        const qs = [];
        for (let i = 0; i < count; i++) {
            // 随机选两个不同的颜色：文字颜色 vs 字体颜色
            const shuffled = shuffle([...COLOR_NAMES]);
            const wordColor = shuffled[0];      // 文字含义
            const fontColor = shuffled[1];       // 字体颜色（正确答案）
            // 备选答案：从所有颜色中取 4 个（含正确答案）
            const options = buildOptions(fontColor, COLOR_NAMES, 4);
            qs.push({
                wordColor,
                fontColor,
                stimulus: wordColor,
                stimulusStyle: `color: ${COLOR_MAP[fontColor]};`,
                correctAnswer: fontColor,
                options
            });
        }
        return qs;
    }

    // 图形 Stroop
    function generateShapeQuestions(count) {
        const qs = [];
        for (let i = 0; i < count; i++) {
            const shuffled = shuffle([...SHAPE_NAMES]);
            const label = shuffled[0];           // 标签文字
            const shape = shuffled[1];            // 实际图形（正确答案）
            const options = buildOptions(shape, SHAPE_NAMES, 4);
            qs.push({
                label,
                shape,
                stimulus: SHAPE_SVG[shape],
                stimulusLabel: label,
                stimulusStyle: `font-size: 16px; color: #64748b;`,
                correctAnswer: shape,
                options
            });
        }
        return qs;
    }

    // 数字 Stroop
    function generateNumberQuestions(count) {
        const qs = [];
        for (let i = 0; i < count; i++) {
            // digit: 显示的数字, count: 显示个数（不同）
            let digit, cnt;
            do {
                digit = Math.floor(Math.random() * 8) + 2;   // 2-9
                cnt = Math.floor(Math.random() * 7) + 2;     // 2-8
            } while (digit === cnt);
            const stimulus = String(digit).repeat(cnt);
            const correctAnswer = cnt;
            // 选项：数字范围 2-9
            const allNums = [2, 3, 4, 5, 6, 7, 8, 9];
            const options = buildOptions(correctAnswer, allNums, 4);
            qs.push({
                digit,
                count: cnt,
                stimulus,
                stimulusStyle: `font-size: ${Math.min(72, 260 / stimulus.length)}px; letter-spacing: 6px;`,
                correctAnswer: cnt,
                options
            });
        }
        return qs;
    }

    /** 从候选中构建选项（含正确答案），打乱顺序 */
    function buildOptions(correct, candidates, total) {
        const others = candidates.filter(c => c !== correct);
        const shuffled = shuffle(others).slice(0, total - 1);
        const opts = [correct, ...shuffled];
        return shuffle(opts);
    }

    // ========== 语音识别 ==========
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    let recognition = null;
    let listening = false;

    function initSpeechRecognition() {
        if (!SpeechRecognition) return;
        recognition = new SpeechRecognition();
        recognition.lang = 'zh-CN';
        recognition.continuous = true;    // 持续监听，一次启动贯穿全程
        recognition.interimResults = false;
        recognition.maxAlternatives = 3;

        recognition.onresult = (event) => {
            if (!answering || finished) return;
            for (let i = event.resultIndex; i < event.results.length; i++) {
                const result = event.results[i];
                if (!result.isFinal) continue;
                const spoken = result[0].transcript.trim();
                if (!spoken) continue;
                const q = questions[currentIndex];
                if (!q) continue;
                const matched = matchOption(spoken, q.options);
                if (matched) {
                    // 语音识别到答案，直接触发答题（不用停止识别，持续模式继续监听下一题）
                    onAnswer(matched);
                    break;
                }
            }
        };

        recognition.onend = () => {
            listening = false;
            document.body.classList.remove('listening');
            // 持续模式下若意外结束（如被其他语音打断），自动重启
            if (!finished) {
                retryStartListening();
            }
        };

        recognition.onerror = (event) => {
            if (event.error === 'aborted') return;
            // 首次启动可能因缺少用户手势而失败，自动重试直到成功
            if (!finished) {
                setTimeout(retryStartListening, 500);
            }
        };
    }

    function retryStartListening() {
        if (listening || finished || !recognition) return;
        try {
            recognition.start();
            listening = true;
            document.body.classList.add('listening');
        } catch (e) {
            // 启动失败，稍后重试（等待用户手势）
            setTimeout(retryStartListening, 300);
        }
    }

    function startListening() {
        retryStartListening();
    }

    function stopListening() {
        if (!recognition) return;
        try {
            recognition.stop();
        } catch (e) { /* ignore */ }
        listening = false;
        document.body.classList.remove('listening');
    }

    /** 将语音识别的文本匹配到选项值 */
    function matchOption(spoken, options) {
        if (!options || options.length === 0) return null;
        if (typeof options[0] === 'number') {
            // 数字模式：提取数字
            const digits = spoken.replace(/[^\d]/g, '');
            for (const opt of options) {
                if (String(opt) === digits) return opt;
            }
            // 汉字数字映射
            const cnMap = { '一': 1, '二': 2, '两': 2, '三': 3, '四': 4, '五': 5, '六': 6, '七': 7, '八': 8, '九': 9 };
            for (const [cn, num] of Object.entries(cnMap)) {
                if (spoken.includes(cn)) return num;
            }
        } else {
            // 文字模式：逐一匹配选项文本
            for (const opt of options) {
                if (spoken.includes(opt)) return opt;
            }
            // 对单字颜色，尝试匹配"红色"→"红"等全称
            for (const opt of options) {
                if (opt.length === 1 && spoken.includes(opt + '色')) return opt;
            }
        }
        return null;
    }

    // ========== 渲染 ==========

    function renderQuestion() {
        if (currentIndex >= questions.length) {
            finishGame();
            return;
        }
        answering = false;
        const q = questions[currentIndex];
        const total = questions.length;
        const remain = total - currentIndex;

        remainEl.textContent = remain;
        setProgress(progressEl, currentIndex, total);

        // 渲染刺激
        if (mode === "colorWord") {
            stimulusEl.textContent = q.stimulus;
            stimulusEl.style.cssText = q.stimulusStyle + ` font-size: 80px; font-weight: 900;`;
            modeHint.textContent = "请说出字体颜色，忽略文字含义";
            hintEl.textContent = "说出与字体颜色匹配的颜色，也可点击按钮选择";
        } else if (mode === "shape") {
            stimulusEl.innerHTML = `<svg viewBox="0 0 100 100" width="120" height="120">${q.stimulus}</svg>`;
            stimulusEl.style.cssText = "";
            // 标签叠加在图形下方
            const labelSpan = document.createElement("div");
            labelSpan.className = "shape-label";
            labelSpan.textContent = q.stimulusLabel;
            // 清除旧标签
            const oldLabel = stimulusEl.querySelector(".shape-label");
            if (oldLabel) oldLabel.remove();
            stimulusEl.appendChild(labelSpan);
            modeHint.textContent = "请说出图形是什么，忽略下方文字标签";
            hintEl.textContent = "说出与图形匹配的名称，也可点击按钮选择";
        } else if (mode === "number") {
            stimulusEl.textContent = q.stimulus;
            stimulusEl.style.cssText = q.stimulusStyle + ` font-weight: 900; color: #1e293b;`;
            modeHint.textContent = `请说出数字${q.digit}共出现了几次，忽略数字本身`;
            hintEl.textContent = "说出出现的次数，也可点击按钮选择";
        }

        // 渲染选项
        optionsEl.innerHTML = "";
        q.options.forEach(opt => {
            const btn = document.createElement("button");
            btn.className = "option-btn";

            if (mode === "colorWord") {
                btn.textContent = opt;
                btn.style.fontSize = "20px";
                btn.style.fontWeight = "700";
            } else if (mode === "shape") {
                btn.innerHTML = `${SHAPE_ICON[opt] || "?"} ${opt}`;
                btn.style.fontSize = "22px";
            } else if (mode === "number") {
                btn.textContent = opt;
                btn.style.fontSize = "24px";
                btn.style.fontWeight = "700";
            }

            btn.dataset.value = opt;
            btn.addEventListener("click", () => onAnswer(opt));
            optionsEl.appendChild(btn);
        });

        answering = true;
        // 显示新题目后自动启动语音监听
        startListening();
    }

    // ========== 交互 ==========

    function onAnswer(selected) {
        if (!answering || finished) return;
        if (!timer.isRunning()) { timer.start(); }

        const q = questions[currentIndex];
        const correct = selected === q.correctAnswer;

        // 反馈
        const buttons = optionsEl.querySelectorAll(".option-btn");
        buttons.forEach(btn => {
            btn.disabled = true;
            if (btn.dataset.value === q.correctAnswer) btn.classList.add("correct");
            else if (btn.dataset.value === selected && !correct) btn.classList.add("wrong");
        });

        if (correct) {
            score += 10;
            correctCount++;
            scoreEl.textContent = score;
            correctEl.textContent = correctCount;
            toast("✅ 正确！");
            GameUI.playSound("match");
        } else {
            wrongCount++;
            toast(`❌ 正确答案：${q.correctAnswer}`);
            GameUI.playSound("error");
        }

        // 延迟进入下一题
        setTimeout(() => {
            currentIndex++;
            renderQuestion();
        }, correct ? 600 : 1200);
    }

    // ========== 结算 ==========

    function finishGame() {
        if (finished) return;
        finished = true;
        timer.stop();
        stopListening();

        const total = questions.length;
        const rate = total ? Math.round(correctCount / total * 100) : 0;
        let stars = 3;
        if (rate < 80) stars = 2;
        if (rate < 60) stars = 1;
        if (rate < 40) stars = 0;

        const elapsed = timer.getElapsed();
        const emoji = stars >= 3 ? "🏆" : stars >= 2 ? "🎉" : stars >= 1 ? "😊" : "💪";
        const modeNames = { colorWord: "颜色-词语", shape: "图形", number: "数字" };

        $("resultEmoji").textContent = emoji;
        $("resultTitle").textContent = stars >= 1 ? "完成！" : "继续加油！";
        $("resultStars").innerHTML = renderStars(stars);
        $("resultScore").textContent = score;
        $("resultRate").textContent = rate + "%";
        $("resultRight").textContent = `${correctCount}/${total}`;
        $("resultTime").textContent = formatTime(elapsed);
        $("resultMode").textContent = modeNames[mode] || mode;
        $("resultOverlay").classList.remove("hidden");

        sendToHost({
            type: "gameEnd",
            mode: "stroop",
            subMode: mode,
            difficulty,
            timeMs: Math.round(elapsed * 1000),
            score,
            correct: correctCount,
            errors: wrongCount,
            total: questions.length,
            star: stars
        });
    }

    // ========== 初始化 ==========

    function boot(data, themeName, meta) {
        mode = data.mode || "colorWord";
        difficulty = data.difficulty || "普通";
        totalQuestions = data.totalQuestions || 15;
        applyTheme(themeName);

        // 初始化语音识别
        initSpeechRecognition();

        // 生成题目
        if (mode === "colorWord") {
            questions = generateColorWordQuestions(totalQuestions);
        } else if (mode === "shape") {
            questions = generateShapeQuestions(totalQuestions);
        } else if (mode === "number") {
            questions = generateNumberQuestions(totalQuestions);
        }

        resetGame();
        renderQuestion();
    }

    function resetGame() {
        currentIndex = 0;
        score = 0;
        correctCount = 0;
        wrongCount = 0;
        finished = false;
        answering = false;

        scoreEl.textContent = "0";
        correctEl.textContent = "0";
        remainEl.textContent = questions.length;
        timerEl.textContent = "0s";
        setProgress(progressEl, 0, 1);
        timer.reset();
    }

    function loadData() {
        listenInit((data, theme, meta) => boot(data, theme, meta));
        if (isMock()) boot(MOCK_CONFIG, "light");
    }

    // 按钮
    $("btnRestart").addEventListener("click", () => {
        if (window.GameUI.bridge()) { sendToHost({ type: "restart" }); return; }
        boot({
            mode,
            difficulty,
            totalQuestions,
            theme: document.body.getAttribute("data-theme") || "light"
        }, document.body.getAttribute("data-theme") || "light");
    });

    $("btnAgain").addEventListener("click", () => {
        $("resultOverlay").classList.add("hidden");
        $("btnRestart").click();
    });

    $("btnClose").addEventListener("click", () => $("resultOverlay").classList.add("hidden"));

    loadData();
})();