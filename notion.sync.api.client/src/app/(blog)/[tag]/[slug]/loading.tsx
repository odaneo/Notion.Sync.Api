export default function Loading() {
  return (
    <section aria-busy="true" aria-live="polite" className="space-y-4">
      <div className="h-10 w-1/2 animate-pulse rounded bg-base-200" />

      <div className="flex flex-wrap gap-2">
        <div className="h-5 w-16 animate-pulse rounded bg-base-200" />
        <div className="h-5 w-20 animate-pulse rounded bg-base-200" />
        <div className="h-5 w-14 animate-pulse rounded bg-base-200" />
      </div>

      <div className="space-y-4">
        <div className="h-6 w-5/6 animate-pulse rounded bg-base-200" />
        <div className="h-6 w-4/6 animate-pulse rounded bg-base-200" />
        <div className="h-6 w-3/6 animate-pulse rounded bg-base-200" />
        <div className="h-6 w-2/3 animate-pulse rounded bg-base-200" />
        <div className="h-6 w-1/2 animate-pulse rounded bg-base-200" />
      </div>
    </section>
  );
}
