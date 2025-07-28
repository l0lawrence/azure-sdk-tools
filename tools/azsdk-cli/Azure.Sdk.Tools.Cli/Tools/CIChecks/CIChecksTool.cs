// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Sdk.Tools.Cli.Commands;
using Azure.Sdk.Tools.Cli.Contract;
using Azure.Sdk.Tools.Cli.Services;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel;

namespace Azure.Sdk.Tools.Cli.Tools.CIChecks
{
    [McpServerToolType, Description("Runs common CI checks and validations")]
    public class CIChecksTool : MCPTool
    {
        private readonly ICICheckRunner checkRunner;
        private readonly IOutputService output;
        private readonly ILogger<CIChecksTool> logger;

        // Options
        private readonly Option<string> pathOpt = new(["--path", "-p"], () => Directory.GetCurrentDirectory(), "Root path to run checks against");
        private readonly Option<string[]> checksOpt = new(["--checks", "-c"], "Specific checks to run (comma-separated). If not specified, all checks will run");
        private readonly Option<bool> listOpt = new(["--list", "-l"], () => false, "List available checks");
        private readonly Option<bool> verboseOpt = new(["--verbose", "-v"], () => false, "Enable verbose output");

        public CIChecksTool(
            ICICheckRunner checkRunner,
            IOutputService output,
            ILogger<CIChecksTool> logger
        ) : base()
        {
            this.checkRunner = checkRunner;
            this.output = output;
            this.logger = logger;

            CommandHierarchy =
            [
                SharedCommandGroups.CIChecks
            ];
        }

        public override Command GetCommand()
        {
            var runCommand = new Command("run", "Run CI checks")
            {
                pathOpt, checksOpt, verboseOpt
            };
            runCommand.SetHandler(async ctx => { await HandleRunCommand(ctx, ctx.GetCancellationToken()); });

            var listCommand = new Command("list", "List available CI checks")
            {
                verboseOpt
            };
            listCommand.SetHandler(async ctx => { await HandleListCommand(ctx, ctx.GetCancellationToken()); });

            var ciCommand = new Command("checks", "CI checks and validations")
            {
                runCommand, listCommand
            };

            return ciCommand;
        }

        public override async Task HandleCommand(InvocationContext ctx, CancellationToken ct)
        {
            // This is handled by sub-commands
            await Task.CompletedTask;
        }

        private async Task HandleRunCommand(InvocationContext ctx, CancellationToken ct)
        {
            var path = ctx.ParseResult.GetValueForOption(pathOpt)!;
            var checks = ctx.ParseResult.GetValueForOption(checksOpt) ?? [];
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);

            if (verbose)
            {
                logger.LogInformation("Running CI checks with path: {Path}, checks: {Checks}", path, string.Join(", ", checks));
            }

            if (!Directory.Exists(path))
            {
                logger.LogError("Path does not exist: {Path}", path);
                ctx.ExitCode = 1;
                return;
            }

            try
            {
                var result = await checkRunner.RunChecksAsync(path, checks, ct);
                
                if (verbose)
                {
                    logger.LogInformation("Checks completed. Status: {Status}, Issues: {Issues}, Execution time: {Time}ms",
                        result.OverallStatus, result.TotalIssues, result.ExecutionTime.TotalMilliseconds);
                }

                output.Output(result);

                // Set exit code based on result
                ctx.ExitCode = result.OverallStatus switch
                {
                    Models.Responses.CICheckStatus.Pass => 0,
                    Models.Responses.CICheckStatus.Warning => 0, // Warnings don't fail the build
                    Models.Responses.CICheckStatus.Fail => 1,
                    Models.Responses.CICheckStatus.Error => 1,
                    _ => 0
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error running CI checks");
                ctx.ExitCode = 1;
            }
        }

        private async Task HandleListCommand(InvocationContext ctx, CancellationToken ct)
        {
            var verbose = ctx.ParseResult.GetValueForOption(verboseOpt);

            var availableChecks = checkRunner.AvailableChecks;
            
            if (verbose)
            {
                logger.LogInformation("Available CI checks ({Count}):", availableChecks.Count);
                foreach (var check in availableChecks)
                {
                    logger.LogInformation("  {Name}: {Description}", check.Name, check.Description);
                }
            }

            var listResponse = new
            {
                availableChecks = availableChecks.Select(c => new
                {
                    name = c.Name,
                    description = c.Description
                }).ToList(),
                totalCount = availableChecks.Count
            };

            output.Output(listResponse);
            await Task.CompletedTask;
        }
    }
}
