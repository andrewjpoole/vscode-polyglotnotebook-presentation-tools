using sequential_reveal_md_tool;

namespace SequentialRevealMarkdownToolTests;

public class MarkdownSectionSplitterTests
{
    [Test]
    public void SplitMarkdownIntoSections_ShouldSplitCorrectly()
    {
        var markdown = new List<string> 
        {
            "<!-- Comment to show content hint while cell is collapsed -->\n",
            "<link rel=\"stylesheet\" href=\"styles.css\">\n",
            "\n",
            "# A Title\n",
            "\n",
            "## A new Section\n",
            "\n",
            "A paragraph of text\n",
            "\n",
            "- bullet point one\n",
            "- bullet point two\n",
            "* bullet point three\n",
            "\n",
            "Another paragraph of text\n",
            "\n",
            "\n",
            "\n",
            "1. Bonus numeric list one\n",
            "<!-- Random comment -->\n",
            "2. Bonus numeric list two\n",
            "\n",
            "[done]\n"
        };

        var sections = MarkdownSectionSplitter.Split(markdown);

        Assert.That(sections.Count, Is.EqualTo(10));
    }

    [Test]
    public void SplitMarkdownIntoSections_ShouldSplitCorrectly_WithCodeBlocksAsSections()
    {
        var markdown = new List<string> 
        {
           "<link rel=\"stylesheet\" href=\"styles.css\">\n",
            "\n",
            "# Some markdown containing some additional things that should be treated as separate sections\n",
            "\n",
            "```csharp\n",
            "var now = TimeProvider.System.GetUtcNow();\n",
            "Console.Writeline($\"Hello world@{now}\")\n",
            "```\n",
            "\n",
            "Some text after the code block\n",
        };

        var sections = MarkdownSectionSplitter.Split(markdown);

        Assert.That(sections.Count, Is.EqualTo(3));
        Assert.That(sections[1].Count, Is.EqualTo(5));
        Assert.That(sections[1][1], Is.EqualTo("```csharp\n"));
        Assert.That(sections[1][2], Is.EqualTo("var now = TimeProvider.System.GetUtcNow();\n"));
        Assert.That(sections[1][3], Is.EqualTo("Console.Writeline($\"Hello world@{now}\")\n"));
        Assert.That(sections[1][4], Is.EqualTo("```\n"));
    }

    [Test]
    public void Split_ShouldAddSinglelineHtmlToTheNextSectionWithPrintableText()
    {
        var markdownLines = new List<string>
        {
            "<div>This is a single-line HTML block.</div>",
            "Another line of markdown."
        };

        var sections = MarkdownSectionSplitter.Split(markdownLines);

        Assert.That(sections.Count, Is.EqualTo(1));
        Assert.That(sections[0], Is.EqualTo(new List<string>
        {
            "<div>This is a single-line HTML block.</div>",
            "Another line of markdown."
        }));
    }

    [Test]
    public void Split_ShouldAddSinglelineHtmlCommentToTheNextSectionWithPrintableText()
    {
        var markdownLines = new List<string>
        {
            "<!-- This is an html comment-->",
            "Another line of markdown."
        };

        var sections = MarkdownSectionSplitter.Split(markdownLines);

        Assert.That(sections.Count, Is.EqualTo(1));
        Assert.That(sections[0], Is.EqualTo(new List<string>
        {
            "<!-- This is an html comment-->",
            "Another line of markdown."
        }));
    }

    [Test]
    public void Split_ShouldAddSeveralSingleHtmlLinesToTheNextSectionWithPrintableText()
    {
        var markdownLines = new List<string>
        {
            "<!-- This is an html comment-->",
            "<div>This is a single-line HTML block.</div>",
            "<link rel=\"stylesheet\" href=\"styles.css\">",
            "Another line of markdown."
        };

        var sections = MarkdownSectionSplitter.Split(markdownLines);

        Assert.That(sections.Count, Is.EqualTo(1));
        Assert.That(sections[0].Count, Is.EqualTo(4));
    }

    [Test]
    public void Split_ShouldAddMultilineHtmlToTheNextSectionWithPrintableText()
    {
        var markdownLines = new List<string>
        {
            "<div>",
            "This is inside a multiline HTML block.",
            "<p>Another HTML tag.</p>",
            "</div>",
            "Another line of markdown."
        };

        var sections = MarkdownSectionSplitter.Split(markdownLines);

        Assert.That(sections.Count, Is.EqualTo(1));
        Assert.That(sections[0], Is.EqualTo(new List<string>
        {
            "<div>",
            "This is inside a multiline HTML block.",
            "<p>Another HTML tag.</p>",
            "</div>",
            "Another line of markdown."
        }));
    }

    [Test]
    public void Split_ShouldAddMultilineHtmlContainingScriptToTheNextSectionWithPrintableText()
    {
        var markdownLines = new List<string>
        {
            "<script>",
            "startTypewriter = function(codeId) {",
            "    var typewriter = setupTypewriter(document.getElementById(\"typewriter\"));",
            "    typewriter.setSyntaxHighlighting(true);",
            "    typewriter.setTypeSpeed(300);",
            "    typewriter.parseString(htmlUnescape(document.getElementById(codeId).innerHTML));",
            "    typewriter.type();",
            "}",
            "</script>",
            "Another line of markdown."
        };

        var sections = MarkdownSectionSplitter.Split(markdownLines);

        Assert.That(sections.Count, Is.EqualTo(1));
    }

    [Test]
    public void Split_ShouldThrowIfHtmlIsInvalid()
    {
        var markdownLines = new List<string>
        {
            "<div>",
            "This is inside a multiline HTML block.",
            "<p>Another HTML tag.</p>", // unclosed tag
            "Another line of markdown."
        };

        Assert.Throws<FormatException>(() => MarkdownSectionSplitter.Split(markdownLines));        
    }
}
