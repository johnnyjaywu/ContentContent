using System;

namespace ContentContent.Dialog
{
    public enum DialogComparisonOperator
    {
        Equal = 0,
        NotEqual = 1,
        GreaterThan = 2,
        GreaterOrEqual = 3,
        LessThan = 4,
        LessOrEqual = 5,
        Exists = 6,
        NotExists = 7
    }

    /// <summary>
    /// Data-driven predicate evaluated against an <see cref="IDialogVariableStore"/>.
    /// </summary>
    public sealed class DialogCondition
    {
        public string VariableName { get; }
        public DialogComparisonOperator Operator { get; }
        public DialogValue CompareValue { get; }

        public DialogCondition(string variableName, DialogComparisonOperator op, DialogValue compareValue = default)
        {
            if (string.IsNullOrEmpty(variableName))
                throw new ArgumentException("Variable name cannot be null or empty.", nameof(variableName));

            VariableName = variableName;
            Operator = op;
            CompareValue = compareValue;
        }

        public bool Evaluate(IDialogVariableStore store)
        {
            if (store == null)
                return false;

            bool exists = store.TryGet(VariableName, out DialogValue current);
            switch (Operator)
            {
                case DialogComparisonOperator.Exists:
                    return exists;
                case DialogComparisonOperator.NotExists:
                    return !exists;
                case DialogComparisonOperator.Equal:
                    return exists && current.Equals(CompareValue);
                case DialogComparisonOperator.NotEqual:
                    return !exists || !current.Equals(CompareValue);
                case DialogComparisonOperator.GreaterThan:
                    return exists && current.CompareNumeric(CompareValue) > 0;
                case DialogComparisonOperator.GreaterOrEqual:
                    return exists && current.CompareNumeric(CompareValue) >= 0;
                case DialogComparisonOperator.LessThan:
                    return exists && current.CompareNumeric(CompareValue) < 0;
                case DialogComparisonOperator.LessOrEqual:
                    return exists && current.CompareNumeric(CompareValue) <= 0;
                default:
                    return false;
            }
        }
    }
}
