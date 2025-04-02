namespace sequential_reveal_md_tool;

public static class MarkdownSectionSplitter
{
    private const string codeBlockLineStart = "```";
    private const string htmlCommentLineStart = "<!--";
    private const string htmlLineStart = "<";

    public static List<List<string>> Split(List<string> markdownLines)
    {
        var sections = new List<List<string>>();
        var currentSection = new List<string>();
        var alreadyInsideACodeBlock = false;

        foreach (var line in markdownLines)
        {
            // If the line starts or ends a code block
            if(line.TrimStart().StartsWith(codeBlockLineStart))
            {
                if(alreadyInsideACodeBlock)
                {
                    // Close current code block and start a new section
                    currentSection.Add(line);
                    AddAndClearCurrentSection(currentSection, sections);
                    alreadyInsideACodeBlock = false;
                    continue;
                }
                else
                {
                    // Start a new code block section
                    if(currentSection.ContainsNonWhiteSpaceLines())
                        AddAndClearCurrentSection(currentSection, sections);
                    
                    currentSection.Add(line);
                    alreadyInsideACodeBlock = true;
                    continue;
                }
            }

            // If it's a new printable markdown line, start a new section
            if (!line.StartsWith(htmlCommentLineStart) && !line.StartsWith(htmlLineStart) && !string.IsNullOrWhiteSpace(line) && alreadyInsideACodeBlock == false)
            {
                currentSection.Add(line);
                AddAndClearCurrentSection(currentSection, sections);
                continue;
            }

            // otherwise just add the line to the current section
            currentSection.Add(line);
        }

        // Add the last section if it exists
        if (currentSection.ContainsNonWhiteSpaceLines())
            sections.Add(currentSection);

        return sections;
    }

    private static bool ContainsNonWhiteSpaceLines(this List<string> currentSection)
    {
        return currentSection.Any(x => string.IsNullOrWhiteSpace(x) != true);
    }

    private static void AddAndClearCurrentSection(List<string> currentSection, List<List<string>> sections)
    {
        sections.Add([.. currentSection]);
        currentSection.Clear();
    }
}
