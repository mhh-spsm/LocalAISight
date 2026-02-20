using HtmlAgilityPack;
using System.Linq;
namespace LocalAISight.Extensions;

public static class HtmlPostProcessor
{
    public static string Transform(string html)
    {
        var doc = new HtmlDocument();
        doc.OptionCheckSyntax = false;
        doc.OptionAutoCloseOnEnd = true;
        doc.LoadHtml(html);

        PromoteStrongLineToHeading(doc, headingLevel: 2);
        ReplaceLiteralBackslashNWithBrOutsideLists(doc.DocumentNode, doc);

        return doc.DocumentNode.OuterHtml;
    }

    private static void PromoteStrongLineToHeading(HtmlDocument doc, int headingLevel)
    {
        var pNodes = doc.DocumentNode.SelectNodes("//p");
        if (pNodes == null) return;

        foreach (var p in pNodes.ToList())
        {
            // Hitta första "meningsfulla" noden i <p>
            var first = p.ChildNodes.FirstOrDefault(n => !IsWhitespaceText(n));
            if (first == null || !first.Name.Equals("strong", System.StringComparison.OrdinalIgnoreCase))
                continue;

            // Nästa meningsfulla nod efter <strong>
            var afterStrong = NextMeaningfulSibling(first);
            // Kräver att det är en <br> (tack vare UseSoftlineBreakAsHardlineBreak)
            if (afterStrong == null || !afterStrong.Name.Equals("br", System.StringComparison.OrdinalIgnoreCase))
                continue;

            // Bygg <h2>Rubrik</h2>
            var h = doc.CreateElement($"h{headingLevel}");
            h.InnerHtml = first.InnerHtml;

            // Infoga h2 före p
            p.ParentNode.InsertBefore(h, p);

            // Ta bort <strong> och <br> ur paragrafen
            p.RemoveChild(first);
            p.RemoveChild(afterStrong);

            // Rensa ledande whitespace‑textnoder efter borttagning
            while (p.ChildNodes.Count > 0 && IsWhitespaceText(p.ChildNodes[0]))
                p.RemoveChild(p.ChildNodes[0]);

            // Om <p> nu är tomt → ta bort
            if (string.IsNullOrWhiteSpace(p.InnerText))
                p.ParentNode.RemoveChild(p);
        }
    }

    private static HtmlNode NextMeaningfulSibling(HtmlNode node)
    {
        var s = node.NextSibling;
        while (s != null && IsWhitespaceText(s))
            s = s.NextSibling;
        return s;
    }

    private static bool IsWhitespaceText(HtmlNode n) =>
        n.NodeType == HtmlNodeType.Text && string.IsNullOrWhiteSpace(n.InnerText);

private static void ReplaceLiteralBackslashNWithBrOutsideLists(HtmlNode node, HtmlDocument doc, bool insideList = false)
    {
        bool nowInsideList = insideList || node.Name is "ul" or "ol" or "li" or "pre" or "code";

        if (!nowInsideList)
        {
            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                var child = node.ChildNodes[i];
                if (child.NodeType == HtmlNodeType.Text)
                {
                    var text = child.InnerHtml;
                    // Endast bokstavliga "\n" – låt pipeline-genererat <br/> vara ifred
                    if (text?.Contains("\\n") == true)
                    {
                        var parts = text.Split(new[] { "\\n" }, StringSplitOptions.None);
                        HtmlNode refNode = child;
                        for (int p = 0; p < parts.Length; p++)
                        {
                            node.InsertBefore(doc.CreateTextNode(parts[p]), refNode);
                            if (p < parts.Length - 1)
                                node.InsertBefore(doc.CreateElement("br"), refNode);
                        }
                        node.RemoveChild(child);
                        i--;
                    }
                }
            }
        }

        foreach (var c in node.ChildNodes.ToList())
            ReplaceLiteralBackslashNWithBrOutsideLists(c, doc, nowInsideList);
    }
}