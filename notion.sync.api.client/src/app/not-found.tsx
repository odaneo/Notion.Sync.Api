import Link from "next/link";

export default function NotFound() {
  return (
    <div className="flex flex-col items-center justify-center mt-20">
      <h1 className="text-4xl font-bold text-error">404</h1>
      <p className="mt-2 text-lg">找不到页面</p>
      <Link href="/" className="btn btn-primary mt-4">
        回到首页
      </Link>
    </div>
  );
}
