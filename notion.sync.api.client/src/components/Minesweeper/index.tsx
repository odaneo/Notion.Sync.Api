"use client";

import { useEffect, useRef, useState } from "react";
import useConfetti from "@/hooks/useConfetti";
import { useMinesweeper } from "@ebinas/react-use-minesweeper";
import { GameInfoHeader } from "./GameInfoHeader";
import Board from "./Board";
import GameToolBar from "./GameToolBar";
import GameContextAction from "./GameContextAction";
import HelpDialog from "./HelpDialog";

const Minesweeper = () => {
  const {
    board,
    gameState,
    gameMode,
    init,
    restart,
    open,
    toggleFlag,
    switchFlagType,
    flags,
    settings,
  } = useMinesweeper();
  const confetti = useConfetti();
  const boardRef = useRef<HTMLDivElement>(null);
  const [isHelpOpen, setIsHelpOpen] = useState(false);
  const [showGuideHint, setShowGuideHint] = useState(false);

  useEffect(() => {
    if (gameState !== "completed") return;

    confetti.showBoth();

    const timerId = setInterval(() => {
      setTimeout(() => {
        confetti.showLeft();
      }, Math.random() * 1500);
      setTimeout(() => {
        confetti.showRight();
      }, Math.random() * 1500);
    }, 3000);

    return () => clearInterval(timerId);
  }, [gameState, confetti]);

  useEffect(() => {
    const boardElement = boardRef.current;
    if (!boardElement) return;
    boardElement.classList.remove("p-2");

    const isScrollable = boardElement.scrollHeight > boardElement.clientHeight;
    if (isScrollable) {
      boardElement.classList.add("p-2");
    }
  }, [gameMode]);

  useEffect(() => {
    const storageKey = "ms-guide-seen";
    try {
      if (localStorage.getItem(storageKey)) return;
      setShowGuideHint(true);
    } catch {
      setShowGuideHint(true);
    }
  }, []);

  const openHelp = () => {
    setIsHelpOpen(true);
    if (showGuideHint) {
      setShowGuideHint(false);
      try {
        localStorage.setItem("ms-guide-seen", "1");
      } catch {
        // Ignore storage failures.
      }
    }
  };

  return (
    <div className="h-full pt-10 flex flex-col items-stretch">
      <div className="py-0.5 flex flex-col justify-end">
        <GameInfoHeader
          normalFlags={flags.normal}
          suspectedFlags={flags.suspected}
          boardConfig={board.meta}
        />
      </div>
      <div
        className={
          "overflow-auto max-w-[90vw] max-h-[55vh] md:max-h-[62vh] xl:max-h-[70vh] bg-black/50 dark:bg-white/50"
        }
        ref={boardRef}
      >
        <Board
          board={board}
          open={open}
          toggleFlag={toggleFlag}
          switchFlagType={switchFlagType}
        />
      </div>
      <div className="py-2 flex justify-between">
        <GameToolBar init={init} gameMode={gameMode} settings={settings} />
        <button
          type="button"
          className={
            showGuideHint
              ? "px-2 py-1 text-[0.65rem] sm:text-xs text-slate-500/90 tracking-wide hover:text-slate-700/90"
              : "px-2 py-1 text-[0.65rem] sm:text-xs text-slate-500/90 bg-transparent opacity-0 hover:opacity-100 focus-visible:opacity-100 transition-opacity"
          }
          onClick={openHelp}
          aria-label="Help"
        >
          <span className="leading-none">GUIDE</span>
        </button>
      </div>

      <div className="min-h-10">
        {(gameState === "completed" || gameState === "failed") && (
          <GameContextAction restart={restart} />
        )}
      </div>
      <HelpDialog isOpen={isHelpOpen} onClose={() => setIsHelpOpen(false)} />
    </div>
  );
};

export default Minesweeper;
