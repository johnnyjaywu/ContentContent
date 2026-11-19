using System;
using System.Collections.Generic;

namespace ContentContent.Dialog
{
    /// <summary>
    /// Named command dispatched by the dialog runner to game code (e.g. play audio, open gate).
    /// </summary>
    public sealed class DialogCommand
    {
        private static readonly string[] EmptyArgs = Array.Empty<string>();

        public string Name { get; }
        public IReadOnlyList<string> Arguments { get; }

        public DialogCommand(string name, params string[] arguments)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Command name cannot be null or empty.", nameof(name));

            Name = name;
            Arguments = arguments != null && arguments.Length > 0 ? arguments : EmptyArgs;
        }
    }
}
