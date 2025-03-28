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
    public void DisplayNextSequentialSectionFromPrecedingMarkdownCell(string tag)
    {
        var markdownContent = RenderNextSequentialSectionFromPrecedingMarkdownCell(tag);
        markdownContent.DisplayAs("text/markdown");
    }

    /// <summary>
    /// Display the next sequential section from the preceding Markdown cell using a style string.
    /// This method is useful for applying specific styles to the displayed content.
    /// </summary>
    /// <param name="tag">A string containing a cell tag used to locate the current cell in the notebook file, consequently also used to find the preceding markdown cell.</param>
    /// <param name="styleString">A string containing css styles which will be prepended to the markdown output, intended to be used to make font size large enough to see during a presentation.
    // example: <link rel=\"stylesheet\" href=\"styles.css\"> where styles.css is a file in the same directory as the notebook file.
    // example: <style>font-size: 2em;</style> where the style is applied to the markdown output.
    // </param>
    public void DisplayNextSequentialSectionFromPrecedingMarkdownCellUsingStyleString(string tag, string styleString)
    {
        var styleLines = styleString.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

        var markdownContent = RenderNextSequentialSectionFromPrecedingMarkdownCell(tag, styleLines);
        markdownContent.DisplayAs("text/markdown");
    }    

    public string RenderNextSequentialSectionFromPrecedingMarkdownCell(string tag, string[]? styleLines = null)
    {
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

        // If we have a styleLines, we should remove them from markdownLines here...
        if(styleLines != null)
        {
            for (int i = 0; i < styleLines.Length; i++)
            {
                if(markdownLines[0] == styleLines[i])
                    markdownLines.Remove(styleLines[i]);
            }
        }
       
        var markdownSections = markdownLines
                        .Where(x => string.IsNullOrWhiteSpace(x)== false)
                        .ToArray();

        var sb = new StringBuilder();

        // If no previous output just return the first section
        if(currentCell.Outputs is null || currentCell.Outputs.Count == 0)
        {            
            if(styleLines != null)
            {
                foreach (var line in styleLines)
                {
                    sb.Append(line);
                }
                sb.AppendLine(Environment.NewLine);
            }
            sb.AppendLine(markdownSections[0]);
            return sb.ToString();
        }

        // Enumerate outputs to determine the last displayed Markdown, there is likely only one output.
        var lastMarkdownOutput = "";
        foreach(var markdownOutputCell in currentCell.GetMarkdownOutputs())
        {
            var outputDataLines = markdownOutputCell.GetMarkdownLines();
            if (outputDataLines != null && outputDataLines.Length > 0)
            {
                lastMarkdownOutput = outputDataLines.Last(x => string.IsNullOrWhiteSpace(x) == false);
            }
        }

        var lastMarkdownoutputLine = lastMarkdownOutput.Split(["\n"], StringSplitOptions.RemoveEmptyEntries).Last();

        // Determine how many sections to display
        int sectionsToDisplay = 1;
        for (int i = 0; i < markdownSections.Length; i++)
        {
            if (lastMarkdownoutputLine == markdownSections[i].Trim())
            {
                sectionsToDisplay = i + 1;
                break;
            }
        }

        if(sectionsToDisplay >= markdownSections.Length)
            sectionsToDisplay = markdownSections.Length - 1;

        // Build the output with the appropriate number of sections
        if(styleLines != null)
        {
            foreach (var line in styleLines)
            {
                sb.Append(line);
            }
            sb.AppendLine(Environment.NewLine);
        }

        for (int i = 0; i <= sectionsToDisplay; i++)
        {
            sb.AppendLine(markdownSections[i]);
        }

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
