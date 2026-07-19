# How to run tests

## CLI / batchmode

```
Unity.exe -batchmode -projectPath "D:\All for codding\Homework general\Knight-Adventure-2D-Tests" -runTests -testPlatform PlayMode -testResults "TestResults/playmode-results.xml" -logFile "Logs/playmode.log"
```

Same for EditMode — swap `-testPlatform EditMode` and use a different `-testResults`/`-logFile` path so runs don't overwrite each other.

**Do not add `-quit`.** Combining `-quit` with `-runTests` is a known Unity race condition: the Editor process can exit before the test runner finishes writing results, silently leaving the `-testResults` file from a previous run in place with no error printed anywhere. The test runner already terminates the process itself once the run completes, so `-quit` is redundant and actively harmful here.

After any run, check the `<test-run ... start-time="...">` timestamp in the resulting XML against the current time before trusting it — otherwise it's easy to mistake yesterday's results for a fresh run (this happened once while adding `PlayerTests.cs`: the `-quit` race left a stale one-day-old results file with the new tests silently missing from the count).

## Where to look

- `TestResults/*.xml` — NUnit-compatible XML; check `testcasecount`, `passed`, `failed` on the root `<test-run>` element.
- `Logs/*.log` — full Editor log: script compilation, errors, per-test-case output (also partially duplicated in the XML's `<output>` nodes).

## Licensing

A batchmode run only gets past `[Licensing::Module]` if Unity Personal has already been activated interactively on the machine at least once. Behavior on a clean/CI machine without a prior interactive activation hasn't been verified in this repo.

## Via the Editor (Test Runner window)

`Window → General → Test Runner` → PlayMode or EditMode tab → Run All. More convenient for local iteration since you see live progress and can re-run individual tests.
