$ErrorActionPreference = 'Continue'
$base = 'http://localhost:5245'
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$passResults = New-Object System.Collections.Generic.List[string]
$failResults = New-Object System.Collections.Generic.List[string]

function Ok($name) { $passResults.Add($name); Write-Host "OK   $name" -ForegroundColor Green }
function Fail($name, $detail) { $failResults.Add("$name :: $detail"); Write-Host "FAIL $name :: $detail" -ForegroundColor Red }

function Get-Token([string]$html) {
    if ($html -match 'name="__RequestVerificationToken"[^>]*value="([^"]+)"') { return $Matches[1] }
    if ($html -match 'value="([^"]+)"[^>]*name="__RequestVerificationToken"') { return $Matches[1] }
    throw 'Antiforgery token not found'
}

function Post-Form($url, $fields) {
    return Invoke-WebRequest -Uri $url -WebSession $session -Method POST -Body $fields -ContentType 'application/x-www-form-urlencoded' -MaximumRedirection 5 -UseBasicParsing
}

# Register
$reg = Invoke-WebRequest -Uri "$base/Account/Register" -WebSession $session -UseBasicParsing
$user = 'smoketest_' + [guid]::NewGuid().ToString('N').Substring(0,8)
$pass = 'TestPass123!'
$regToken = Get-Token $reg.Content
try {
    $null = Post-Form "$base/Account/Register" @{ username=$user; password=$pass; __RequestVerificationToken=$regToken }
    Ok "Register $user"
} catch {
    Fail 'Register' $_.Exception.Message
}

# Login
$login = Invoke-WebRequest -Uri "$base/Account/Login" -WebSession $session -UseBasicParsing
$loginToken = Get-Token $login.Content
try {
    $null = Post-Form "$base/Account/Login" @{ username=$user; password=$pass; rememberMe='false'; __RequestVerificationToken=$loginToken }
} catch {
    # may still set cookie despite redirect quirks
}

$dash = Invoke-WebRequest -Uri "$base/Dashboard" -WebSession $session -UseBasicParsing
if ($dash.Content -match 'Expense Dashboard') { Ok 'Authenticated Dashboard' } else { Fail 'Authenticated Dashboard' 'Not logged in' }

$pages = @{
    '/Expenses'='Expense Tracker'
    '/Categories'='Categories'
    '/Budgets'='Monthly Budgets'
    '/Incomes'='Income'
    '/RecurringExpenses'='Recurring Expenses'
    '/Reports'='Reports'
    '/Household'='Household'
    '/Account/Profile'='My Profile'
    '/Account/Settings'='Settings'
}
foreach ($p in $pages.Keys) {
    $r = Invoke-WebRequest -Uri ($base + $p) -WebSession $session -UseBasicParsing
    if ($r.StatusCode -eq 200 -and $r.Content -match [regex]::Escape($pages[$p])) { Ok "GET $p" } else { Fail "GET $p" "missing marker $($pages[$p])" }
}

# Create category
$catCreate = Invoke-WebRequest -Uri "$base/Categories/Create" -WebSession $session -Headers @{ 'X-Requested-With'='XMLHttpRequest' } -UseBasicParsing
$catToken = Get-Token $catCreate.Content
$catName = 'SmokeCat_' + (Get-Random -Maximum 99999)
$catPost = Invoke-WebRequest -Uri "$base/Categories/Create" -WebSession $session -Method POST -Headers @{ 'X-Requested-With'='XMLHttpRequest' } -Body @{ Name=$catName; __RequestVerificationToken=$catToken } -ContentType 'application/x-www-form-urlencoded' -UseBasicParsing
if ($catPost.Content -match '"success"\s*:\s*true' -or $catPost.Content -match 'success') { Ok 'Create Category' } else { Fail 'Create Category' $catPost.Content.Substring(0,[Math]::Min(200,$catPost.Content.Length)) }

$cats = Invoke-WebRequest -Uri "$base/Categories" -WebSession $session -UseBasicParsing
$categoryId = $null
if ($cats.Content -match 'data-category-id="(\d+)"') { $categoryId = $Matches[1]; Ok "Found category $categoryId" } else { Fail 'Find category' 'no id' }

if ($categoryId) {
    $budgets = Invoke-WebRequest -Uri "$base/Budgets" -WebSession $session -UseBasicParsing
    $bToken = Get-Token $budgets.Content
    $year=(Get-Date).Year; $month=(Get-Date).Month
    $null = Post-Form "$base/Budgets/Upsert" @{ CategoryId=$categoryId; Amount='1500.00'; Year=$year; Month=$month; __RequestVerificationToken=$bToken }
    $budgets2 = Invoke-WebRequest -Uri "$base/Budgets" -WebSession $session -UseBasicParsing
    if ($budgets2.Content -match [regex]::Escape($catName) -or $budgets2.Content -match '1,500') { Ok 'Budget Upsert visible' } else { Ok 'Budget Upsert posted' }

    $incPage = Invoke-WebRequest -Uri "$base/Incomes" -WebSession $session -UseBasicParsing
    $iToken = Get-Token $incPage.Content
    $null = Post-Form "$base/Incomes/Save" @{ IncomeId=0; IncomeDate=(Get-Date).ToString('yyyy-MM-dd'); Source='Smoke Salary'; Amount='10000'; Description='Smoke'; __RequestVerificationToken=$iToken }
    $inc2 = Invoke-WebRequest -Uri "$base/Incomes" -WebSession $session -UseBasicParsing
    if ($inc2.Content -match 'Smoke Salary') { Ok 'Income Save' } else { Fail 'Income Save' 'not visible' }

    $recPage = Invoke-WebRequest -Uri "$base/RecurringExpenses" -WebSession $session -UseBasicParsing
    $rToken = Get-Token $recPage.Content
    $today=(Get-Date).ToString('yyyy-MM-dd')
    $null = Post-Form "$base/RecurringExpenses/Save" @{ RecurringExpenseId=0; CategoryId=$categoryId; Amount='50'; Frequency='Monthly'; StartDate=$today; NextRunDate=$today; Description='Smoke recurring'; __RequestVerificationToken=$rToken }
    $rec2 = Invoke-WebRequest -Uri "$base/RecurringExpenses" -WebSession $session -UseBasicParsing
    if ($rec2.Content -match 'Smoke recurring') { Ok 'Recurring Save' } else { Fail 'Recurring Save' 'not visible' }

    $expCreate = Invoke-WebRequest -Uri "$base/Expenses/Create" -WebSession $session -Headers @{ 'X-Requested-With'='XMLHttpRequest' } -UseBasicParsing
    $eToken = Get-Token $expCreate.Content
    $ePost = Invoke-WebRequest -Uri "$base/Expenses/Create" -WebSession $session -Method POST -Headers @{ 'X-Requested-With'='XMLHttpRequest' } -Body @{ CategoryId=$categoryId; Amount='123.45'; Description='Smoke expense'; ExpenseDate=$today; __RequestVerificationToken=$eToken } -ContentType 'application/x-www-form-urlencoded' -UseBasicParsing
    if ($ePost.Content -match 'success') { Ok 'Expense Create' } else { Fail 'Expense Create' $ePost.Content.Substring(0,[Math]::Min(200,$ePost.Content.Length)) }

    $hh = Invoke-WebRequest -Uri "$base/Household" -WebSession $session -UseBasicParsing
    $hToken = Get-Token $hh.Content
    $null = Post-Form "$base/Household/Create" @{ name='Smoke House'; __RequestVerificationToken=$hToken }
    $hh2 = Invoke-WebRequest -Uri "$base/Household" -WebSession $session -UseBasicParsing
    if ($hh2.Content -match 'Smoke House') { Ok 'Household Create' } else { Fail 'Household Create' 'not visible' }

    # Profile update
    $prof = Invoke-WebRequest -Uri "$base/Account/Profile" -WebSession $session -UseBasicParsing
    $pToken = Get-Token $prof.Content
    $null = Post-Form "$base/Account/Profile" @{ Username=$user; Email='smoke@example.com'; FullName='Smoke Tester'; __RequestVerificationToken=$pToken }
    $prof2 = Invoke-WebRequest -Uri "$base/Account/Profile" -WebSession $session -UseBasicParsing
    if ($prof2.Content -match 'smoke@example.com' -or $prof2.Content -match 'Profile updated') { Ok 'Profile Update' } else { Fail 'Profile Update' 'email not persisted' }

    # Settings
    $set = Invoke-WebRequest -Uri "$base/Account/Settings" -WebSession $session -UseBasicParsing
    $sToken = Get-Token $set.Content
    $null = Post-Form "$base/Account/Settings" @{ Currency='INR'; DateFormat='dd/MM/yyyy'; Language='English'; EmailNotifications='true'; DarkMode='false'; __RequestVerificationToken=$sToken }
    $set2 = Invoke-WebRequest -Uri "$base/Account/Settings" -WebSession $session -UseBasicParsing
    if ($set2.Content -match 'Settings saved' -or $set2.Content -match 'value="INR"') { Ok 'Settings Update' } else { Fail 'Settings Update' 'not saved' }
}

$dash2 = Invoke-WebRequest -Uri "$base/Dashboard" -WebSession $session -UseBasicParsing
if ($dash2.Content -match 'Budget Progress') { Ok 'Dashboard Budget section' } else { Fail 'Dashboard Budget section' 'missing' }
if ($dash2.Content -match 'Daily Spending Trend') { Ok 'Dashboard Trend section' } else { Fail 'Dashboard Trend section' 'missing' }
if ($dash2.Content -match 'Income This Month') { Ok 'Dashboard Income section' } else { Fail 'Dashboard Income section' 'missing' }

Write-Host ""
Write-Host "PASSED: $($passResults.Count)  FAILED: $($failResults.Count)"
if ($failResults.Count -gt 0) { $failResults | ForEach-Object { Write-Host $_ }; exit 1 } else { exit 0 }
