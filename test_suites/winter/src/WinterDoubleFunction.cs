namespace Cthangover.Mods.RpgPlus.Tests
{
    using System.Collections.Generic;
    using Cthangover.Mods.Expressions;
    using Cthangover.Mods.Stats.Formulas;

    /// <summary>Public opt-in test strategy proves discovery from a separately compiled consumer mod.</summary>
    public class WinterDoubleFunction : IExpressionFunctionStrategy
    {
        public string Name => "winter_test_double";
        public int MinArguments => 1;
        public int MaxArguments => 1;
        public double Evaluate(ExpressionEvaluationContext context, IReadOnlyList<FormulaNode> arguments)
            => context.Value(arguments[0]) * 2;
    }
}
