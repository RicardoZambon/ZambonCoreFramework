﻿namespace Zambon.Core.Module.Interfaces.Models
{
    public interface IValue : IParent
    {
        string Value { get; set; }

        string DisplayName { get; set; }
    }
}