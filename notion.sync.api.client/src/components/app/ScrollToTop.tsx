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
      type="button"
      onClick={scrollToTop}
      className="icon-link back-to-top"
      aria-label="返回顶部"
      hidden={!visible}
    >
      <ArrowUp className="h-5 w-5" />
    </button>
  );
}
