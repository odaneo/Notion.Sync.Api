"use client";

import { useEffect } from "react";

export default function GlobalError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <html>
      <body>
        <div className="flex flex-col items-center justify-center mt-20">
          <h1 className="text-4xl font-bold text-error">
            Something went wrong!
          </h1>
          <button className="btn btn-primary mt-4" onClick={() => reset()}>
            Try again
          </button>
        </div>
      </body>
    </html>
  );
}
