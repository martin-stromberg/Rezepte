#Requires -Version 7.0
<#
.SYNOPSIS
    Shared TRX helpers for the test scripts (imported via dot-sourcing).

.DESCRIPTION
    Encapsulates parsing of the VSTest TRX format (namespace, UnitTestResult nodes,
    Times attributes) so XPath and namespace details are not maintained in every
    script. Usage in the calling script:

        . (Join-Path $PSScriptRoot "lib/Trx.ps1")
#>

$script:TrxNamespaceUri = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"

function Get-TrxDocument {
    # Loads a TRX file as an XML document; $null for a missing/unreadable file.
    # Returned via the "," operator: XmlDocument implements IEnumerable over its
    # child nodes and would otherwise be unrolled into the function output.
    param([string]$TrxPath)
    if (-not (Test-Path $TrxPath)) { return $null }
    try {
        [xml]$trx = Get-Content $TrxPath -Raw
        return ,$trx
    }
    catch {
        return $null
    }
}

function New-TrxNamespaceManager {
    # Namespace manager with the VSTest TRX prefix "t" for XPath queries.
    # Returned via the "," operator: XmlNamespaceManager implements IEnumerable and
    # would otherwise be unrolled into the registered prefixes.
    param([xml]$Trx)
    $ns = [System.Xml.XmlNamespaceManager]::new($Trx.NameTable)
    $ns.AddNamespace("t", $script:TrxNamespaceUri)
    return ,$ns
}

function Get-TrxTestResults {
    # Returns testName -> outcome per UnitTestResult; $null for a missing/unreadable file.
    param([string]$TrxPath)
    $trx = Get-TrxDocument -TrxPath $TrxPath
    if ($null -eq $trx) { return $null }
    $ns = New-TrxNamespaceManager -Trx $trx
    $resultNodes = @($trx.SelectNodes("//t:UnitTestResult", $ns))
    if ($resultNodes.Count -eq 0) { return $null }
    $results = [ordered]@{}
    foreach ($node in $resultNodes) {
        $results["$($node.testName)"] = "$($node.outcome)"
    }
    return $results
}

function Get-TrxRunStartTime {
    # Run start timestamp (UTC) from the <Times start="..."> attribute of the TestRun
    # node; $null when not readable. More robust than LastWriteTimeUtc because the
    # timestamp comes from the run itself, not from the download/copy time of the file.
    param([string]$TrxPath)
    $trx = Get-TrxDocument -TrxPath $TrxPath
    if ($null -eq $trx) { return $null }
    $ns = New-TrxNamespaceManager -Trx $trx
    $timesNode = $trx.SelectSingleNode("//t:Times", $ns)
    if ($null -eq $timesNode -or [string]::IsNullOrWhiteSpace("$($timesNode.start)")) { return $null }
    try {
        return [datetimeoffset]::Parse("$($timesNode.start)").UtcDateTime
    }
    catch {
        return $null
    }
}
