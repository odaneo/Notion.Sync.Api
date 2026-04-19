"use client";

import { useEffect, useState } from "react";
import dayjs from "dayjs";
import relativeTime from "dayjs/plugin/relativeTime";
import utc from "dayjs/plugin/utc";
import timezone from "dayjs/plugin/timezone";
import "dayjs/locale/zh-cn";

dayjs.extend(relativeTime);
dayjs.extend(utc);
dayjs.extend(timezone);

dayjs.locale("zh-cn");

export default function UpdatedAtJST({
  date,
  relative = true,
}: {
  date: string;
  relative?: boolean;
}) {
  const d = dayjs(date).tz("Asia/Tokyo");

  const absoluteValue = d.format("YYYY-MM-DD HH:mm");
  const [displayValue, setDisplayValue] = useState(absoluteValue);
  const fullTimeTitle = d.format("YYYY-MM-DD HH:mm:ss");

  useEffect(() => {
    const value = relative
      ? dayjs(date).tz("Asia/Tokyo").fromNow()
      : dayjs(date).tz("Asia/Tokyo").format("YYYY-MM-DD HH:mm");

    setDisplayValue(value);
  }, [date, relative]);

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
