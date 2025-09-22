# Word Puzzle Game

A Unity-based **word-puzzle** game that blends the logic of a **Mahjong-style tile board** with the creativity of **word formation**.  
The goal is to clear the board by creating valid dictionary words from the open tiles.  
This document explains the project **in full detail**: architecture, algorithms, editor tools, and gameplay systems.

---

## 🎮 Gameplay Overview
- **Board Layout**  
  Each level defines a set of overlapping tiles (letters) arranged in 2D space.  
  Players click **open** tiles to move them into **Letter Holders** and form words.

- **Word Formation**  
  - A word is valid if:
    - It exists in the **dictionary** (`_Game/WordDictionary.txt`).
    - Its length is at least `minWordLength` (default = 2).
  - Players can undo moves or return letters back to the board.

- **Level Completion**  
  - **Win Condition 1**: All tiles are removed.  
  - **Win Condition 2**: When ≤7 tiles remain *and* no valid words can be formed from open tiles
    (checked with the same DFS algorithm used by **AutoSolver**).

---

## 🏗️ Project Architecture

### 1. Core Managers
| Script | Responsibility |
|-------|-----------------|
| **BoardManager** | Maintains the graph of tiles and controls open/closed states using indegree calculations. |
| **LetterHolderManager** | Manages slots (Letter Holders), reservations, commits, and animated returns of tiles. |
| **AnswerManager** | Handles dictionary lookups, prefix pruning, validation of words, and submission logic. |
| **GameFlowManager** | Manages level progression, high scores, and win logic. |
| **LevelManager** | Loads level JSON files, instantiates tiles, and provides grid layout. |
| **ScoreManager** | Computes word scores (length-based or custom rules). |

### 2. UI System
A custom generic **UIController\<T\>** base class provides:
- **Show / Hide** animations using **DOTween** (`CanvasGroup.DOFade`, scale tweens).
- Instant or animated panel transitions.
- Panels:
  - `MainMenuController`
  - `LevelPopupController`
  - `GamePanelController`
  - `WinUIController`

### 3. Special Features
- **AutoSolver**  
  - Searches all permutations of *currently open* tiles using a DFS with **prefix pruning** to find the **longest valid word**.
  - Used by both the **Booster** and the **end-of-level check**.
- **BoosterController**  
  - A limited-use power-up (max 2 per level) that automatically plays the best word found by AutoSolver.
  - Sequentially moves each required tile into the Letter Holders with tween animations.
  - Disables user input and holder clicks during the process.

---

## 🔑 Algorithms

### Tile Openness (Graph + Indegree)
Each tile has a list of children/parents representing vertical blocking relationships.  
`BoardManager` computes **indegrees**:
- **Mode A/B**: Uses explicit `children` links if provided in the JSON.
- **Geometric Mode**: Falls back to z-position overlap detection if no explicit links exist.
- A tile is *open* if `indegree == 0` and it is not picked or in flight.

### Word Search (DFS with Prefix Pruning)
- Input: letters of open tiles.
- Recursive DFS tries all permutations:
  - If the current prefix is not in the dictionary’s **prefix set**, prune the branch.
  - Track the longest valid word with tie-breaking by score.

### Booster Sequential Placement
- For each character of the chosen word:
  1. Find an **open tile** with the matching letter that was open **at the moment of selection**.
  2. Reserve the next Letter Holder.
  3. Tween the tile to the holder and commit it.
  4. Wait for state updates before moving to the next character.

---

## 📂 Data & Levels
- **Dictionary**: `Resources/_Game/WordDictionary.txt`  
  - Loaded into two HashSets:  
    - `_words` (full words)  
    - `_prefixes` (prefixes for pruning).
- **Levels**: `Resources/Levels/level_X.json`  
  - JSON fields:
    - `title` (string) – shown in level list.
    - `tiles[]` – id, character, position, and child relations.

---

## ⚙️ Editor & Tools
- **LevelPopupController** builds a scrollable list of levels at runtime.
- **LevelListItem** displays:
  - Level number
  - Title
  - High score
  - Lock state
- DOTween is used across the UI for:
  - Panel fades
  - Letter return animations
  - Booster tile movements.

---

## 💡 Key Design Decisions
- **Singleton Managers**:  
  Simplifies access between gameplay systems (Board ↔ Answer ↔ UI).
- **Prefix HashSet**:  
  Reduces DFS search space dramatically when checking possible words.
- **Indegree Graph**:  
  Efficiently determines which tiles are playable at any given moment.
- **Sequential Coroutines**:  
  Ensures smooth booster animations and correct timing of open-state updates.

---

## 🚀 How to Play
1. **Select a Level** from the Level Popup.
2. Click open tiles to move them into the Letter Holders.
3. Press **Submit** to score a valid word.
4. Use the **Booster** for automatic word placement (max 2 uses per level).
5. Clear all tiles or exhaust all possible words to win.

---

## 🧩 Dependencies
- **Unity 2021+**
- **DOTween** for all tween animations.
- **TextMeshPro** for crisp UI text rendering.

---

## 📈 Future Improvements
- Smart hint system (reuse AutoSolver logic).
- Additional scoring mechanics (letter multipliers, combo bonuses).
- Online leaderboards and cloud save.

---

## 🏆 Credits
- **Programming & Architecture**: Mehmet Doğru  
- **Dictionary**: Standard English word list (filtered for game use).  

---

# Use of ChatGPT in Development

This project was developed primarily by **Mehmet Doğru**, with **ChatGPT** used as a **coding assistant** and **technical consultant**.

### How ChatGPT Helped
- **Brainstorming & Architecture**  
  - Discussed possible game mechanics, data structures, and algorithmic approaches (e.g. *DFS with prefix pruning*, *indegree graph* for tile openness).
  - Suggested clean **class separations** (e.g. `BoardManager`, `LetterHolderManager`, `AutoSolver`).

- **Code Reviews & Refactoring**  
  - Helped rewrite large scripts into smaller, more maintainable classes.
  - Pointed out potential bugs and suggested optimizations.

- **Unity / C# Examples**  
  - Provided quick examples of Unity-specific features (DOTween tweens, UI transitions).
  - Offered coroutine patterns for smooth animations (e.g. Booster sequential placement).

- **Documentation & README**  
  - Assisted in writing this detailed README and technical explanations.

### What ChatGPT *Did Not* Do
- No automated code generation without supervision.  
- No direct access to the Unity project or runtime debugging.  
- All final design decisions, code integration, and testing were performed by the developer.

**Summary:**  
ChatGPT acted as a **knowledge partner**—helping to accelerate problem solving and provide guidance—while the actual game logic, implementation, and creative direction were fully managed by the developer.

