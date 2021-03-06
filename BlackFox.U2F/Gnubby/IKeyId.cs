﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlackFox.U2F.Gnubby
{
    public interface IKeyId : IEquatable<IKeyId>
    {
        string Product { get; }
        string Manufacturer { get; }
        Task<IKey> OpenAsync(CancellationToken cancellationToken = default (CancellationToken));
    }
}