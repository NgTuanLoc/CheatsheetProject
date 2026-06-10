'use client';

export function HtmlRenderer({ content }: { content: string }) {
  return (
    <iframe
      srcDoc={content}
      sandbox="allow-same-origin"
      className="w-full min-h-[600px] border-0 rounded-xl"
      title="HTML cheatsheet"
      onLoad={(e) => {
        const iframe = e.currentTarget;
        const body = iframe.contentDocument?.body;
        if (body) {
          iframe.style.height = `${body.scrollHeight + 32}px`;
        }
      }}
    />
  );
}
