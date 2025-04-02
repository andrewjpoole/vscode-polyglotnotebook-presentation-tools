using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Documents.Utility;

namespace SequentialRevealMarkdownToolTests;

public class ExperimentalTests
{
    //[Test]
    public async Task Test()
    {        
               
        var DefaultKernelInfos = new KernelInfoCollection
        {
            new("csharp", "C#", new[] { "cs", "C#", "c#" })
        };
        DefaultKernelInfos.DefaultKernelName = "csharp";

        var jupyter = new
        {
            cells = new object[]
            {
                new
                {
                    cell_type = "code",
                    execution_count = 0,
                    source = new[] { "// this is the code" }
                }
            },
            metadata = new
            {
                kernelspec = new
                {
                    display_name = $".NET (cs)",
                    language = "cs",
                    name = $".net-csharp"
                },
                language_info = new
                {
                    file_extension = ".cs",
                    mimetype = $"text/x-csharp",
                    name = "cs",
                    pygments_lexer = "csharp",
                    version = "8.0"
                }
            },
            nbformat = 4,
            nbformat_minor = 4
        };

        var content = JsonSerializer.Serialize(jupyter);
        var interactiveDoc = Notebook.Parse(content, DefaultKernelInfos);
        // what can I do with this now?
        

        var kernel = new CompositeKernel
        {
            new CSharpKernel()
                //.UseNugetDirective()
                .UseKernelHelpers()
                .UseWho()
                .UseValueSharing()
                .UseImportMagicCommand(),            

            new HtmlKernel(),

            new JavaScriptKernel()
                .UseWho()
                .UseValueSharing()
                .UseImportMagicCommand(),
        }
        //.UseLogMagicCommand()
        .UseImportMagicCommand()
        .UseNuGetExtensions();

        var diagnostics = new List <Diagnostic>();
        kernel.KernelEvents.Subscribe(x =>
        {
            if (x is DiagnosticsProduced diag)
                diagnostics.AddRange(diag.Diagnostics);
        });

        var outputs = new List <string>();
        kernel.KernelEvents.Subscribe(x =>
        {
            if (x is DisplayEvent evt)
                {
                    if(evt.Value is not ScriptContent && evt.Value is not InstallPackagesMessage)
                        outputs.AddRange(evt.FormattedValues.Select(y => y.Value));
                }
        });

        var notebookFileInfo = new FileInfo(TestContext.CurrentContext.TestDirectory + "/testData/sequentialReveal/initial.ipynb");
        await kernel.LoadAndRunInteractiveDocument(notebookFileInfo);
        // this runs but looking at the diagnostic outputs, it cant find project/nuget references


        //var csharpKernel = new CSharpKernel();
        //kernel.Add(csharpKernel);               
        //var result = await csharpKernel.SubmitCodeAsync("Console.WriteLine(\"hello world!\")");
        // this works.        
        
    }
}