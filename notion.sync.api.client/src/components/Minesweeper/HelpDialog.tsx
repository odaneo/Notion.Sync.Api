"use client";

import Image from "next/image";
import { useEffect } from "react";

type Props = {
  isOpen: boolean;
  onClose: () => void;
};

const HelpDialog: React.FC<Props> = ({ isOpen, onClose }) => {
  useEffect(() => {
    if (!isOpen) return;
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <button
        type="button"
        className="absolute inset-0 bg-black/60"
        aria-label="Close help"
        onClick={onClose}
      />
      <div className="relative z-10 w-[90vw] max-w-sm rounded-sm bg-slate-700 p-4 text-slate-100 shadow-lg">
        <div className="flex items-center justify-between">
          <h2 className="text-sm font-semibold">GUIDE</h2>
          <button
            type="button"
            className="text-xs px-2 py-1 bg-slate-500"
            onClick={onClose}
          >
            CLOSE
          </button>
        </div>
        <div className="mt-4 flex flex-col gap-3">
          <div className="help-row">
            <div className="help-cell-stack" aria-hidden="true">
              <div className="help-cell help-cell-closed" />
              <div className="help-ripple" />
              <div className="help-cursor">
                <svg viewBox="0 0 24 24" focusable="false">
                  <path d="M5 3l13 9-6 1.5L9.5 20 8 8.5 5 3z" />
                </svg>
                <span className="help-tag">CLICK</span>
              </div>
            </div>
            <div className="help-arrow" aria-hidden="true">
              <svg viewBox="0 0 24 24" focusable="false">
                <path d="M4 12h14m0 0-4-4m4 4-4 4" />
              </svg>
            </div>
            <div className="help-cell-result" aria-hidden="true">
              <div className="help-cell help-cell-open">
                <span className="help-number-text">1</span>
              </div>
            </div>
            <span className="sr-only">クリック/タップで開く</span>
          </div>
          <div className="help-row">
            <div className="help-cell-stack" aria-hidden="true">
              <div className="help-cell help-cell-closed help-hold-shake" />
              <div className="help-cursor">
                <svg viewBox="0 0 24 24" focusable="false">
                  <path d="M5 3l13 9-6 1.5L9.5 20 8 8.5 5 3z" />
                </svg>
                <span className="help-tag">HOLD</span>
              </div>
            </div>
            <div className="help-arrow help-arrow-bidir" aria-hidden="true">
              <svg viewBox="0 0 24 24" focusable="false">
                <path d="M4 12h16m0 0-3-3m3 3-3 3M4 12l3-3m-3 3 3 3" />
              </svg>
            </div>
            <div className="help-cell-result" aria-hidden="true">
              <div className="help-cell help-cell-closed">
                <Image
                  src="/flag.png"
                  alt=""
                  width={16}
                  height={16}
                  className="help-flag"
                />
              </div>
            </div>
            <span className="sr-only">長押しで旗を付ける</span>
          </div>
          <div className="help-row">
            <div className="help-cell-stack" aria-hidden="true">
              <div className="help-cell help-cell-closed">
                <Image
                  src="/flag.png"
                  alt=""
                  width={16}
                  height={16}
                  className="help-flag"
                />
              </div>
              <div className="help-ripple" />
              <div className="help-cursor">
                <svg viewBox="0 0 24 24" focusable="false">
                  <path d="M5 3l13 9-6 1.5L9.5 20 8 8.5 5 3z" />
                </svg>
                <span className="help-tag">CLICK</span>
              </div>
            </div>
            <div className="help-arrow help-arrow-bidir" aria-hidden="true">
              <svg viewBox="0 0 24 24" focusable="false">
                <path d="M4 12h16m0 0-3-3m3 3-3 3M4 12l3-3m-3 3 3 3" />
              </svg>
            </div>
            <div className="help-cell-result" aria-hidden="true">
              <div className="help-cell help-cell-closed">
                <svg
                  className="help-question"
                  viewBox="0 0 24 24"
                  focusable="false"
                >
                  <path d="M9 9.5C9 7.6 10.6 6 12.5 6c1.7 0 3.1 1.3 3.1 2.9 0 1.5-0.8 2.2-1.7 2.8-0.9 0.6-1.4 1.2-1.4 2.3v0.8" />
                  <circle cx="12.5" cy="18" r="1" />
                </svg>
              </div>
            </div>
            <span className="sr-only">旗があるマスをクリックで?に切り替え</span>
          </div>
        </div>
      </div>
    </div>
  );
};

export default HelpDialog;
