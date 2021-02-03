﻿using Azure.Sdk.Tools.PerfAutomation.Models;
using Microsoft.Crank.Agent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Azure.Sdk.Tools.PerfAutomation
{
    static class Net
    {
        public static async Task<Result> RunAsync(string workingDirectory, bool debug,
            LanguageSettings languageSettings, string arguments, IDictionary<string, string> packageVersions)
        {
            var processArguments = $"run -c release -f netcoreapp2.1 -p {languageSettings.Project} -- " +
                $"{languageSettings.TestName} {arguments}";

            var result = await ProcessUtil.RunAsync(
                "dotnet",
                processArguments,
                workingDirectory: workingDirectory,
                log: true,
                captureOutput: true,
                captureError: true
            );

            /*
            === Warmup ===
            Current         Total           Average
            622025          622025          617437.38

            === Results ===
            Completed 622,025 operations in a weighted-average of 1.01s (617,437.38 ops/s, 0.000 s/op)

            === Test ===
            Current         Total           Average
            693696          693696          692328.31

            === Results ===
            Completed 693,696 operations in a weighted-average of 1.00s (692,328.31 ops/s, 0.000 s/op)
            */

            var match = Regex.Match(result.StandardOutput, @"\((.*) ops/s", RegexOptions.RightToLeft);
            var opsPerSecond = double.Parse(match.Groups[1].Value);

            return new Result
            {
                OperationsPerSecond = opsPerSecond,
                StandardError = result.StandardError,
                StandardOutput = result.StandardOutput,
            };
        }
    }
}
