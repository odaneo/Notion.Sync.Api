"use client";

import type { Minesweeper } from "@ebinas/react-use-minesweeper";

type Props = Pick<Minesweeper, "restart">;

export const GameContextAction: React.FC<Props> = ({ restart }) => {
  return (
    <div className="flex flex-col items-center">
      <button
        className="bg-slate-500 shadow-[1px_1px_1px_#444,-1px_-1px_1px_#fff] text-white px-3 py-1 text-sm focus:bg-slate-400 focus:scale-110 origin-center"
        onClick={restart}
      >
        NEW GAME
      </button>
    </div>
  );
};

export default GameContextAction;
