import dayjs from "dayjs";
import relativeTime from "dayjs/plugin/relativeTime";
import "dayjs/locale/zh-cn";

dayjs.extend(relativeTime);

dayjs.locale("zh-cn");

export default function UpdatedAtJST({
  date,
  relative = true,
}: {
  date: string;
  relative?: boolean;
}) {
  const d = dayjs(date);

  const displayValue = relative ? d.fromNow() : d.format("YYYY-MM-DD HH:mm");
  const fullTimeTitle = d.format("YYYY-MM-DD HH:mm:ss");

  return (
    <time
      dateTime={d.toISOString()}
      className="badge badge-dash ml-auto rounded"
      title={fullTimeTitle}
    >
      {displayValue}
    </time>
  );
}
