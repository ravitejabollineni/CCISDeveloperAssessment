# CCIS Developer Test 2026

Both assessment tasks implemented in a single .NET console application:

| Task | Description |
| ---- | ----------- |
| A | **UK Currency Change Calculator** — works out the change due for a purchase and breaks it down into UK notes and coins, largest first. |
| B | **Date Addition** — adds a number of days to a `d/M/yyyy` or `dd/MM/yyyy` date using manual integer arithmetic, with **no** built-in or third-party date/time functions. The result is always normalised to `dd/MM/yyyy`. |

Both tasks are reachable from the same menu in the same application.

## Solution structure

```
CCISDeveloperAssessment/
├── CCISDeveloperAssessment.sln                   Solution (classic format, opens in any VS 2022+)
├── CCISDeveloperAssessment/                      Console application
│   ├── CCISDeveloperAssessment.csproj
│   ├── Program.cs                                Entry point only
│   ├── ConsoleMenu.cs                            All console interaction
│   ├── Currency/                                 Task A
│   │   ├── ChangeCalculator.cs                   Greedy change algorithm (long pence) + supported range
│   │   ├── ChangeResult.cs                       Denomination, DenominationCount, ChangeResult
│   │   └── CurrencyInputParser.cs                Text -> decimal -> validated long pence
│   └── Dates/                                    Task B
│       ├── ManualDate.cs                         Immutable day/month/year value
│       ├── DateParser.cs                         Manual d/M/yyyy and dd/MM/yyyy parsing, day counts
│       └── DateCalculator.cs                     Leap years, month lengths, addition, formatting
├── CCISDeveloperAssessment.Tests/                xUnit tests for both tasks
│   ├── CCISDeveloperAssessment.Tests.csproj
│   ├── ChangeCalculatorTests.cs
│   ├── CurrencyInputParserTests.cs
│   ├── DateParserTests.cs
│   └── DateCalculatorTests.cs
├── README.md
└── .gitignore
```

Project relationships:

* `CCISDeveloperAssessment.Tests` references `CCISDeveloperAssessment`.
* The task logic lives in the `CCISDeveloperAssessment.Currency` and `CCISDeveloperAssessment.Dates`
  namespaces inside the console project. `Program.cs` and `ConsoleMenu.cs` contain no business logic:
  they only read input, call those namespaces and print the results.

## Prerequisites

* **.NET SDK 8.0 or later.** Developed and verified with SDK 10.0.303 building for `net8.0`.
* Target framework: **net8.0**, nullable reference types and implicit usings enabled.
* Solution file is the **classic `.sln` format**, so it opens in Visual Studio 2022, Rider, VS Code
  and any `dotnet` CLI from 8.0 onwards. (The newer XML `.slnx` format is deliberately not used:
  the CLI only understands it from SDK 9.0.200 onwards, which would silently break an SDK 8 build.)
* Test framework: xUnit. No other third-party packages are used.

## Restore, build, test and run

From the repository root:

```bash
# restore
dotnet restore CCISDeveloperAssessment.sln

# build
dotnet build CCISDeveloperAssessment.sln

# run the unit tests
dotnet test CCISDeveloperAssessment.sln

# run the application
dotnet run --project CCISDeveloperAssessment/CCISDeveloperAssessment.csproj
```

### From a clean checkout

```bash
git clone <repository-url>
cd CCISDeveloperAssessment
dotnet restore CCISDeveloperAssessment.sln
dotnet build CCISDeveloperAssessment.sln
dotnet test CCISDeveloperAssessment.sln
dotnet run --project CCISDeveloperAssessment/CCISDeveloperAssessment.csproj
```

## How the menu works

```
CCIS Developer Test 2026

1. UK Currency Change Calculator
2. Date Addition
0. Exit

Select an option:
```

* `1` runs Task A, `2` runs Task B, `0` exits.
* Any other entry prints an error and redisplays the menu.
* After a task finishes, the menu is shown again.
* Invalid input never crashes the application: the task prints an `Error: ...` line explaining
  what was wrong and control returns to the menu.
* If the input stream ends (end of file, `Ctrl+Z` on Windows, `Ctrl+D` on Linux/macOS, or piped
  input running out), the application stops instead of looping forever.



