# CI Checks - Verify README

This document describes the `verify-readme` CI check that validates README files in SDK repositories using the doc-warden tool.

## Overview

The `verify-readme` check runs the `Verify-Readme.ps1` script from the eng/common/scripts directory to validate README files across the repository. This check ensures that README files follow the required format and contain necessary sections.

## Usage

### Run the verify-readme check specifically:
```bash
azsdk ci run --checks verify-readme --path /path/to/repo --verbose
```

### Run all CI checks (including verify-readme):
```bash
azsdk ci run --path /path/to/repo
```

### List available checks:
```bash
azsdk ci list
```

## Requirements

The check requires the following to be present in the repository:

1. **Verify-Readme.ps1 script**: Located in one of these paths:
   - `eng/common/scripts/Verify-Readme.ps1`
   - `eng/scripts/Verify-Readme.ps1`
   - `scripts/Verify-Readme.ps1`

2. **Settings file**: A doc-warden settings file in one of these locations:
   - `.docsettings.yml`
   - `.docsettings.yaml`
   - `eng/.docsettings.yml`
   - `eng/.docsettings.yaml`
   - `eng/common/.docsettings.yml`
   - `eng/common/.docsettings.yaml`

3. **PowerShell Core**: The check requires `pwsh` to be available in the system PATH.

## Scan Paths

The check automatically determines which directories to scan by looking for common SDK directories in this order:

1. `sdk/`
2. `docs/`
3. `src/`
4. `lib/`
5. `packages/`
6. `tools/`

If none of these directories exist, it will scan the root directory.

## Output

The check provides detailed output including:

- **Overall status**: Pass, Fail, Warning, Error, or Skipped
- **Files checked**: Number of README files processed
- **Issues found**: Count of validation errors
- **Detailed error messages**: File-specific issues with line numbers when available

### Example Output

```json
{
  "checkName": "verify-readme",
  "status": "Fail",
  "summary": "README verification failed with 2 issues",
  "filesChecked": 15,
  "issuesFound": 2,
  "details": [
    {
      "file": "sdk/example/README.md",
      "line": 25,
      "severity": "Error",
      "message": "Missing required section: 'Getting Started'",
      "rule": "doc-warden"
    },
    {
      "file": "sdk/another/README.md",
      "line": 10,
      "severity": "Warning",
      "message": "Section heading format should use title case",
      "rule": "doc-warden"
    }
  ]
}
```

## Error Handling

The check handles various error scenarios:

- **Script not found**: Returns error status with appropriate message
- **Settings file missing**: Returns error status with helpful guidance
- **PowerShell execution errors**: Captures and reports script execution issues
- **Permission issues**: Reports file access problems
- **Network issues**: Handles pip installation failures for doc-warden

## Integration with GitHub Actions

This check can be integrated into GitHub Actions workflows:

```yaml
- name: Run README verification
  run: |
    azsdk ci run --checks verify-readme --verbose
  env:
    CI: true
```

## Troubleshooting

### Common Issues

1. **"Script not found" error**: Ensure the repository has the Verify-Readme.ps1 script in one of the expected locations.

2. **"Settings file missing" error**: Create a `.docsettings.yml` file with appropriate doc-warden configuration.

3. **"PowerShell not found" error**: Install PowerShell Core (pwsh) on the system.

4. **pip installation failures**: Ensure Python and pip are available and have internet connectivity for installing doc-warden.

### Debug Mode

Use the `--verbose` flag to see detailed execution information:

```bash
azsdk ci run --checks verify-readme --verbose
```

This will show:
- Script path resolution
- Settings file location
- Scan paths determined
- PowerShell command executed
- Raw output from doc-warden
