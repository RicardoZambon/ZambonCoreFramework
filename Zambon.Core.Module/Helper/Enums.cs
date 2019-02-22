﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Zambon.Core.Module.Helper
{
    public class Enums
    {
        public enum AuthenticationType
        {
            [Display(Name = "User & Password")]
            UserPassword = 0,
            LDAP = 1
        }

        public enum PermissionTypes
        {
            FullAccess = 0,
            ReadWrite = 1, //Navigate, Read, Create, Write
            ReadOnly = 2, //Navigate, Read
            Navigate = 3,
            Read = 4,
            Create = 5,
            Write = 6,
            Delete = 7
        }

    }
}