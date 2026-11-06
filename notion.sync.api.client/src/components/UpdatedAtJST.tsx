"use client";

import { useSyncExternalStore } from "react";
import dayjs from "dayjs";
import relativeTime from "dayjs/plugin/relativeTime";
import utc from "dayjs/plugin/utc";
import timezone from "dayjs/plugin/timezone";
import "dayjs/locale/zh-cn";

dayjs.extend(relativeTime);
dayjs.extend(utc);
dayjs.extend(timezone);

dayjs.locale("zh-cn");
const subscribe = () => () => {};

export default function UpdatedAtJST({
  date,
  relative = true,
}: {
  date: string;
  relative?: boolean;
}) {
  const d = dayjs(date).tz("Asia/Tokyo");

  const absoluteValue = d.format("YYYY-MM-DD HH:mm");
  const hydrated = useSyncExternalStore(
    subscribe,
    () => true,
    () => false,
  );
  const displayValue = hydrated && relative ? d.fromNow() : absoluteValue;
  const fullTimeTitle = d.format("YYYY-MM-DD HH:mm:ss");

  return (
    <time
      dateTime={d.toISOString()}
      className="entry-date"
      title={fullTimeTitle}
    >
      {displayValue}
    </time>
  );
}
