// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric Engine contributors.
// Licensed under the MIT License. See LICENSE.txt in root directory.
// ***********************************************************************

#if NET20
namespace System
{
    internal delegate void Action<T>(T arg);
    internal delegate void Action<T1, T2>(T1 arg1, T2 arg2);
    internal delegate void Action<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3);
}
#endif
