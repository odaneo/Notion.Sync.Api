export default function Loading() {
  return (
    <div aria-busy="true" aria-live="polite" className="space-y-4">
      <div className="h-7 w-1/2 animate-pulse rounded bg-slate-200 dark:bg-slate-700" />

      <div className="flex flex-wrap gap-2">
        <div className="h-5 w-16 animate-pulse rounded-full bg-slate-200 dark:bg-slate-700" />
        <div className="h-5 w-20 animate-pulse rounded-full bg-slate-200 dark:bg-slate-700" />
        <div className="h-5 w-14 animate-pulse rounded-full bg-slate-200 dark:bg-slate-700" />
      </div>

      <div className="space-y-2">
        <div className="h-4 w-5/6 animate-pulse rounded bg-slate-200 dark:bg-slate-700" />
        <div className="h-4 w-4/6 animate-pulse rounded bg-slate-200 dark:bg-slate-700" />
        <div className="h-4 w-3/6 animate-pulse rounded bg-slate-200 dark:bg-slate-700" />
        <div className="h-4 w-2/3 animate-pulse rounded bg-slate-200 dark:bg-slate-700" />
        <div className="h-4 w-1/2 animate-pulse rounded bg-slate-200 dark:bg-slate-700" />
      </div>
    </div>
  );
}
