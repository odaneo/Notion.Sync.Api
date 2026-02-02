"use client";

import React, { useRef } from "react";
import OpenedCell from "./OpenedCell";
import UnopenedCell from "./UnopenedCell";
import { type Cell, isOpened } from "@ebinas/react-use-minesweeper";

type Props = {
  cell: Cell;
  row: number;
  col: number;
  handleClick: (index: number) => void;
  toggleFlag: (index: number) => void;
  switchFlagType: (id: number) => void;
};

type Direction = "up" | "down" | "left" | "right";

const getClosestButton = (
  from: HTMLDivElement,
  direction: Direction,
): HTMLButtonElement | null => {
  const nextElement = getNextCell(from, direction);
  if (!nextElement) return null;
  const button = nextElement.querySelector("button");
  if (button) return button;
  return getClosestButton(nextElement, direction);
};

const getNextCell = (
  from: HTMLDivElement,
  direction: Direction,
): HTMLDivElement | null => {
  const row = Number(from.dataset.row);
  const col = Number(from.dataset.col);
  switch (direction) {
    case "up":
      return getCell(row - 1, col);
    case "down":
      return getCell(row + 1, col);
    case "left":
      return getCell(row, col - 1);
    case "right":
      return getCell(row, col + 1);
  }
};

const getCell = (row: number, col: number): HTMLDivElement | null => {
  return document.querySelector(`div[data-row="${row}"][data-col="${col}"]`);
};

const Cell: React.FC<Props> = ({
  cell,
  row,
  col,
  handleClick,
  toggleFlag,
  switchFlagType,
}) => {
  const cellRef = useRef<HTMLDivElement>(null);

  const handleArrowMove = (e: React.KeyboardEvent<HTMLDivElement>) => {
    const currentCell = cellRef.current;
    if (!currentCell) return;

    let closestButton: HTMLButtonElement | null;
    switch (e.key) {
      case "ArrowUp":
        closestButton = getClosestButton(currentCell, "up");
        break;
      case "ArrowDown":
        closestButton = getClosestButton(currentCell, "down");
        break;
      case "ArrowLeft":
        closestButton = getClosestButton(currentCell, "left");
        break;
      case "ArrowRight":
        closestButton = getClosestButton(currentCell, "right");
        break;
      default:
        return;
    }

    closestButton?.focus();
  };

  return (
    <div
      className={
        "flex justify-center items-center aspect-square w-[8vmin] md:w-[6vmin]"
      }
      data-row={row}
      data-col={col}
      ref={cellRef}
      onKeyDown={handleArrowMove}
    >
      {isOpened(cell) ? (
        <OpenedCell cell={cell} />
      ) : (
        <UnopenedCell
          cell={cell}
          handleClick={() => handleClick(cell.id)}
          toggleFlag={() => toggleFlag(cell.id)}
          switchFlagType={() => switchFlagType(cell.id)}
        />
      )}
    </div>
  );
};

export default React.memo(Cell);
