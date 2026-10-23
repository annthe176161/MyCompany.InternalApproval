<#
.SYNOPSIS
    Automated E2E Test Suite Runner for Employee Profile & Tenure Module.

.DESCRIPTION
    Executes the 4-tier automated E2E test harness covering:
    - Tier 1: Feature Coverage (>=5 test cases per feature for isolated verification)
    - Tier 2: Boundary & Corner Cases (null JoinDate fallback, future dates, zero requests, leap years, multiple leaves)
    - Tier 3: Cross-Feature Interactions (updating JoinDate affects tenure in list, profile, and department average tenure; submitting leave updates timeline and yearly leave days)
    - Tier 4: Real-World Scenarios (complete employee lifecycle: onboarding, tenure milestones, submitting requests, approving leave, viewing HR analytics)

.PARAMETER Tier
    Specify tier to execute: '1', '2', '3', '4', or 'all' (default: 'all')

.PARAMETER ApiUrl
    Optional URL of running backend HTTP API (e.g. 'http://localhost:44305')

.PARAMETER DotnetTest
    Switch to also run the backend EF Core SQLite integration tests

.EXAMPLE
    .\test\e2e\run-e2e-tests.ps1 -Tier all
    .\test\e2e\run-e2e-tests.ps1 -Tier 1
    .\test\e2e\run-e2e-tests.ps1 -Tier all -DotnetTest
#>

[CmdletBinding()]
param(
    [ValidateSet('profile', 'hierarchical', 'all')]
    [string]$Module = 'all',

    [ValidateSet('1', '2', '3', '4', 'all')]
    [string]$Tier = 'all',

    [string]$ApiUrl = '',

    [switch]$DotnetTest
)

$ErrorActionPreference = 'Stop'
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptDir "..\..")

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "    EMPLOYEE PROFILE & TENURE: 4-TIER AUTOMATED E2E VERIFICATION RUNNER" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "Project Root: $ProjectRoot"
Write-Host "Target Tier : $Tier"
if ($ApiUrl) {
    Write-Host "API URL     : $ApiUrl"
}
Write-Host "Timestamp   : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss K')"
Write-Host "--------------------------------------------------------------------------------"

# Step 1: Verify Node.js prerequisite
try {
    $nodeVersion = node -v
    Write-Host "[Check] Node.js available: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Node.js runtime not found in PATH." -ForegroundColor Red
    exit 1
}

# Step 2: If DotnetTest requested, execute .NET EF Core integration tests
if ($DotnetTest) {
    Write-Host "`n>>> Running In-Process EF Core Integration Tests..." -ForegroundColor Yellow
    $efTestsProj = Join-Path $ProjectRoot "aspnet-core\test\MyCompany.InternalApproval.EntityFrameworkCore.Tests\MyCompany.InternalApproval.EntityFrameworkCore.Tests.csproj"
    if (Test-Path $efTestsProj) {
        & dotnet test $efTestsProj --no-build --logger "console;verbosity=normal"
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[ERROR] .NET Integration tests failed!" -ForegroundColor Red
            exit $LASTEXITCODE
        }
        Write-Host "[PASS] .NET Integration tests passed!" -ForegroundColor Green
    } else {
        Write-Host "[WARN] EF Core test project not found at: $efTestsProj" -ForegroundColor Yellow
    }
}

# Step 3: Execute Master E2E Runner
Write-Host "`n>>> Running 4-Tier Automated E2E Test Suite..." -ForegroundColor Yellow
$runnerScript = Join-Path $ScriptDir "e2e-runner.js"

$runnerArgs = @($runnerScript, "--module=$Module", "--tier=$Tier")
if ($ApiUrl) {
    $runnerArgs += "--api-url=$ApiUrl"
}

& node $runnerArgs
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    Write-Host "`n[SUCCESS] All E2E verification test suites PASSED with 100% success rate." -ForegroundColor Green
} else {
    Write-Host "`n[FAILURE] E2E verification tests encountered failures. Exit code: $exitCode" -ForegroundColor Red
}

exit $exitCode
