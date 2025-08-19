using System.Text.Json.Serialization;

namespace Azure.Sdk.Tools.Cli.Models;

/// <summary>
/// Base class for CLI check responses with exit code and output.
/// </summary>
public class CLICheckResponse: Response
{
    [JsonPropertyName("exit_code")]
    public int ExitCode { get; set;}
    
    [JsonPropertyName("check_status_details")]
    public string CheckStatusDetails { get; set;}

    /// <summary>
    /// Suggested next steps or actions to resolve the error.
    /// </summary>
    [JsonPropertyName("next_steps")]
    public string? NextSteps { get; set; }

    public CLICheckResponse() { }


    public CLICheckResponse(int exitCode, string checkStatusDetails, string error = null, string? nextSteps = null)
    {
        ExitCode = exitCode;
        CheckStatusDetails = checkStatusDetails;
        if (!string.IsNullOrEmpty(error))
        {
            ResponseError = error;
        }
        if (!string.IsNullOrEmpty(nextSteps))
        {
            NextSteps = nextSteps;
        }
    }

    public override string ToString()
    {
        var output = ToString(CheckStatusDetails);
        if (!string.IsNullOrWhiteSpace(NextSteps))
        {
            output += System.Environment.NewLine + "Next Steps:" + System.Environment.NewLine + NextSteps;
        }
        return output;
    }
}

/// <summary>
/// CLI check response for cookbook/documentation reference responses.
/// </summary>
public class CookbookCLICheckResponse : CLICheckResponse
{
    [JsonPropertyName("cookbook_reference")]
    public string CookbookReference { get; set;}


    public CookbookCLICheckResponse(int exitCode, string checkStatusDetails, string cookbookReference, string? nextSteps = null)
        : base(exitCode, checkStatusDetails, null, nextSteps)
    {
        CookbookReference = cookbookReference;
    }

    public override string ToString()
    {
        var output = ToString(CheckStatusDetails);
        if (!string.IsNullOrWhiteSpace(NextSteps))
        {
            output += System.Environment.NewLine + "Next Steps:" + System.Environment.NewLine + NextSteps;
        }
        return output;
    }
}

