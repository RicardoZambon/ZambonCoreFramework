﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Zambon.Core.Module.Expressions
{
    public interface IExpression
    {

        string ConditionExpression { get; set; }

        string ConditionArguments { get; set; }

        string[] ConditionArgumentsList { get; }

    }
}