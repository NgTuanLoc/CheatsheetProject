import { unified } from 'unified';
import remarkParse from 'remark-parse';
import remarkGfm from 'remark-gfm';
import remarkRehype from 'remark-rehype';
import rehypeRaw from 'rehype-raw';
import rehypeShiki from '@shikijs/rehype';
import rehypeStringify from 'rehype-stringify';
import { RenderedMarkdown } from './RenderedMarkdown';

export async function MarkdownRenderer({ content }: { content: string }) {
  const file = await unified()
    .use(remarkParse)
    .use(remarkGfm)
    .use(remarkRehype, { allowDangerousHtml: true })
    .use(rehypeRaw)
    .use(rehypeShiki, {
      themes: { light: 'github-light', dark: 'catppuccin-mocha' },
    })
    .use(rehypeStringify)
    .process(content);

  return <RenderedMarkdown html={String(file)} />;
}
