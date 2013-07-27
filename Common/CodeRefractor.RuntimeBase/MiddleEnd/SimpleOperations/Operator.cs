#region Usings

using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations;

#endregion

namespace CodeRefractor.RuntimeBase.MiddleEnd
{
    public class Operator : IdentifierValue
    {
        private readonly string _name;

        public Operator(string name)
        {
            _name = name;
        }

        public override string FormatVar()
        {
            return _name;
        }
    }
}