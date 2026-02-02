"use client";

import Image from "next/image";
import { BoardConfig } from "@ebinas/react-use-minesweeper";
import { useState } from "react";

type Props = {
  normalFlags: number;
  suspectedFlags: number;
  boardConfig: BoardConfig;
};

export const GameInfoHeader: React.FC<Props> = ({
  normalFlags,
  suspectedFlags,
  boardConfig,
}) => {
  const [showNormalFlag, setShowNormalFlag] = useState(true);
  return (
    <header className="flex justify-between items-center">
      <h1>Mine Sweeper</h1>
      <div
        className="flex gap-2"
        onClick={() => setShowNormalFlag(!showNormalFlag)}
      >
        {showNormalFlag ? (
          <div className="flex items-center">
            <Image src="/flag.png" alt="flag" width={15} height={15} />
            <span className="text-xs">×{normalFlags}</span>
          </div>
        ) : (
          <div className="flex items-center">
            <span className="text-gray-600 dark:invert">?</span>
            <span className="text-xs">×{suspectedFlags}</span>
          </div>
        )}

        <div className="flex items-center">
          <Image src="/mine.svg" alt="exploded mine" width={15} height={15} />
          <span className="text-xs">×{boardConfig.mines}</span>
        </div>
      </div>
    </header>
  );
};
