$body = '{"emailOrUsername":"admin@acxiomcrm.com","password":"Admin@12345"}'
$login = Invoke-RestMethod -Uri "http://localhost:5123/api/auth/login" -Method Post -Body $body -ContentType "application/json"
Write-Host "API Login Success:" $login.success
Write-Host "Logged in Role:" $login.data.role
Write-Host "User:" $login.data.fullName

$token = $login.data.token
$headers = @{ "Authorization" = "Bearer $token" }

$customers = Invoke-RestMethod -Uri "http://localhost:5123/api/customers" -Method Get -Headers $headers
Write-Host "Customers Count:" $customers.data.Count
Write-Host "Customer 1:" $customers.data[0].customerName "Code:" $customers.data[0].customerCode

$leads = Invoke-RestMethod -Uri "http://localhost:5123/api/leads" -Method Get -Headers $headers
Write-Host "Leads Count:" $leads.data.Count
Write-Host "Lead 1:" $leads.data[0].leadName "Status:" $leads.data[0].status

$opps = Invoke-RestMethod -Uri "http://localhost:5123/api/opportunities" -Method Get -Headers $headers
Write-Host "Opportunities Count:" $opps.data.Count
Write-Host "Opportunity 1:" $opps.data[0].opportunityName "Amount: $" $opps.data[0].amount

$pipeline = Invoke-RestMethod -Uri "http://localhost:5123/api/reports/pipeline" -Method Get -Headers $headers
Write-Host "Pipeline Total: $" $pipeline.data.totalPipelineValue "Weighted: $" $pipeline.data.weightedPipelineValue
