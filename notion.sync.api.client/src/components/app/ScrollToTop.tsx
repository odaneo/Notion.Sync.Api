"use client";
import { ArrowUp } from "lucide-react";
import { useEffect, useState } from "react";

export default function ScrollToTop() {
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    const isMobile = window.matchMedia("(max-width: 639.98px)").matches;

    const toggleVisible = () => setVisible(!isMobile && window.scrollY > 200);
    window.addEventListener("scroll", toggleVisible);
    return () => window.removeEventListener("scroll", toggleVisible);
  }, []);

  const scrollToTop = () => {
    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  return (
    <button
      onClick={scrollToTop}
      className={`btn btn-circle btn-primary fixed bottom-6 right-6 transition-opacity duration-300 ${
        visible ? "opacity-100" : "opacity-0 pointer-events-none"
      }`}
    >
      <ArrowUp className="h-5 w-5" />
    </button>
  );
}
