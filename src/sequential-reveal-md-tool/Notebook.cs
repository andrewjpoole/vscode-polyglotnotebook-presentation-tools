using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive;

namespace sequential_reveal_md_tool;

public record Notebook(
    [property: JsonPropertyName("cells")] List<NotebookCell> Cells,
    [property: JsonPropertyName("metadata")] NotebookMetadata? Metadata,
    [property: JsonPropertyName("nbformat")] int NbFormat,
    [property: JsonPropertyName("nbformat_minor")] int NbFormatMinor
)
{
    public static Notebook FromJson(string json)
    {
        var notebook = JsonSerializer.Deserialize<Notebook>(json) ?? throw new ArgumentException("Invalid JSON string.");
        if(notebook.Cells is null)
            throw new ArgumentException("Notebook contains no cells.");

        return notebook;
    }

    public static Notebook FromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Notebook file not found.", filePath);
        }

        string notebookContent = File.ReadAllText(filePath);
        var notebook = FromJson(notebookContent);

        if(notebook.Cells is null)
            throw new ArgumentException("Notebook contains no cells.");

        return notebook;
    }

    public int GetIndexOfCellWithTag(string tag)
    {
        if(Cells is null)
            throw new ArgumentException("Notebook contains no cells.");
        
        return Cells.FindIndex(cell => cell.Metadata?.Tags?.Contains(tag) == true);
    }

    public NotebookCell? GetCellWithTag(string tag)
    {
        if(Cells is null)
            throw new ArgumentException("Notebook contains no cells.");

        var cellIndex = GetIndexOfCellWithTag(tag);

        if(cellIndex == -1)
            return null;

        return Cells[cellIndex];
    }

    /// <summary>
    /// Display the next sequential section from the preceding Markdown cell.
    /// </summary>
    /// <param name="tag">A string containing a cell tag used to locate the current cell in the notebook file, consequently also used to find the preceding markdown cell.</param>
    /// <param name="appendLinesOfSpace">An integer which if non zero, will trigger a div of the specified height (rem) to be appended to the end of the markdown. 
    /// This should prevent the next slide content from showing.
    /// </param>
    public void DisplayNextSequentialSectionFromPrecedingMarkdownCell(string tag, int appendLinesOfSpace = 0)
    {
        var markdownContent = RenderNextSequentialSectionFromPrecedingMarkdownCell(tag, appendLinesOfSpace);
        markdownContent.DisplayAs("text/markdown");
    }    

    public string RenderNextSequentialSectionFromPrecedingMarkdownCell(string tag, int appendLinesOfSpace = 0)
    {
        var appendSpaceString = $"<div style=\"display:block; height:{appendLinesOfSpace}rem;\"></div>";
 
        if(Cells is null)
            throw new ArgumentException("Notebook contains no cells.");

        var currentCellIndex = GetIndexOfCellWithTag(tag);

        if(currentCellIndex == -1)
            throw new ArgumentException("Tag not found in any cell.");

        if(currentCellIndex == 0)
            throw new ArgumentException("Tag found in first cell. There must be a preceding cell containing Markdown.");

        var precedingMarkdownCellIndex = currentCellIndex - 1;

        var currentCell = Cells[currentCellIndex];
        var precedingMarkdownCell = Cells[precedingMarkdownCellIndex];

        if(precedingMarkdownCell.CellType != "markdown")
            throw new ArgumentException("The preceding cell is not a Markdown cell.");       

        // Extract the Markdown content from the previous cell
        var markdownLines = precedingMarkdownCell.Source;
        if(markdownLines == null)
            throw new ApplicationException("No content found in the preceding Markdown cell.");

        var markdownSections = MarkdownSectionSplitter.Split(markdownLines);

        var sb = new StringBuilder();

        // If no previous output just return the first section
        var markdownOutputs = currentCell.GetMarkdownOutputs();
        if(markdownOutputs.Count == 0)
        { 
            // Output first markdown section
            foreach (var line in markdownSections.First())
                sb.AppendLine(line.Trim());        

            if(appendLinesOfSpace > 0)
                sb.AppendLine(appendSpaceString);

            return sb.ToString();
        }

        // Enumerate outputs to determine the last displayed Markdown, there is likely only one output.
        var lastMarkdownOutput = "";
        foreach(var markdownOutput in markdownOutputs)
        {
            var outputDataLines = markdownOutput.GetMarkdownLines();

            // Remove appended space div if present
            if(appendLinesOfSpace > 0 && outputDataLines[^1].Trim() == appendSpaceString)
                outputDataLines[^1] = string.Empty;

            if (outputDataLines != null && outputDataLines.Length > 0)
                lastMarkdownOutput = outputDataLines.Last(x => string.IsNullOrWhiteSpace(x) == false);
        }        

        var lastMarkdownOutputLine = lastMarkdownOutput.Split(["\n"], StringSplitOptions.RemoveEmptyEntries).Last();

        // Determine how many sections to display
        int sectionsToDisplay = 1;
        for (int i = 0; i < markdownSections.Count; i++)
        {
            if (lastMarkdownOutputLine.Trim() == markdownSections[i].Last().Trim())
            {
                sectionsToDisplay = i + 1;
                break;
            }
        }

        if(sectionsToDisplay >= markdownSections.Count)
            sectionsToDisplay = markdownSections.Count - 1;

        // Build the output with the appropriate number of sections    
        for (int i = 0; i <= sectionsToDisplay; i++)
        {
            foreach (var line in markdownSections[i])
                sb.AppendLine(line.Trim()); 
        }

        if(appendLinesOfSpace > 0)
            sb.AppendLine(appendSpaceString);

        return sb.ToString();
    }    
}

public record NotebookCell(
    [property: JsonPropertyName("cell_type")] string? CellType,
    [property: JsonPropertyName("metadata")] CellMetadata? Metadata,
    [property: JsonPropertyName("source")] List<string>? Source,
    [property: JsonPropertyName("outputs")] List<CellOutput>? Outputs
)
{
    public List<CellOutput> GetMarkdownOutputs()
    {
        return Outputs?.Where(output => output.OutputType == "display_data" && output.Data?.ContainsKey("text/markdown") == true).ToList() ?? new List<CellOutput>();    
    }

    public string[] GetSourceLines()
    {
        return Source?.ToArray() ?? new string[0];
    }
}

public record CellMetadata(
    [property: JsonPropertyName("tags")] List<string>? Tags
);

public record CellOutput(
    //[property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("output_type")] string? OutputType,
    [property: JsonPropertyName("data")] Dictionary<string, object>? Data,
    [property: JsonPropertyName("metadata")] object? MetaData = null
)
{
    public static CellOutput FromMarkdownLines(string[] outputLines)
    {
        var output = new CellOutput("display_data", new Dictionary<string, object> { { "text/markdown", outputLines } });
        return output;
    }

    public string[] GetMarkdownLines()
    {
        if(Data is null)
            return [];

        var dictionaryValue = (JsonElement)Data["text/markdown"];
        var markdownLines = dictionaryValue.EnumerateArray().Select(x => x.GetString()).ToArray();

        return markdownLines ?? [];
    }
}

public record NotebookMetadata(
    [property: JsonPropertyName("kernelspec")] KernelSpec? KernelSpec,
    [property: JsonPropertyName("language_info")] LanguageInfo? LanguageInfo
);

public record KernelSpec(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("display_name")] string? DisplayName
);

public record LanguageInfo(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("version")] string? Version,
    [property: JsonPropertyName("mimetype")] string? MimeType,
    [property: JsonPropertyName("file_extension")] string? FileExtension
);
