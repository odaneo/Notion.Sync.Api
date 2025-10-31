export default function UpdatedAtJST({ date }: { date: string }) {
  const d = new Date(date);

  const formatted = d.toLocaleString("zh-CN", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    timeZoneName: "short",
  });

  return (
    <time
      dateTime={d.toISOString()}
      className="badge badge-dash ml-auto rounded"
      title={d.toISOString()}
    >
      {"更新于"}：{formatted}
    </time>
  );
}
