param(
    [string]$PythonPath = $env:CODEX_PYTHON
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($PythonPath)) {
    $bundledPython = Join-Path $HOME ".cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe"
    if (Test-Path -LiteralPath $bundledPython) {
        $PythonPath = $bundledPython
    } else {
        $PythonPath = (Get-Command python -ErrorAction Stop).Source
    }
}

if (-not (Test-Path -LiteralPath $PythonPath)) {
    throw "Python executable was not found: $PythonPath"
}

Write-Host "[1/3] Runtime build and tests"
& (Join-Path $PSScriptRoot "test-runtime.ps1")
if ($LASTEXITCODE -ne 0) { throw "Runtime tests failed." }

Write-Host "[2/3] Controlled-summary consistency"
& $PythonPath (Join-Path $PSScriptRoot "validate-controlled-summaries.py")
if ($LASTEXITCODE -ne 0) { throw "Controlled-summary validation failed." }

Write-Host "[3/3] Markdown index integrity"
$indexValidator = Get-ChildItem -Path $root -Recurse -Filter "validate_index.py" |
    Where-Object { $_.FullName -notmatch '[\\/](bin|obj|_archive)[\\/]' } |
    Select-Object -First 1
if ($null -eq $indexValidator) { throw "Markdown index validator was not found." }
& $PythonPath $indexValidator.FullName
if ($LASTEXITCODE -ne 0) { throw "Markdown index validation failed." }

Write-Host "PROJECT VALIDATION PASSED"
