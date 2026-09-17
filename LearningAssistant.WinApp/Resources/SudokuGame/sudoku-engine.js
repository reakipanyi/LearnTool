/**
 * 数独引擎：生成、求解、校验。
 * 纯函数，无外部依赖。
 */
window.SudokuEngine = (() => {
    "use strict";

    // ---------- 求解器（回溯） ----------
    function solve(grid) {
        // grid: 9×9 二维数组，0 表示空
        const g = grid.map(row => [...row]);
        return _solve(g) ? g : null;
    }

    function _solve(g) {
        for (let r = 0; r < 9; r++) {
            for (let c = 0; c < 9; c++) {
                if (g[r][c] !== 0) continue;
                for (let n = 1; n <= 9; n++) {
                    if (isValid(g, r, c, n)) {
                        g[r][c] = n;
                        if (_solve(g)) return true;
                        g[r][c] = 0;
                    }
                }
                return false;
            }
        }
        return true;
    }

    function isValid(grid, row, col, num) {
        // 行
        for (let c = 0; c < 9; c++) {
            if (grid[row][c] === num) return false;
        }
        // 列
        for (let r = 0; r < 9; r++) {
            if (grid[r][col] === num) return false;
        }
        // 3×3 宫
        const br = Math.floor(row / 3) * 3;
        const bc = Math.floor(col / 3) * 3;
        for (let r = br; r < br + 3; r++) {
            for (let c = bc; c < bc + 3; c++) {
                if (grid[r][c] === num) return false;
            }
        }
        return true;
    }

    // ---------- 题目生成 ----------
    function generate(clueCount) {
        // 1. 先求解一个完整解
        const full = generateFullSolution();
        // 2. 根据线索数移除数字
        const puzzle = full.map(row => [...row]);
        const positions = [];
        for (let r = 0; r < 9; r++) {
            for (let c = 0; c < 9; c++) {
                positions.push([r, c]);
            }
        }
        // 随机打乱移除顺序
        shuffleArray(positions);
        const toRemove = 81 - Math.min(clueCount, 81);
        for (let i = 0; i < toRemove && i < positions.length; i++) {
            const [r, c] = positions[i];
            puzzle[r][c] = 0;
        }
        return { puzzle, solution: full };
    }

    function generateFullSolution() {
        const grid = Array.from({ length: 9 }, () => Array(9).fill(0));
        // 用随机顺序填充
        _fillRandom(grid);
        return grid;
    }

    function _fillRandom(g) {
        const nums = [1, 2, 3, 4, 5, 6, 7, 8, 9];
        for (let r = 0; r < 9; r++) {
            for (let c = 0; c < 9; c++) {
                if (g[r][c] !== 0) continue;
                shuffleArray(nums);
                for (const n of nums) {
                    if (isValid(g, r, c, n)) {
                        g[r][c] = n;
                        if (_fillRandom(g)) return true;
                        g[r][c] = 0;
                    }
                }
                return false;
            }
        }
        return true;
    }

    function shuffleArray(arr) {
        for (let i = arr.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [arr[i], arr[j]] = [arr[j], arr[i]];
        }
        return arr;
    }

    // ---------- 校验 ----------
    function checkComplete(grid) {
        for (let r = 0; r < 9; r++) {
            for (let c = 0; c < 9; c++) {
                if (grid[r][c] === 0) return false;
                if (!isValid(grid, r, c, grid[r][c])) return false;
            }
        }
        return true;
    }

    function getErrors(grid, solution) {
        const errs = [];
        for (let r = 0; r < 9; r++) {
            for (let c = 0; c < 9; c++) {
                if (grid[r][c] !== 0 && grid[r][c] !== solution[r][c]) {
                    errs.push([r, c]);
                }
            }
        }
        return errs;
    }

    function getHint(grid, solution) {
        // 找到第一个玩家填错或未填的格子
        for (let r = 0; r < 9; r++) {
            for (let c = 0; c < 9; c++) {
                if (grid[r][c] !== solution[r][c]) {
                    return { row: r, col: c, value: solution[r][c] };
                }
            }
        }
        return null;
    }

    // ---------- 候选数（笔记用） ----------
    function getCandidates(grid, row, col) {
        if (grid[row][col] !== 0) return [];
        const cands = [];
        for (let n = 1; n <= 9; n++) {
            if (isValid(grid, row, col, n)) cands.push(n);
        }
        return cands;
    }

    return {
        solve,
        isValid,
        generate,
        checkComplete,
        getErrors,
        getHint,
        getCandidates
    };
})();