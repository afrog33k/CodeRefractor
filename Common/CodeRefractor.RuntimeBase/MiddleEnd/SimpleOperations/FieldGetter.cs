#region Usings

using CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations.Identifiers;

#endregion

namespace CodeRefractor.RuntimeBase.MiddleEnd.SimpleOperations
{
    public class FieldGetter : IClonableOperation
    {
        public LocalVariable AssignedTo;
        public LocalVariable Instance;
        public string FieldName;

        public object Clone()
        {
            return new FieldGetter
            {
                AssignedTo = (LocalVariable) AssignedTo.Clone(),
                Instance = (LocalVariable) Instance.Clone(),
                FieldName = FieldName
            };
        }

        public override string ToString()
        {
            return string.Format("{0} = {1}->{2}", AssignedTo.Name, Instance.Name, FieldName);
        }
    }
}