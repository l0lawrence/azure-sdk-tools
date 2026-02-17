// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Azure.Sdk.Tools.Cli.Helpers;
using Azure.Sdk.Tools.Cli.Services;
using Azure.Sdk.Tools.Cli.Services.Languages;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Azure.Sdk.Tools.Cli.Tests.Services.Languages;

[TestFixture]
internal class PythonLanguageSpecificChecksTests
{
    private Mock<IProcessHelper> _processHelperMock = null!;
    private Mock<IPythonHelper> _pythonHelperMock = null!;
    private Mock<INpxHelper> _npxHelperMock = null!;
    private Mock<IGitHelper> _gitHelperMock = null!;
    private Mock<ICommonValidationHelpers> _commonValidationHelpersMock = null!;
    private PythonLanguageService _languageService = null!;
    private string _packagePath = null!;

    [SetUp]
    public void SetUp()
    {
        _processHelperMock = new Mock<IProcessHelper>();
        _pythonHelperMock = new Mock<IPythonHelper>();
        _npxHelperMock = new Mock<INpxHelper>();
        _gitHelperMock = new Mock<IGitHelper>();
        _gitHelperMock.Setup(g => g.GetRepoName(It.IsAny<string>())).Returns("azure-sdk-for-python");
        _commonValidationHelpersMock = new Mock<ICommonValidationHelpers>();

        _languageService = new PythonLanguageService(
            _processHelperMock.Object,
            _pythonHelperMock.Object,
            _npxHelperMock.Object,
            _gitHelperMock.Object,
            NullLogger<LanguageService>.Instance,
            _commonValidationHelpersMock.Object,
            Mock.Of<IFileHelper>(),
            Mock.Of<ISpecGenSdkConfigHelper>());

        _packagePath = "/tmp/python-package";
    }

    #region AnalyzeDependencies Tests

    [Test]
    public async Task AnalyzeDependencies_ReturnsSuccess_WhenProcessCompletes()
    {
        // Arrange
        var processResult = new ProcessResult { ExitCode = 0 };
        processResult.AppendStdout("All minimum dependencies are compatible");

        PythonOptions? capturedOptions = null;
        _pythonHelperMock
            .Setup(p => p.Run(It.IsAny<PythonOptions>(), It.IsAny<CancellationToken>()))
            .Callback<PythonOptions, CancellationToken>((options, _) => capturedOptions = options)
            .ReturnsAsync(processResult);

        // Act
        var response = await _languageService.AnalyzeDependencies(_packagePath, false, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.ExitCode, Is.EqualTo(0));
            Assert.That(response.CheckStatusDetails, Does.Contain("Dependency analysis completed successfully"));
            Assert.That(response.ResponseError, Is.Null);
        });

        // Verify correct command was called
        Assert.That(capturedOptions, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedOptions!.WorkingDirectory, Is.EqualTo(_packagePath));
            Assert.That(capturedOptions.Timeout, Is.EqualTo(TimeSpan.FromMinutes(5)));
        });

        _pythonHelperMock.Verify(p => p.Run(
            It.Is<PythonOptions>(opts => 
                opts.Args.Contains("mindependency") && 
                opts.Args.Contains("--isolate") && 
                opts.Args.Contains(_packagePath) &&
                opts.WorkingDirectory == _packagePath &&
                opts.Timeout == TimeSpan.FromMinutes(5)), 
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Test]
    public async Task AnalyzeDependencies_ReturnsFailure_WhenDependencyIssuesFound()
    {
        // Arrange
        var processResult = new ProcessResult { ExitCode = 1 };
        processResult.AppendStderr("ERROR: Incompatible dependency versions found");

        _pythonHelperMock
            .Setup(p => p.Run(It.IsAny<PythonOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(processResult);

        // Act
        var response = await _languageService.AnalyzeDependencies(_packagePath, false, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.ExitCode, Is.EqualTo(1));
            Assert.That(response.CheckStatusDetails, Does.Contain("ERROR: Incompatible dependency versions found"));
            Assert.That(response.ResponseError, Does.Contain("Dependency analysis found issues"));
        });

        _pythonHelperMock.Verify(p => p.Run(It.IsAny<PythonOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AnalyzeDependencies_ReturnsErrorResponse_WhenProcessThrows()
    {
        // Arrange
        _pythonHelperMock
            .Setup(p => p.Run(It.IsAny<PythonOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("azpysdk not found"));

        // Act
        var response = await _languageService.AnalyzeDependencies(_packagePath, false, CancellationToken.None);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(response.ExitCode, Is.EqualTo(1));
            Assert.That(response.CheckStatusDetails, Is.EqualTo(string.Empty));
            Assert.That(response.ResponseError, Does.Contain("Error running dependency analysis: azpysdk not found"));
        });

        _pythonHelperMock.Verify(p => p.Run(It.IsAny<PythonOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AnalyzeDependencies_PassesCorrectExecutableAndArguments()
    {
        // Arrange
        var processResult = new ProcessResult { ExitCode = 0 };
        processResult.AppendStdout("Success");

        PythonOptions? capturedOptions = null;
        _pythonHelperMock
            .Setup(p => p.Run(It.IsAny<PythonOptions>(), It.IsAny<CancellationToken>()))
            .Callback<PythonOptions, CancellationToken>((options, _) => capturedOptions = options)
            .ReturnsAsync(processResult);

        // Act
        await _languageService.AnalyzeDependencies(_packagePath, false, CancellationToken.None);

        // Assert
        Assert.That(capturedOptions, Is.Not.Null);
        Assert.Multiple(() =>
        {
            // Verify azpysdk executable is used
            Assert.That(capturedOptions!.ExecutableName, Is.EqualTo("azpysdk"));
            
            // Verify command arguments are correct
            Assert.That(capturedOptions.Args, Does.Contain("mindependency"));
            Assert.That(capturedOptions.Args, Does.Contain("--isolate"));
            Assert.That(capturedOptions.Args, Does.Contain(_packagePath));
            
            // Verify working directory and timeout
            Assert.That(capturedOptions.WorkingDirectory, Is.EqualTo(_packagePath));
            Assert.That(capturedOptions.Timeout, Is.EqualTo(TimeSpan.FromMinutes(5)));
        });
    }

    #endregion
}
