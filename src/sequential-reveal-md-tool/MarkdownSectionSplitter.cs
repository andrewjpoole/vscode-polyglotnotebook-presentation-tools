using System.Text.RegularExpressions;

namespace sequential_reveal_md_tool;

public static class MarkdownSectionSplitter
{
    private const string codeBlockLineStart = "```";
    private const string htmlLineStart = "<";    

    public static List<List<string>> Split(List<string> markdownLines)
    {
        var sections = new List<List<string>>();
        var currentSection = new List<string>();
        var alreadyInsideACodeBlock = false;
        var alreadyInsideHtmlBlock = false;
        var htmlBlockDepth = 0; // Track nested HTML blocks

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

            // Detect the start of an HTML block
            if (line.TrimStart().StartsWith(htmlLineStart))
            {
                if(line.Trim().ContainsHtmlStartAndMatchingEndTags() 
                    || line.Trim().ContainsHtmlVoidElement()
                    || line.Trim().ContainsHtmlSelfClosingTag() 
                    || line.Trim().LineContainsHtmlComment())
                {
                    // Single line HTML block detected
                    currentSection.Add(line);
                    continue;
                }

                if(line.Trim().ContainsHtmlStartTag())
                {
                    // Opening tag detected
                    htmlBlockDepth++;
                    alreadyInsideHtmlBlock = true;
                    currentSection.Add(line);
                    continue;
                }

                if(line.Trim().ContainsHtmlEndTag())
                {
                    // Closing tag detected
                    htmlBlockDepth--;
                    if (htmlBlockDepth <= 0)
                    {
                        alreadyInsideHtmlBlock = false;
                        htmlBlockDepth = 0;
                    }
                    currentSection.Add(line);
                    continue;
                }
            }

            // If it's a new printable markdown line, start a new section
            if (!string.IsNullOrWhiteSpace(line) 
                && alreadyInsideACodeBlock == false 
                && alreadyInsideHtmlBlock == false)
            {
                currentSection.Add(line);
                AddAndClearCurrentSection(currentSection, sections);
                continue;
            }

            // otherwise just add the line to the current section
            currentSection.Add(line);
        }

        // Check for unclosed HTML blocks
        if (htmlBlockDepth > 0)
        {
            throw new FormatException("Unclosed HTML block detected.");
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

    private static bool ContainsHtmlStartAndMatchingEndTags(this string line)
    {
        var regex = new Regex(@"^<[^/!][^>]*>.*</[^>]+>$");
        return regex.IsMatch(line);
    }

    private static bool ContainsHtmlVoidElement(this string line)
    {
        var regex = new Regex(@"^<\s*(area|base|br|col|embed|hr|img|input|link|meta|param|source|track|wbr)(\s+[^>]*)?>$");
        return regex.IsMatch(line);
    }

    private static bool ContainsHtmlSelfClosingTag(this string line)
    {
        var regex = new Regex(@"^<[^/!][^>]*\s*/>$");
        return regex.IsMatch(line);
    }

    private static bool ContainsHtmlStartTag(this string line)
    {
        var regex = new Regex(@"^<[^/!][^>]*[^/]>$");
        return regex.IsMatch(line);
    }

    private static bool ContainsHtmlEndTag(this string line)
    {
        var regex = new Regex(@"^</[^>]+>$");
        return regex.IsMatch(line);
    }

    private static bool LineContainsHtmlComment(this string line)
    {
        return line.TrimStart().StartsWith("<!--") && line.TrimEnd().EndsWith("-->");
    }
}
