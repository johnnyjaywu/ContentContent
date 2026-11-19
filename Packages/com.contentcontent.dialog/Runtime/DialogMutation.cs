using System;

namespace ContentContent.Dialog
{
    public enum DialogMutationOperator
    {
        Set = 0,
        Add = 1,
        Subtract = 2,
        Toggle = 3
    }

    /// <summary>
    /// Data-driven variable write applied on node entry or choice transition.
    /// </summary>
    public sealed class DialogMutation
    {
        public string VariableName { get; }
        public DialogMutationOperator Operator { get; }
        public DialogValue Value { get; }

        public DialogMutation(string variableName, DialogMutationOperator op, DialogValue value = default)
        {
            if (string.IsNullOrEmpty(variableName))
                throw new ArgumentException("Variable name cannot be null or empty.", nameof(variableName));

            VariableName = variableName;
            Operator = op;
            Value = value;
        }

        public void Apply(IDialogVariableStore store)
        {
            if (store == null)
                return;

            switch (Operator)
            {
                case DialogMutationOperator.Set:
                    store.Set(VariableName, Value);
                    break;

                case DialogMutationOperator.Toggle:
                    bool currentBool = store.GetBool(VariableName);
                    store.Set(VariableName, DialogValue.FromBool(!currentBool));
                    break;

                case DialogMutationOperator.Add:
                    ApplyArithmetic(store, 1f);
                    break;

                case DialogMutationOperator.Subtract:
                    ApplyArithmetic(store, -1f);
                    break;
            }
        }

        private void ApplyArithmetic(IDialogVariableStore store, float sign)
        {
            bool exists = store.TryGet(VariableName, out DialogValue current);
            bool useFloat = (exists && current.Type == DialogValueType.Float) || Value.Type == DialogValueType.Float;

            if (useFloat)
            {
                float baseVal = exists ? current.AsFloat() : 0f;
                store.Set(VariableName, DialogValue.FromFloat(baseVal + (Value.AsFloat() * sign)));
            }
            else
            {
                int baseVal = exists ? current.AsInt() : 0;
                store.Set(VariableName, DialogValue.FromInt(baseVal + (Value.AsInt() * (int)sign)));
            }
        }
    }
}
