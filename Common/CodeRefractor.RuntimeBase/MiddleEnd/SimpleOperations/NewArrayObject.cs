#region Usings

using System;

#endregion

namespace CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations
{
    public class NewArrayObject : IdentifierValue
    {
        public Type TypeArray { get; set; }

        public IdentifierValue ArrayLength { get; set; }
    }
}