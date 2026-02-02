/* eslint-disable @typescript-eslint/no-explicit-any */
"use client";

import { useRef, useState } from "react";

export const BUTTON_NUMBERS = {
  LEFT_CLICK: 0,
  RIGHT_CLICK: 2,
} as const;

interface Options {
  shouldPreventDefault?: boolean;
  delay?: number;
  targetButton?: number;
}

const useLongPress = (
  onLongPress: (e?: PointerEvent) => void,
  onclick?: (e?: PointerEvent) => void,
  {
    shouldPreventDefault = true,
    delay = 200,
    targetButton = BUTTON_NUMBERS.LEFT_CLICK,
  }: Options = {},
) => {
  const [isLongPress, setIsLongPress] = useState(false);
  const pressTimer = useRef<number | undefined>(undefined);

  const clearTimer = () => {
    window.clearTimeout(pressTimer.current);
    pressTimer.current = undefined;
  };

  const handlePointerDown = (e: PointerEvent) => {
    if (e.button !== targetButton) return;

    setIsLongPress(false);
    pressTimer.current = window.setTimeout(() => {
      setIsLongPress(true);
      onLongPress(e);
    }, delay);
  };

  const handlePointerUp = (e: PointerEvent) => {
    if (e.button !== targetButton) return;

    if (pressTimer.current) {
      clearTimer();
    }
    if (!isLongPress && !!onclick) {
      onclick();
    }
  };

  return {
    onPointerDown: (e: any) => {
      shouldPreventDefault && e.preventDefault();
      handlePointerDown(e);
    },
    onPointerUp: (e: any) => {
      shouldPreventDefault && e.preventDefault();
      handlePointerUp(e);
    },
    onPointerLeave: (e: any) => {
      shouldPreventDefault && e.preventDefault();
      !!pressTimer.current && clearTimer();
    },
  };
};

export default useLongPress;
