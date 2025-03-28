using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Events;

namespace SequentialRevealMarkdownToolTests;

public class ExperimentalTests
{
    //[Test]
    public async Task Test()
    {        
        var notebookFileInfo = new FileInfo(TestContext.CurrentContext.TestDirectory + "/testData/sequentialReveal/initial.ipynb");
        
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

        await kernel.LoadAndRunInteractiveDocument(notebookFileInfo);

        //var csharpKernel = new CSharpKernel();
        //kernel.Add(csharpKernel);               
        //var result = await csharpKernel.SubmitCodeAsync("Console.WriteLine(\"hello world!\")");

        
        
    }
}