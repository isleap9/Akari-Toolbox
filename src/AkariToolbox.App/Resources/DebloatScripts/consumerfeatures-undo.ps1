# Enable Consumer Features (Tips, Suggestions, Promotions)
$policyPath = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\CloudContent"
If (Test-Path $policyPath) {
    Remove-ItemProperty -Path $policyPath -Name "DisableWindowsConsumerFeatures" -Force -ErrorAction SilentlyContinue
}
# Supplementary step (intentional, not a 1:1 reversal of consumerfeatures.ps1's single
# policy-key write): also restores the out-of-box "suggested apps / content delivery /
# tips" defaults, since these are the same feature area a user expects "Consumer
# Features" to control even though Run itself never touched them.
$regKeys = @(
    @{Path="HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced"; Name="ShowSyncProviderNotifications"; Value=1; Type="DWord"},
    @{Path="HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"; Name="ContentDeliveryAllowed"; Value=1; Type="DWord"},
    @{Path="HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"; Name="OemPreInstalledAppsEnabled"; Value=1; Type="DWord"},
    @{Path="HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"; Name="PreInstalledAppsEnabled"; Value=1; Type="DWord"},
    @{Path="HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"; Name="PreInstalledAppsEverEnabled"; Value=1; Type="DWord"},
    @{Path="HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"; Name="SilentInstallAppsEnabled"; Value=1; Type="DWord"},
    @{Path="HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"; Name="SoftLandingEnabled"; Value=1; Type="DWord"},
    @{Path="HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager"; Name="SubscribedContentEnabled"; Value=1; Type="DWord"}
)
foreach ($key in $regKeys) {
    If (!(Test-Path $key.Path)) { New-Item -Path $key.Path -Force | Out-Null }
    Set-ItemProperty -Path $key.Path -Name $key.Name -Value $key.Value -Type $key.Type -Force
}
Write-Host "Consumer Features restored to default."
