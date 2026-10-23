#!/usr/bin/env node

/**
 * Master E2E Automated Test Runner
 * MyCompany.InternalApproval - 4-Tier Automated Verification Harness
 * 
 * Supports modules:
 * 1. Employee Profile & Tenure Module (R1-R4 Milestone 4)
 * 2. Hierarchical Authorization & Leave Statistics Module (R1-R3 Milestone 5)
 * 
 * Usage:
 *   node test/e2e/e2e-runner.js [--module=profile|hierarchical|all] [--tier=1|2|3|4|all] [--api-url=http://localhost:44305]
 */

const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');

const args = process.argv.slice(2);
let selectedModule = 'all';
let selectedTier = 'all';
let apiUrl = null;

for (const arg of args) {
  if (arg.startsWith('--tier=')) {
    selectedTier = arg.split('=')[1].toLowerCase();
  } else if (arg.startsWith('--module=')) {
    selectedModule = arg.split('=')[1].toLowerCase();
  } else if (arg.startsWith('--api-url=')) {
    apiUrl = arg.split('=')[1];
  }
}

const specsDir = path.join(__dirname, 'specs');

const moduleSuites = {
  profile: {
    name: 'Module 1: Employee Profile & Tenure',
    tiers: {
      '1': { file: path.join(specsDir, 'tier1-feature-coverage.test.js'), title: 'Tier 1: Feature Coverage (Profile & Tenure)' },
      '2': { file: path.join(specsDir, 'tier2-boundary-corner.test.js'), title: 'Tier 2: Boundary & Corner Cases (Profile & Tenure)' },
      '3': { file: path.join(specsDir, 'tier3-cross-interactions.test.js'), title: 'Tier 3: Cross-Feature Interactions (Profile & Tenure)' },
      '4': { file: path.join(specsDir, 'tier4-real-world-scenarios.test.js'), title: 'Tier 4: Real-World Scenarios (Profile & Tenure)' }
    }
  },
  hierarchical: {
    name: 'Module 2: Hierarchical Authorization & Leave Statistics',
    tiers: {
      '1': { file: path.join(specsDir, 'tier1-hierarchical-auth-leave-stats.test.js'), title: 'Tier 1: Feature Coverage (Hierarchical Auth & Leave Stats)' },
      '2': { file: path.join(specsDir, 'tier2-hierarchical-auth-boundary-corner.test.js'), title: 'Tier 2: Boundary & Corner (Hierarchical Auth & Leave Stats)' },
      '3': { file: path.join(specsDir, 'tier3-hierarchical-auth-cross-interactions.test.js'), title: 'Tier 3: Cross-Interactions (Hierarchical Auth & Leave Stats)' },
      '4': { file: path.join(specsDir, 'tier4-hierarchical-auth-real-world.test.js'), title: 'Tier 4: Real-World Scenarios (Hierarchical Auth & Leave Stats)' }
    }
  }
};

async function runTestFile(tierNumber, suiteTitle, filePath) {
  return new Promise((resolve) => {
    const startTime = Date.now();
    const child = spawn(process.execPath, ['--test', filePath], {
      stdio: ['inherit', 'pipe', 'pipe']
    });

    let stdout = '';
    let stderr = '';

    child.stdout.on('data', data => {
      stdout += data.toString();
    });

    child.stderr.on('data', data => {
      stderr += data.toString();
    });

    child.on('close', code => {
      const elapsed = Date.now() - startTime;
      
      const testsMatch = stdout.match(/# tests (\d+)/);
      const passMatch = stdout.match(/# pass (\d+)/);
      const failMatch = stdout.match(/# fail (\d+)/);

      const totalTests = testsMatch ? parseInt(testsMatch[1], 10) : 0;
      const passedTests = passMatch ? parseInt(passMatch[1], 10) : 0;
      const failedTests = failMatch ? parseInt(failMatch[1], 10) : 0;

      resolve({
        tier: tierNumber,
        title: suiteTitle,
        exitCode: code,
        passed: code === 0,
        total: totalTests,
        passedCount: passedTests,
        failedCount: failedTests,
        durationMs: elapsed,
        stdout,
        stderr
      });
    });
  });
}

async function main() {
  console.log('================================================================================');
  console.log('           AUTOMATED E2E TEST HARNESS: INTERNAL APPROVAL SYSTEM');
  console.log('================================================================================');
  console.log(`Target Module: ${selectedModule.toUpperCase()}`);
  console.log(`Target Tier  : ${selectedTier.toUpperCase()}`);
  if (apiUrl) console.log(`Target Live API: ${apiUrl}`);
  console.log(`Timestamp    : ${new Date().toISOString()}`);
  console.log('--------------------------------------------------------------------------------\n');

  const modulesToRun = selectedModule === 'all'
    ? ['profile', 'hierarchical']
    : [selectedModule];

  const tiersToRun = selectedTier === 'all'
    ? ['1', '2', '3', '4']
    : [selectedTier];

  const results = [];
  let allPassed = true;

  for (const modKey of modulesToRun) {
    const modConfig = moduleSuites[modKey];
    if (!modConfig) {
      console.error(`Unknown module: ${modKey}. Valid choices: profile, hierarchical, all`);
      process.exit(1);
    }

    console.log(`\n>>> [Module] ${modConfig.name}`);

    for (const tier of tiersToRun) {
      const tierConfig = modConfig.tiers[tier];
      if (!tierConfig) continue;

      if (!fs.existsSync(tierConfig.file)) {
        console.error(`Error: Spec file not found at ${tierConfig.file}`);
        process.exit(1);
      }

      process.stdout.write(`  Executing [Tier ${tier}] ${tierConfig.title}... `);
      const result = await runTestFile(tier, tierConfig.title, tierConfig.file);
      results.push({ ...result, module: modConfig.name });

      if (result.passed) {
        console.log(`\x1b[32mPASSED\x1b[0m (${result.passedCount}/${result.total} tests in ${result.durationMs}ms)`);
      } else {
        console.log(`\x1b[31mFAILED\x1b[0m (${result.failedCount} failures in ${result.durationMs}ms)`);
        allPassed = false;
        console.error(result.stderr || result.stdout);
      }
    }
  }

  console.log('\n================================================================================');
  console.log('                            TEST EXECUTION SUMMARY');
  console.log('================================================================================');
  console.log('Tier    Status    Passed/Total    Duration    Description');
  console.log('--------------------------------------------------------------------------------');

  let grandTotal = 0;
  let grandPassed = 0;
  let grandFailed = 0;
  let grandDuration = 0;

  for (const r of results) {
    grandTotal += r.total;
    grandPassed += r.passedCount;
    grandFailed += r.failedCount;
    grandDuration += r.durationMs;

    const statusBadge = r.passed ? '\x1b[32mPASS\x1b[0m' : '\x1b[31mFAIL\x1b[0m';
    const counts = `${r.passedCount}/${r.total}`.padEnd(14);
    const duration = `${r.durationMs}ms`.padEnd(11);
    console.log(`Tier ${r.tier}  ${statusBadge}      ${counts}  ${duration} ${r.title}`);
  }

  console.log('--------------------------------------------------------------------------------');
  console.log(`TOTAL   ${allPassed ? '\x1b[32mALL PASS\x1b[0m' : '\x1b[31mFAILURES\x1b[0m'}  ${grandPassed}/${grandTotal} passed`.padEnd(35) + `${grandDuration}ms total`);
  console.log('================================================================================\n');

  if (!allPassed) {
    console.error('E2E Test Execution finished with failures.');
    process.exit(1);
  } else {
    console.log(`E2E Test Execution completed successfully: 100% PASS RATE (${grandPassed}/${grandTotal} tests).`);
    process.exit(0);
  }
}

main().catch(err => {
  console.error('Fatal test runner error:', err);
  process.exit(1);
});
