// ***********************************************************************
// Copyright (c) Charlie Poole and TestCentric contributors.
// Licensed under the MIT License. See LICENSE file in root directory.
// ***********************************************************************

#if NET20
namespace System
{
    internal delegate TResult Func<out TResult>();
    internal delegate TResult Func<in T, out TResult>(T arg);
    internal delegate TResult Func<in T1, in T2, out TResult>(T1 arg1, T2 arg2);
}
#endif
