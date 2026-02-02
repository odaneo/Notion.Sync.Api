"use client";

import { toMarixPosition } from "./matrix";
import Cell from "./Cell";
import { Minesweeper } from "@ebinas/react-use-minesweeper";

type Props = Pick<
  Minesweeper,
  "board" | "open" | "toggleFlag" | "switchFlagType"
>;

const Board: React.FC<Props> = ({
  board,
  open,
  toggleFlag,
  switchFlagType,
}) => {
  return (
    <section
      className={"w-fit h-fit bg-slate-700 grid gap-1 p-2"}
      style={{
        gridTemplateColumns: `repeat(${board.meta.cols}, 1fr)`,
        gridTemplateRows: `repeat(${board.meta.rows}, 1fr)`,
      }}
    >
      {board.data.flat().map((cell) => {
        const [row, col] = toMarixPosition(cell.id, board.meta.cols);
        return (
          <Cell
            key={cell.id}
            cell={cell}
            row={row}
            col={col}
            handleClick={open}
            toggleFlag={toggleFlag}
            switchFlagType={switchFlagType}
          />
        );
      })}
    </section>
  );
};

export default Board;
