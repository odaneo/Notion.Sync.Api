import Link from "next/link";

export default function NotFound() {
  return (
    <header className="page-heading">
      <p className="eyebrow">404</p>
      <h1>找不到页面</h1>
      <Link href="/" className="primary-link">
        回到首页
      </Link>
    </header>
  );
}
