#!/bin/bash

# Test script for the verify-readme CI check
# This script demonstrates how to use the new CI checks functionality

echo "=== Azure SDK CLI - CI Checks Test ==="
echo

# Check if we're in the right directory
if [ ! -f "Azure.Sdk.Tools.Cli.csproj" ]; then
    echo "Error: Please run this script from the Azure.Sdk.Tools.Cli directory"
    exit 1
fi

echo "1. Building the project..."
# Note: This would require dotnet CLI to be available
# dotnet build

echo "2. Testing CI checks list command..."
echo "Command: azsdk ci list"
echo "Expected output: List of available CI checks including 'verify-readme'"
echo

echo "3. Testing verify-readme check..."
echo "Command: azsdk ci run --checks verify-readme --path /path/to/repo --verbose"
echo "Expected behavior:"
echo "  - Looks for Verify-Readme.ps1 script in eng/common/scripts/"
echo "  - Looks for .docsettings.yml configuration file"
echo "  - Scans common SDK directories (sdk/, docs/, src/, etc.)"
echo "  - Runs PowerShell script with appropriate parameters"
echo "  - Parses output and returns structured results"
echo

echo "4. Example usage scenarios:"
echo

echo "Run all CI checks:"
echo "  azsdk ci run --path /path/to/azure-sdk-for-python"
echo

echo "Run only README verification:"
echo "  azsdk ci run --checks verify-readme --path /path/to/repo"
echo

echo "Run with verbose output:"
echo "  azsdk ci run --checks verify-readme --verbose"
echo

echo "List available checks:"
echo "  azsdk ci list"
echo

echo "=== Implementation Details ==="
echo
echo "Files created/modified:"
echo "  - Tools/CIChecks/Checks/VerifyReadmeCheck.cs (new)"
echo "  - Tools/CIChecks/CICheckRunner.cs (modified)"
echo "  - Tools/CIChecks/CIChecksTool.cs (existed)"
echo "  - Tools/CIChecks/ICICheck.cs (existed)"
echo "  - Services/ServiceRegistrations.cs (modified)"
echo "  - Commands/SharedCommandGroups.cs (CIChecks group existed)"
echo

echo "Key features:"
echo "  ✓ Automatic script detection (eng/common/scripts/Verify-Readme.ps1)"
echo "  ✓ Flexible settings file location (.docsettings.yml)"
echo "  ✓ Smart directory scanning (sdk/, docs/, src/, etc.)"
echo "  ✓ PowerShell execution with proper error handling"
echo "  ✓ Structured output parsing from doc-warden"
echo "  ✓ Comprehensive error reporting"
echo "  ✓ Integration with existing CLI framework"
echo

echo "=== Next Steps ==="
echo "1. Test with actual dotnet build"
echo "2. Run against a real Azure SDK repository"
echo "3. Verify PowerShell script execution"
echo "4. Test error scenarios (missing files, etc.)"
echo "5. Add unit tests for the VerifyReadmeCheck class"
echo "6. Consider adding more CI checks (e.g., changelog, license, etc.)"
