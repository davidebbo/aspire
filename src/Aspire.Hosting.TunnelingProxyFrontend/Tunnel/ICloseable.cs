// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal interface ICloseable
{
    bool IsClosed { get; }
    void Abort();
}
