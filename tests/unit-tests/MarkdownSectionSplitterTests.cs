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
}
