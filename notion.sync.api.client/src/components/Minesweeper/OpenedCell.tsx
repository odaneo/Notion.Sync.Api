"use client";

import React from "react";
import Image from "next/image";
import { Cell, isMineCount, isMine } from "@ebinas/react-use-minesweeper";

const COLOR_MAP: Record<number, string> = {
  1: "text-blue-600",
  2: "text-green-600",
  3: "text-red-600",
  4: "text-purple-600",
  5: "text-yellow-600",
  6: "text-pink-600",
  7: "text-slate-600",
  8: "text-gray-600",
} as const;

const resolveColor = (value: number) => {
  return value in COLOR_MAP ? COLOR_MAP[value] : "text-black";
};

const Bomb = ({ isExploded }: { isExploded: boolean }) => {
  return (
    <div
      className={`h-full w-full flex justify-center items-center ${
        isExploded ? "bg-red-800" : "bg-slate-400"
      }`}
    >
      {isExploded ? (
        <Image
          src="/mine_explode.svg"
          alt="exploded mine"
          width={30}
          height={30}
          className="w-2/3"
        />
      ) : (
        <Image
          src="/mine.svg"
          alt="mine"
          width={30}
          height={30}
          className="w-3/5"
        />
      )}
    </div>
  );
};

const Count = ({ value }: { value: number }) => {
  return (
    <div className="h-full w-full flex justify-center items-center text-lg">
      <span className={resolveColor(value)}>{value !== 0 ? value : ""}</span>
    </div>
  );
};

const OpenedCell: React.FC<{ cell: Cell }> = ({ cell }) => {
  return (
    <div className={"h-full w-full bg-slate-50"}>
      {isMine(cell) ? (
        <Bomb isExploded={cell.content.exploded} />
      ) : (
        isMineCount(cell) && <Count value={cell.content.value} />
      )}
    </div>
  );
};

export default OpenedCell;
