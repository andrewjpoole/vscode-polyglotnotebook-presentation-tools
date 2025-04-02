namespace sequential_reveal_md_tool;

public class MarkdownSectionSplitter
{
    public static List<List<string>> Split(List<string> markdownLines)
    {
        var sections = new List<List<string>>();
        var currentSection = new List<string>();

        foreach (var line in markdownLines)
        {
            // If it's a new printable markdown line, start a new section
            if (!line.StartsWith("<!--") && !line.StartsWith("<") && !string.IsNullOrWhiteSpace(line))
            {
                currentSection.Add(line);
                sections.Add([.. currentSection]);
                currentSection.Clear();
                continue;
            }

            // otherwise just add the line to the current section
            currentSection.Add(line);
        }

        // Add the last section if it exists
        if (currentSection.Count > 0)
        {
            sections.Add(currentSection);
        }

        return sections;
    }
}
