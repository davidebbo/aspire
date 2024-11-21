// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ReverseProxyTunnel;

/// <summary>
/// 
/// </summary>
/// <param name="pfxFilePath"></param>
/// <param name="pfxPassword"></param>
public class TunnelingProxyConfiguration(string pfxFilePath, string pfxPassword)
{
    /// <summary>
    /// 
    /// </summary>
    public string PfxPath => pfxFilePath;
    /// <summary>
    /// 
    /// </summary>
    public string PfxPassword => pfxPassword;
}
