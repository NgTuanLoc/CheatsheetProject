'use client';

import { useEffect, useRef } from 'react';
import { toast } from 'sonner';

export function RenderedMarkdown({ html }: { html: string }) {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const container = ref.current;
    if (!container) return;
    container.querySelectorAll('pre').forEach((pre) => {
      if (pre.querySelector('.copy-btn')) return;
      const code = pre.querySelector('code')?.innerText ?? '';
      const btn = document.createElement('button');
      btn.className =
        'copy-btn absolute top-2 right-2 px-2 py-1 text-xs rounded-md glass opacity-0 group-hover:opacity-100 transition-opacity cursor-pointer';
      btn.textContent = 'Copy';
      btn.onclick = () => {
        navigator.clipboard.writeText(code).then(() => {
          btn.textContent = 'Copied!';
          toast.success('Copied to clipboard');
          setTimeout(() => {
            btn.textContent = 'Copy';
          }, 2000);
        });
      };
      pre.classList.add('group', 'relative');
      pre.appendChild(btn);
    });
  }, [html]);

  return (
    <div
      ref={ref}
      className="prose max-w-none"
      dangerouslySetInnerHTML={{ __html: html }}
    />
  );
}
