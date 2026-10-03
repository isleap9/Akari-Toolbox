namespace AkariToolbox.Tweaks;

/// <summary>
/// Ultimate "8 Advanced" script 17 (Services): the 273-entry service start-value
/// table, ported 1:1 from the script's Off/Default .reg blobs (verified same order,
/// combined into off/on pairs). Applied in safe mode via RunOnce re-entry.
/// </summary>
public static partial class AdvancedActions
{
    private readonly record struct ServiceStart(string Name, int Off, int On);

    public static void ApplyServicesOff()
    {
        ApplyServiceTable(off: true);
        ArmSafeBoot("*servicesoff", "--services-off");
    }

    public static void ApplyServicesDefault()
    {
        ApplyServiceTable(off: false);
        ArmSafeBoot("*serviceson", "--services-on");
    }

    public static void FinishServicesOff()
    {
        ApplyServiceTable(off: true);
        NativeOps.RunTool("bcdedit", "/deletevalue {current} safeboot");
        NativeOps.Restart();
    }

    public static void FinishServicesDefault()
    {
        ApplyServiceTable(off: false);
        NativeOps.RunTool("bcdedit", "/deletevalue {current} safeboot");
        NativeOps.Restart();
    }

    private static void ApplyServiceTable(bool off)
    {
        foreach (var entry in ServiceTable)
        {
            try
            {
                TweakAction.RegDword(
                    $@"HKLM\SYSTEM\ControlSet001\Services\{entry.Name}",
                    "Start", off ? entry.Off : entry.On).Apply();
            }
            catch { }
        }
    }

    private static int ServiceStartValue(string name)
    {
        int? v = RegRead.Dword($@"HKLM\SYSTEM\ControlSet001\Services\{name}", "Start");
        return v ?? -1;
    }

    /// <summary>Off when the three discriminating services all read off-values,
    /// Default when they all read on-values, otherwise mixed.</summary>
    public static bool? ReadServicesOff()
    {
        int sysMain = ServiceStartValue("SysMain");
        int diagTrack = ServiceStartValue("DiagTrack");
        int push = ServiceStartValue("dmwappushservice");
        if (sysMain == 4 && diagTrack == 4 && push == 4)
            return true;
        if (sysMain == 2 && diagTrack == 2 && push == 3)
            return false;
        return null;
    }

    private static readonly ServiceStart[] ServiceTable =
    [
        new("ADPSvc", 4, 3),
        new("AarSvc", 4, 3),
        new("AJRouter", 4, 3),
        new("ALG", 4, 3),
        new("AppIDSvc", 4, 3),
        new("Appinfo", 3, 3),
        new("AppMgmt", 4, 3),
        new("AppReadiness", 4, 3),
        new("AppVClient", 4, 4),
        new("AppXSvc", 3, 3),
        new("ApxSvc", 4, 3),
        new("AssignedAccessManagerSvc", 4, 3),
        new("AudioEndpointBuilder", 2, 2),
        new("Audiosrv", 2, 2),
        new("autotimesvc", 4, 3),
        new("AxInstSV", 4, 3),
        new("BcastDVRUserService", 4, 3),
        new("BDESVC", 4, 3),
        new("BFE", 4, 2),
        new("BITS", 4, 2),
        new("BluetoothUserService", 4, 3),
        new("Browser", 4, 3),
        new("BrokerInfrastructure", 2, 2),
        new("BTAGService", 4, 3),
        new("BthAvctpSvc", 4, 3),
        new("bthserv", 4, 3),
        new("camsvc", 3, 3),
        new("CaptureService", 4, 3),
        new("cbdhsvc", 4, 2),
        new("CDPSvc", 4, 2),
        new("CDPUserSvc", 4, 2),
        new("CertPropSvc", 4, 3),
        new("ClipSVC", 4, 3),
        new("CloudBackupRestoreSvc", 4, 3),
        new("cloudidsvc", 4, 3),
        new("COMSysApp", 4, 3),
        new("ConsentUxUserSvc", 4, 3),
        new("CoreMessagingRegistrar", 2, 2),
        new("CredentialEnrollmentManagerUserSvc", 4, 3),
        new("CryptSvc", 2, 2),
        new("CscService", 4, 3),
        new("DcomLaunch", 2, 2),
        new("dcsvc", 4, 3),
        new("defragsvc", 4, 3),
        new("DeviceAssociationBrokerSvc", 4, 3),
        new("DeviceAssociationService", 4, 3),
        new("DeviceInstall", 2, 2),
        new("DevicePickerUserSvc", 4, 3),
        new("DevicesFlowUserSvc", 4, 3),
        new("DevQueryBroker", 4, 3),
        new("Dhcp", 2, 2),
        new("diagnosticshub.standardcollector.service", 4, 3),
        new("diagsvc", 4, 3),
        new("DiagTrack", 4, 2),
        new("DialogBlockingService", 4, 4),
        new("DispBrokerDesktopSvc", 2, 2),
        new("DisplayEnhancementService", 4, 3),
        new("DmEnrollmentSvc", 3, 3),
        new("dmwappushservice", 4, 3),
        new("Dnscache", 2, 2),
        new("DoSvc", 4, 2),
        new("dot3svc", 4, 3),
        new("DPS", 4, 2),
        new("DsmSvc", 4, 3),
        new("DsSvc", 4, 3),
        new("DusmSvc", 4, 2),
        new("EapHost", 4, 3),
        new("EFS", 4, 3),
        new("embeddedmode", 4, 3),
        new("EntAppSvc", 4, 3),
        new("EventLog", 4, 2),
        new("EventSystem", 4, 2),
        new("Fax", 4, 3),
        new("fdPHost", 4, 3),
        new("FDResPub", 4, 3),
        new("fhsvc", 4, 3),
        new("FontCache", 4, 2),
        new("FontCache3.0.0.0", 4, 3),
        new("FrameServerMonitor", 4, 3),
        new("FrameServer", 4, 3),
        new("GameInputSvc", 4, 3),
        new("gpsvc", 2, 2),
        new("GraphicsPerfSvc", 4, 3),
        new("hidserv", 4, 3),
        new("hpatchmon", 4, 3),
        new("HvHost", 4, 3),
        new("icssvc", 4, 3),
        new("IKEEXT", 4, 3),
        new("InstallService", 4, 3),
        new("InventorySvc", 4, 3),
        new("iphlpsvc", 4, 2),
        new("IpxlatCfgSvc", 4, 3),
        new("KeyIso", 3, 3),
        new("KtmRm", 4, 3),
        new("LanmanServer", 4, 2),
        new("LanmanWorkstation", 4, 2),
        new("lfsvc", 4, 3),
        new("LicenseManager", 4, 3),
        new("lltdsvc", 4, 3),
        new("lmhosts", 4, 3),
        new("LocalKdc", 4, 3),
        new("LSM", 2, 2),
        new("LxpSvc", 4, 3),
        new("MapsBroker", 4, 2),
        new("McmSvc", 4, 3),
        new("McpManagementService", 4, 3),
        new("MessagingService", 4, 3),
        new("midisrv", 4, 3),
        new("MixedRealityOpenXRSvc", 4, 3),
        new("mpssvc", 4, 2),
        new("MSDTC", 4, 3),
        new("MSiSCSI", 4, 3),
        new("msiserver", 3, 3),
        new("MsKeyboardFilter", 4, 4),
        new("NaturalAuthentication", 4, 3),
        new("NcaSvc", 4, 3),
        new("NcbService", 4, 3),
        new("NcdAutoSetup", 4, 3),
        new("Netlogon", 4, 3),
        new("Netman", 3, 3),
        new("netprofm", 3, 3),
        new("NetSetupSvc", 3, 3),
        new("NetTcpPortSharing", 4, 4),
        new("NgcCtnrSvc", 3, 3),
        new("NgcSvc", 3, 3),
        new("NlaSvc", 4, 2),
        new("NPSMSvc", 4, 3),
        new("nsi", 2, 2),
        new("OneSyncSvc", 4, 2),
        new("p2pimsvc", 4, 3),
        new("p2psvc", 4, 3),
        new("P9RdrService", 4, 3),
        new("PcaSvc", 4, 2),
        new("PeerDistSvc", 4, 3),
        new("PenService", 4, 3),
        new("perceptionsimulation", 4, 3),
        new("PerfHost", 4, 3),
        new("PhoneSvc", 4, 3),
        new("PimIndexMaintenanceSvc", 4, 3),
        new("pla", 4, 3),
        new("PlugPlay", 4, 3),
        new("PNRPAutoReg", 4, 3),
        new("PNRPsvc", 4, 3),
        new("PolicyAgent", 4, 3),
        new("Power", 2, 2),
        new("PrintDeviceConfigurationService", 4, 3),
        new("PrintNotify", 4, 3),
        new("PrintScanBrokerService", 4, 3),
        new("PrintWorkflowUserSvc", 4, 3),
        new("ProfSvc", 2, 2),
        new("PushToInstall", 4, 3),
        new("QWAVE", 4, 3),
        new("RasAuto", 4, 3),
        new("RasMan", 4, 3),
        new("refsdedupsvc", 4, 3),
        new("RemoteAccess", 4, 4),
        new("RemoteRegistry", 4, 4),
        new("RetailDemo", 4, 3),
        new("RmSvc", 4, 3),
        new("RpcEptMapper", 2, 2),
        new("RpcLocator", 4, 3),
        new("RpcSs", 2, 2),
        new("SamSs", 4, 2),
        new("SCardSvr", 4, 3),
        new("ScDeviceEnum", 4, 3),
        new("Schedule", 2, 2),
        new("SCPolicySvc", 4, 3),
        new("SDRSVC", 4, 3),
        new("seclogon", 3, 3),
        new("SEMgrSvc", 4, 3),
        new("SensorDataService", 4, 3),
        new("SensorService", 4, 3),
        new("SensrSvc", 4, 3),
        new("SENS", 2, 2),
        new("SessionEnv", 4, 3),
        new("SgrmBroker", 4, 2),
        new("SharedAccess", 4, 3),
        new("SharedRealitySvc", 4, 3),
        new("ShellHWDetection", 4, 2),
        new("shpamsvc", 4, 4),
        new("smphost", 4, 3),
        new("SmsRouter", 4, 3),
        new("SNMPTrap", 4, 3),
        new("spectrum", 4, 3),
        new("Spooler", 4, 2),
        new("sppsvc", 2, 2),
        new("SSDPSRV", 4, 3),
        new("ssh-agent", 4, 4),
        new("SstpSvc", 4, 3),
        new("StateRepository", 3, 2),
        new("stisvc", 4, 3),
        new("StiSvc", 4, 3),
        new("StorSvc", 4, 2),
        new("svsvc", 4, 3),
        new("swprv", 4, 3),
        new("SysMain", 4, 2),
        new("SystemEventsBroker", 2, 2),
        new("TabletInputService", 4, 3),
        new("TapiSrv", 4, 3),
        new("TermService", 3, 3),
        new("TextInputManagementService", 2, 2),
        new("Themes", 4, 2),
        new("TieringEngineService", 4, 3),
        new("TimeBrokerSvc", 3, 3),
        new("TokenBroker", 4, 3),
        new("TrkWks", 4, 2),
        new("TroubleshootingSvc", 4, 3),
        new("TrustedInstaller", 3, 3),
        new("tzautoupdate", 4, 4),
        new("UdkUserSvc", 4, 3),
        new("UevAgentService", 4, 4),
        new("uhssvc", 4, 4),
        new("UmRdpService", 4, 3),
        new("UnistoreSvc", 4, 3),
        new("upnphost", 4, 3),
        new("UserDataSvc", 4, 3),
        new("UserManager", 2, 2),
        new("UsoSvc", 4, 2),
        new("VacSvc", 4, 3),
        new("VaultSvc", 4, 3),
        new("vds", 4, 3),
        new("vmicguestinterface", 4, 3),
        new("vmicheartbeat", 4, 3),
        new("vmickvpexchange", 4, 3),
        new("vmicrdv", 4, 3),
        new("vmicshutdown", 4, 3),
        new("vmictimesync", 4, 3),
        new("vmicvmsession", 4, 3),
        new("vmicvss", 4, 3),
        new("VSS", 4, 3),
        new("W32Time", 4, 3),
        new("WaaSMedicSvc", 4, 3),
        new("WalletService", 4, 3),
        new("WarpJITSvc", 4, 3),
        new("wbengine", 4, 3),
        new("WbioSrvc", 4, 3),
        new("Wcmsvc", 2, 2),
        new("wcncsvc", 4, 3),
        new("WdiServiceHost", 4, 3),
        new("WdiSystemHost", 4, 3),
        new("WebClient", 4, 3),
        new("Wecsvc", 4, 3),
        new("WEPHOSTSVC", 4, 3),
        new("wercplsupport", 4, 3),
        new("WerSvc", 4, 3),
        new("WFDSConMgrSvc", 4, 3),
        new("whesvc", 4, 2),
        new("WiaRpc", 4, 3),
        new("WinHttpAutoProxySvc", 4, 3),
        new("Winmgmt", 2, 2),
        new("WinRM", 4, 3),
        new("wisvc", 4, 3),
        new("WlanSvc", 4, 3),
        new("wlidsvc", 4, 3),
        new("wlpasvc", 3, 3),
        new("WManSvc", 3, 3),
        new("wmiApSrv", 4, 3),
        new("WMPNetworkSvc", 4, 3),
        new("workfolderssvc", 4, 3),
        new("WpcMonSvc", 4, 3),
        new("WPDBusEnum", 4, 3),
        new("WpnService", 4, 2),
        new("WpnUserService", 4, 2),
        new("WSAIFabricSvc", 4, 2),
        new("WSearch", 4, 2),
        new("wuauserv", 4, 3),
        new("wuqisvc", 4, 3),
        new("WwanSvc", 4, 3),
        new("XblAuthManager", 4, 3),
        new("XblGameSave", 4, 3),
        new("XboxGipSvc", 4, 3),
        new("XboxNetApiSvc", 4, 3),
        new("ZTHELPER", 4, 3),
    ];
}
