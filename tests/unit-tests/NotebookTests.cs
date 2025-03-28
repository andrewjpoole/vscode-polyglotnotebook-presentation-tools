using System.Management.Automation;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.PowerShell;
using Namotion.Reflection;
using sequential_reveal_md_tool;

namespace SequentialRevealMarkdownToolTests;

[TestFixture]
public class NotebookTests
{
    private const string notebookJson = """
{
 "cells": [
  {
   "cell_type": "markdown",
   "metadata": {},
   "source": [
    "# Sequential Reveal Tester Notebook\n",
    "\n",
    "The following markdoen cell will contain some content which we might want to reveal sequentially, like the animation features of Powerpoint or Google Slides.\n",
    "\n",
    "The C# cell below that will render the sections from themarkdown cell one by one.\n",
    "\n",
    "In a presentation you would minimise the markdown cell and repeatedly run the the C# cell to sequentially show the content sections."
   ]
  },
  {
   "cell_type": "markdown",
   "metadata": {},
   "source": [
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
    "1. Bonus numeric list one\n",
    "2. Bonus numeric list two\n",
    "3. Bonus numeric list three\n"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "tags": [
     "cell1"
    ],
    "vscode": {
     "languageId": "polyglot-notebook"
    }
   },
   "outputs": [],
   "source": [
    "#r \"../src/sequential-reveal-md-tool/bin/Debug/net9.0/sequential-reveal-md-tool.dll\"\n",
    "\n",
    "using sequential_reveal_md_tool;\n",
    "\n",
    "SequentialRevealMarkdownTool.Render(\"sequential-reveal.ipynb\", \"cell1\");"
   ]
  },
  {
   "cell_type": "code",
   "execution_count": null,
   "metadata": {
    "tags": [
     "cell2"
    ],
    "vscode": {
     "languageId": "polyglot-notebook"
    }
   },
   "outputs": [],
   "source": [
    "Console.WriteLine(\"hello world!\");"
   ]
  }
 ],
 "metadata": {
  "kernelspec": {
   "display_name": ".NET (C#)",
   "language": "C#",
   "name": ".net-csharp"
  },
  "language_info": {
   "name": "python"
  },
  "polyglot_notebook": {
   "kernelInfo": {
    "defaultKernelName": "csharp",
    "items": [
     {
      "aliases": [],
      "name": "csharp"
     }
    ]
   }
  }
 },
 "nbformat": 4,
 "nbformat_minor": 2
}
""";

    [Test]
    public void GetIndexOfCellWithTag_ShouldReturnCorrectIndex_WhenTagExists()
    {
        var sut = Notebook.FromJson(notebookJson);
        var index = sut.GetIndexOfCellWithTag("cell1");

        Assert.That(index, Is.EqualTo(2));
    }

    [Test]
    public void GetIndexOfCellWithTag_ReturnsMinusOne_WhenTagDoesNotExist()
    {
        var sut = Notebook.FromJson(notebookJson);
        Assert.That(sut.GetIndexOfCellWithTag("nonexistent-tag"), Is.EqualTo(-1));
    }

    [Test]
    public void GetCellWithTag_ShouldReturnCorrectCell_WhenTagExists()
    {   
        var sut = Notebook.FromJson(notebookJson);
        var cell = sut.GetCellWithTag("cell1");

        Assert.That(cell, Is.Not.Null);
        Assert.That(cell.Metadata?.Tags?[0], Is.EqualTo("cell1"));
    }        

    [Test]
    public void RenderNextSequentialSectionFromPrecedingMarkdownCell_ShouldThrowException_WhenPrecedingCellIsNotMarkdown()
    {            
        var sut = Notebook.FromJson(notebookJson);
        var ex = Assert.Throws<ArgumentException>(() => sut.RenderNextSequentialSectionFromPrecedingMarkdownCell("cell2"));
        Assert.That(ex.Message, Is.EqualTo("The preceding cell is not a Markdown cell."));
    }

    [Test]
    public void RenderNextSequentialSectionFromPrecedingMarkdownCell_ShouldReturnCorrectMarkdown_OnFirstExecution()
    {        
        var sut = Notebook.FromFile(TestContext.CurrentContext.TestDirectory + "/testData/sequentialReveal/initial.ipynb");
        var markdown = sut.RenderNextSequentialSectionFromPrecedingMarkdownCell("cell1");

        Assert.That(markdown, Is.Not.Null);
        Assert.That(markdown, Is.EqualTo("## A new Section\n\r\n"));
    }

    [Test]
    public void RenderNextSequentialSectionFromPrecedingMarkdownCell_ShouldReturnCorrectMarkdown_OnSecondExecution()
    {
        var sut = Notebook.FromFile(TestContext.CurrentContext.TestDirectory + "/testData/sequentialReveal/after1.ipynb");

        var markdown = sut.RenderNextSequentialSectionFromPrecedingMarkdownCell("cell1");

        Assert.That(markdown, Is.Not.Null);
        Assert.That(markdown, Is.EqualTo("## A new Section\n\r\nA paragraph of text\n\r\n"));
    }

    [Test]
    public void RenderNextSequentialSectionFromPrecedingMarkdownCell_ShouldReturnCorrectMarkdown_OnThirdExecution()
    {
        var sut = Notebook.FromFile(TestContext.CurrentContext.TestDirectory + "/testData/sequentialReveal/after2.ipynb");

        var markdown = sut.RenderNextSequentialSectionFromPrecedingMarkdownCell("cell1");

        Assert.That(markdown, Is.Not.Null);
        Assert.That(markdown, Is.EqualTo("## A new Section\n\r\nA paragraph of text\n\r\n- bullet point one\n\r\n"));
    }

    [Test]
    public void RenderNextSequentialSectionFromPrecedingMarkdownCell_ShouldReturnCorrectMarkdown_OnFourthExecution()
    {
        var sut = Notebook.FromFile(TestContext.CurrentContext.TestDirectory + "/testData/sequentialReveal/after3.ipynb");

        var markdown = sut.RenderNextSequentialSectionFromPrecedingMarkdownCell("cell1");

        Assert.That(markdown, Is.Not.Null);
        Assert.That(markdown, Is.EqualTo("## A new Section\n\r\nA paragraph of text\n\r\n- bullet point one\n\r\n- bullet point two\n\r\n"));
    }

    [Test]
    public void RenderNextSequentialSectionFromPrecedingMarkdownCell_ShouldReturnCorrectMarkdown_OnFifthExecution()
    {
        var sut = Notebook.FromFile(TestContext.CurrentContext.TestDirectory + "/testData/sequentialReveal/after4.ipynb");

        var markdown = sut.RenderNextSequentialSectionFromPrecedingMarkdownCell("cell1");

        Assert.That(markdown, Is.Not.Null);
        Assert.That(markdown, Is.EqualTo("## A new Section\n\r\nA paragraph of text\n\r\n- bullet point one\n\r\n- bullet point two\n\r\n* bullet point three\n\r\n"));
    }    

    [Test]
    public void RenderNextSequentialSectionFromPrecedingMarkdownCell_ShouldReturnCorrectMarkdown_OnEighthExecution()
    {
        var sut = Notebook.FromFile(TestContext.CurrentContext.TestDirectory + "/testData/sequentialReveal/after7.ipynb");

        var markdown = sut.RenderNextSequentialSectionFromPrecedingMarkdownCell("cell1");

        Assert.That(markdown, Is.Not.Null);
        Assert.That(markdown, Is.EqualTo("## A new Section\n\r\nA paragraph of text\n\r\n- bullet point one\n\r\n- bullet point two\n\r\n* bullet point three\n\r\nAnother paragraph of text\n\r\n1. Bonus numeric list one\n\r\n2. Bonus numeric list two\n\r\n"
));
    }

    [Test]
    public void RenderNextSequentialSectionFromPrecedingMarkdownCell_ShouldReturnNull_WhenAllSectionsAreRendered()
    {
        var sut = Notebook.FromFile(TestContext.CurrentContext.TestDirectory + "/testData/sequentialReveal/after10.ipynb");

        var markdown = sut.RenderNextSequentialSectionFromPrecedingMarkdownCell("cell1");

        Assert.That(markdown, Is.EqualTo("## A new Section\n\r\nA paragraph of text\n\r\n- bullet point one\n\r\n- bullet point two\n\r\n* bullet point three\n\r\nAnother paragraph of text\n\r\n1. Bonus numeric list one\n\r\n2. Bonus numeric list two\n\r\n3. Bonus numeric list three\n\r\n[done]\n\r\n"));
    }

    // These tests require creation of a real kernel, tested manually for now.
    // [Test]
    // public void DisplayNextSequentialSectionFromPrecedingMarkdownCellUsingStyleCellTag_ShouldCompleteWithoutThrowing()
    // {        
    //     var sut = Notebook.FromFile(TestContext.CurrentContext.TestDirectory + "/testData/sequentialReveal/initial.ipynb");
    //     sut.DisplayNextSequentialSectionFromPrecedingMarkdownCellUsingStyleCellTag("cell1", "mdstyle");
    // }

    // [Test]
    // public void DisplayNextSequentialSectionFromPrecedingMarkdownCellUsingStyleFilePath_ShouldCompleteWithoutThrowing()
    // {   
    //     var styleFilePath = TestContext.CurrentContext.TestDirectory + "/testData/sequentialReveal/styleFile.css";
    //     var sut = Notebook.FromFile(TestContext.CurrentContext.TestDirectory + "/testData/sequentialReveal/initial.ipynb");
    //     sut.DisplayNextSequentialSectionFromPrecedingMarkdownCellUsingStyleFilePath("cell1", styleFilePath);
    // }

    // [Test]
    // public void DisplayNextSequentialSectionFromPrecedingMarkdownCellUsingStyleString_ShouldCompleteWithoutThrowing()
    // {   
    //     var styleString = """
    //     <style> 
    //     h1 {
    //     font-size: 5rem;
    //     }
    //     h2 {
    //     font-size: 4rem;
    //     }
    //     h3 {
    //     font-size: 3rem;
    //     }
    //     body, p, ul, ol {
    //     font-size: 2.5rem;
    //     }
    //     </style>
    //     """;

    //     var sut = Notebook.FromFile(TestContext.CurrentContext.TestDirectory + "/testData/sequentialReveal/initial.ipynb");
    //     sut.DisplayNextSequentialSectionFromPrecedingMarkdownCellUsingStyleString("cell1", styleString);
    // }
}
