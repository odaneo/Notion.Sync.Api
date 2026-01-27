import { Mail, Github } from "lucide-react";
import Link from "next/link";

export default async function Contact() {
  return (
    <main className="mt-5 w-full">
      <h3 className="text-2xl ml-4 mb-8 italic">联系我</h3>
      <div className="flex flex-col gap-y-5 mx-10">
        <div className="flex flex-wrap">
          <Mail className="mr-5" />
          <Link href={"mailto:odaneo@outlook.com"}>odaneo@outlook.com</Link>
        </div>
        <div className="flex flex-wrap">
          <Github className="mr-5" />
          <Link target="_blank" href={"https://github.com/odaneo"}>
            github.com/odaneo
          </Link>
        </div>
      </div>
    </main>
  );
}
