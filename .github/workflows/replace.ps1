param(
  [Parameter(Mandatory=$true)]
  [string] $file,
  [Parameter(Mandatory=$true)]
  [string] $from,
  [Parameter(Mandatory=$true)]
  [string] $to
)

# Check if file exists
if (-not (Test-Path $file)) {
  Write-Error "Error: File '$file' not found!"
  exit 1
}

# Perform replacement using Select-String
Select-String -Path $file -Pattern $from -ReplacementValue $to -Quiet -Force | Set-Content -Path $file

Write-Host "Successfully replaced '$from' with '$to' in '$file'"
