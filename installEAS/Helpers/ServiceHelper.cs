namespace installEAS.Helpers;

public static class ServiceHelper
{
    public static bool ServiceExists(string ServiceName)
    {
        return RegistryTools.KeyExists("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\" + ServiceName);
    }

    public static void StopService(string ServiceName)
    {
        ProcessTools.StartHidden("cmd.exe", "/c sc.exe stop " + ServiceName);
    }

    public static void StartService(string ServiceName)
    {
        ProcessTools.StartHidden("cmd.exe", "/c sc.exe start " + ServiceName);
    }

    public static void ChangeServiceStartup(string ServiceName, ServiceStartupType startupType, bool DelayedAutoStart = false)
    {
        var str = "auto";
        switch (startupType)
        {
            case ServiceStartupType.Boot:
                str = "boot";
                break;
            case ServiceStartupType.System:
                str = "system";
                break;
            case ServiceStartupType.Automatic:
                str = "auto";
                if (DelayedAutoStart) str = "delayed-auto";
                break;
            case ServiceStartupType.Manual:
                str = "demand";
                break;
            case ServiceStartupType.Disabled:
                str = "disabled";
                break;
        }

        ProcessTools.StartHidden("cmd.exe", "/c sc.exe config " + ServiceName + " start= " + str);
        var registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\" + ServiceName, true);
        if (registryKey == null) return;
        registryKey.SetValue("Start", (int)startupType, RegistryValueKind.DWord);
        if (startupType == ServiceStartupType.Automatic) registryKey.SetValue(nameof(DelayedAutoStart), DelayedAutoStart ? 1 : 0, RegistryValueKind.DWord);
        registryKey.Close();
    }

    public static void DisableServiceAccess(string ServiceName, bool Disable)
    {
        var securityIdentifier1 = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        var securityIdentifier2 = new SecurityIdentifier(WellKnownSidType.CreatorOwnerSid, null);
        var securityIdentifier3 = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
        var identity1 = (NTAccount)securityIdentifier1.Translate(typeof(NTAccount));
        var identity2 = (NTAccount)securityIdentifier2.Translate(typeof(NTAccount));
        var identity3 = (NTAccount)securityIdentifier3.Translate(typeof(NTAccount));
        var rule1 = new RegistryAccessRule(identity1, RegistryRights.ExecuteKey, InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow);
        var rule2 = new RegistryAccessRule(identity2, RegistryRights.ExecuteKey, InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow);
        var rule3 = new RegistryAccessRule(identity3, RegistryRights.ExecuteKey, InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow);
        var registryAccessRule1 = new RegistryAccessRule(identity1, RegistryRights.FullControl, InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow);
        var registryAccessRule2 = new RegistryAccessRule(identity2, RegistryRights.FullControl, InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow);
        var registryAccessRule3 = new RegistryAccessRule(identity3, RegistryRights.FullControl, InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow);
        var registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\" + ServiceName, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.TakeOwnership);
        var registrySecurity = new RegistrySecurity();
        if (Disable)
        {
            if (registryKey != null)
            {
                var accessControl1 = registryKey.GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner);
                accessControl1.SetOwner(identity1);
                registryKey.SetAccessControl(accessControl1);
            }

            if (registryKey != null)
            {
                var accessControl2 = registryKey.GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner);
                accessControl2.SetAccessRuleProtection(true, true);
                registryKey.SetAccessControl(accessControl2);
            }

            if (registryKey != null)
            {
                var accessControl3 = registryKey.GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner);
                foreach (RegistryAccessRule accessRule in accessControl3.GetAccessRules(true, true, typeof(NTAccount)))
                {
                    if (accessRule.IdentityReference.Equals(identity1)) accessControl3.RemoveAccessRuleSpecific(accessRule);
                    if (accessRule.IdentityReference.Equals(identity3)) accessControl3.RemoveAccessRuleSpecific(accessRule);
                    if (accessRule.IdentityReference.Equals(identity2)) accessControl3.RemoveAccessRuleSpecific(accessRule);
                }

                accessControl3.AddAccessRule(rule1);
                accessControl3.AddAccessRule(rule2);
                accessControl3.AddAccessRule(rule3);
                registryKey.SetAccessControl(accessControl3);
            }
        }
        else
        {
            if (registryKey != null)
            {
                var accessControl4 = registryKey.GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner);
                accessControl4.SetOwner(identity1);
                registryKey.SetAccessControl(accessControl4);
            }

            if (registryKey != null)
            {
                var accessControl5 = registryKey.GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner);
                accessControl5.SetAccessRuleProtection(false, false);
                registryKey.SetAccessControl(accessControl5);
            }

            if (registryKey != null)
            {
                var accessControl6 = registryKey.GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner);
                var accessRules = accessControl6.GetAccessRules(true, false, typeof(SecurityIdentifier));
                for (var index = 0; index < accessRules.Count; ++index)
                    try
                    {
                        var rule4 = (RegistryAccessRule)accessRules[index];
                        accessControl6.RemoveAccessRule(rule4);
                    }
                    catch
                    {
                        // ignored
                    }

                accessControl6.SetOwner(identity3);
                registryKey.SetAccessControl(accessControl6);
            }
        }

        registryKey?.Close();
    }

    public enum ServiceStartupType
    {
        Boot,
        System,
        Automatic,
        Manual,
        Disabled
    }
}