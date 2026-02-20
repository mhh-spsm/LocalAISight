// This is not currently beeing used - keeping for reference
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
namespace LocalAISight.Extensions;

public class HtmlPreProcessor : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        pipeline.DocumentProcessed += ProcessDocument;
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
    }

    private void ProcessDocument(MarkdownDocument document)
    {
        // FALL 2: Ensamt strong-inline som inte ligger i ParagraphBlock
        foreach (var block in document.ToList())
        {
            if (block is LeafBlock leaf && leaf.Inline?.FirstChild is EmphasisInline emph)
            {
                if (IsStrongStandaloneInline(emph))
                {
                    var text = ((LiteralInline)emph.FirstChild).Content.ToString();

                    var heading = new HeadingBlock(null)
                    {
                        Level = 2,
                        Inline = new ContainerInline()
                    };
                    heading.Inline.AppendChild(new LiteralInline(text));

                    ReplaceBlock(document, block, heading);
                }
            }
        }

        // Vi måste iterera på en kopia eftersom vi kan byta ut block
        foreach (var block in document.ToList())
        {
            if (block is ParagraphBlock paragraph)
            {
                var inline = paragraph.Inline?.FirstChild;

                // --- 1. Rubrik-detektering ---
                if (IsStrongSingleLineHeading(paragraph))
                {
                    string headingText = ExtractStrongText(paragraph);

                    var heading = new HeadingBlock(null)
                    {
                        Level = 2,
                        Inline = new ContainerInline()
                    };

                    heading.Inline.AppendChild(new LiteralInline(headingText));

                    ReplaceBlock(document, block, heading);
                    continue;
                }

                // --- 2. Lägg till <br> för \n (inte i listor)
                if (!(block.Parent is ListBlock) && !(block.Parent is ListItemBlock))
                {
                    ReplaceNewlines(paragraph);
                }
            }
        }
    }

    private bool IsStrongSingleLineHeading(ParagraphBlock paragraph)
    {
        if (paragraph?.Inline == null) return false;

        // Paragrafen måste ha exakt EN inline-nod
        if (paragraph.Inline.FirstChild is EmphasisInline emph &&
            emph.DelimiterCount == 2 &&    // '**' → strong
            emph.FirstChild is LiteralInline &&
            emph.NextSibling == null &&
            emph.PreviousSibling == null)
        {
            return true;
        }

        return false;
    }

    private string ExtractStrongText(ParagraphBlock paragraph)
    {
        var emph = (EmphasisInline)paragraph.Inline.FirstChild;
        var lit = (LiteralInline)emph.FirstChild;

        return lit.Content.ToString().Trim();
    }

    private void ReplaceBlock(MarkdownDocument document, Block oldBlock, Block newBlock)
    {
        var parent = oldBlock.Parent as ContainerBlock;

        if (parent == null)
        {
            // block ligger direkt under dokumentet
            int index = document.IndexOf(oldBlock);
            document.RemoveAt(index);
            document.Insert(index, newBlock);
        }
        else
        {
            int index = parent.IndexOf(oldBlock);
            parent.RemoveAt(index);
            parent.Insert(index, newBlock);
        }
    }

    private void ReplaceNewlines(ParagraphBlock paragraph)
    {
        Inline? current = paragraph.Inline?.FirstChild;

        while (current != null)
        {
            if (current is LiteralInline lit)
            {
                string text = lit.Content.ToString();
                if (text.Contains("\\n"))
                {
                    lit.Content = new Markdig.Helpers.StringSlice(
                        text.Replace("\\n", "<br/>")
                    );
                }
            }

            current = current.NextSibling;
        }
    }
    private bool IsStrongStandaloneInline(EmphasisInline emph)
    {
        return emph.DelimiterCount == 2 &&
               emph.FirstChild is LiteralInline &&
               emph.PreviousSibling == null &&
               emph.NextSibling == null;
    }
}