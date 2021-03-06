﻿#region Usings

using System.Collections.Generic;

#endregion

namespace CodeRefractor.RuntimeBase.MiddleEnd.Methods
{
    public class PureMethodTable
    {
        private readonly HashSet<string> _descriptions = new HashSet<string>();
        private static readonly PureMethodTable StaticInstance = new PureMethodTable();

        public static PureMethodTable Instance
        {
            get { return StaticInstance; }
        }

        public static bool ComputeMethodPurity(string description)
        {
            return Instance._descriptions.Contains(description);
        }
    }
}