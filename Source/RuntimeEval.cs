using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using static ImprovedHordes.IHLog;

namespace ImprovedHordes
{
    public class RuntimeEvalRegistry
    {
        public static Dictionary<Type, RuntimeVarTypeBase> VariableTypes = new Dictionary<Type, RuntimeVarTypeBase>();
        public static Dictionary<string, RuntimeVar> Variables = new Dictionary<string, RuntimeVar>();

        static RuntimeEvalRegistry()
        {
            RegisterVariableType(RuntimeVarPredefinedTypes.UINT32);
            RegisterVariableType(RuntimeVarPredefinedTypes.INT32);
            RegisterVariableType(RuntimeVarPredefinedTypes.BOOL);
            RegisterVariableType(RuntimeVarPredefinedTypes.FLOAT);
        }

        public static void RegisterVariableType<T>(RuntimeVarType<T> type)
        {
            if (VariableTypes.ContainsKey(typeof(T)))
                throw new Exception(String.Format("Variable type handler {0} is already registered. Registered type handler: {1}", typeof(T).FullName, VariableTypes[typeof(T)].GetType().FullName));

            VariableTypes.Add(typeof(T), type);
        }

        public static RuntimeVarType<T> GetVariableType<T>()
        {
            if (!VariableTypes.ContainsKey(typeof(T)))
                throw new Exception(String.Format("Variable Type {0} has not been defined.", typeof(T).FullName));

            return VariableTypes[typeof(T)] as RuntimeVarType<T>;
        }

        public static void RegisterVariable<T>(string variableName, Func<T> fetcher)
        {
            if (Variables.ContainsKey(variableName))
                throw new Exception(String.Format("Variable {0} is already defined.", variableName));

            if (!VariableTypes.ContainsKey(typeof(T)))
                throw new Exception(String.Format("Variable {0} cannot be defined as the Variable Type {1} has not been defined.", variableName, typeof(T).FullName));

            RuntimeVarType<T> type = VariableTypes[typeof(T)] as RuntimeVarType<T>;

            RuntimeVar variable = new RuntimeVar(type, () =>
            {
                return type.ToString(fetcher.Invoke());
            });

            Variables.Add(variableName, variable);
        }

        // P - parameter
        public static void RegisterVariable<T, P>(string variableName, Func<P, T> fetcher)
        {

        }
    }

    public class RuntimeEval<T>
    {
        private readonly string expression;
        private readonly Func<string, T> parser;

        private RuntimeEval<T> trueEval;
        private RuntimeEval<T> falseEval;

        private readonly List<EvalVariable> variables = new List<EvalVariable>();

        private RuntimeEval(string expr, Func<string, T> parser)
        {
            this.expression = expr;
            this.parser = parser;
        }

        public static RuntimeEval<T> Parse(string expr, Func<string, T> parser)
        {
            RuntimeEval<T> eval = new RuntimeEval<T>(expr, parser);

            if(eval.IsValid())
                eval.ParseConditions();

            return eval;
        }

        public string GetExpression()
        {
            return this.expression;
        }

        public T Evaluate()
        {
            if (!this.IsValid())
            {
                return parser.Invoke(this.expression);
            }

            if (!this.IsConditional())
            {
                string variable = this.expression.Substring(1, this.expression.Length - 2);

                if (!RuntimeEvalRegistry.Variables.ContainsKey(variable))
                {
                    throw new NullReferenceException(String.Format("Variable {0} has not been defined.", variable));
                }

                return parser.Invoke(RuntimeEvalRegistry.Variables[variable].fetcher.Invoke());
            }

            // TODO: OR | operation
            bool success = true;
            foreach (var variable in variables)
            {
                string variableRef = variable.variable;
                EEvalCondition cond = variable.condition;
                string toCompare = variable.compareAgainstValue;

                if (!RuntimeEvalRegistry.Variables.ContainsKey(variableRef))
                {
                    Warning(String.Format("Variable {0} has not been defined. Defaulting to false.", variableRef));
                    success = false;
                    continue;
                }

                RuntimeVar runtimeVariable = RuntimeEvalRegistry.Variables[variableRef];
                if (!runtimeVariable.Compare(cond, toCompare))
                {
                    success = false;
                }
            }

            if (success)
                return this.trueEval.Evaluate();
            else
            {
                if (this.falseEval != null)
                    return this.falseEval.Evaluate();
                else
                    throw new InvalidOperationException(String.Format("Cannot evaluate false expression for expression {0} as it does not exist.", expression));
            }
        }

        public bool Nullable()
        {
            return this.falseEval == null;
        }

        private static bool IsValid(string expr)
        {
            return (expr.StartsWith("{") && expr.EndsWith("}"));
        }

        private bool IsValid()
        {
            return IsValid(this.expression);
        }

        private static readonly Dictionary<string, EEvalCondition> _operators = new Dictionary<string, EEvalCondition>() { { "=", EEvalCondition.EQUAL_TO }, { "!=", EEvalCondition.NOT_EQUAL_TO }, { "<", EEvalCondition.LESS_THAN }, { "<=", EEvalCondition.LESS_THAN_OR_EQUAL_TO }, { ">", EEvalCondition.GREATER_THAN }, { ">=", EEvalCondition.GREATER_THAN_OR_EQUAL_TO } };
        private EvalVariable? FindConditionalVariable(string exprSub)
        {
            EvalVariable? variable = null;

            foreach(var op in _operators)
            {
                var opSymbol = op.Key;

                if(exprSub.Contains(opSymbol))
                {
                    variable = new EvalVariable()
                    {
                        variable = exprSub.Substring(0, exprSub.IndexOf(opSymbol)), // Name
                        condition = _operators[opSymbol], // Operator
                        compareAgainstValue = exprSub.Substring(exprSub.IndexOf(opSymbol) + 1) // Result
                    };

                    Log(variable.Value.variable);
                    Log(opSymbol);
                    Log(variable.Value.compareAgainstValue);

                    return variable;
                }
            }

            string boolVarName = exprSub;
            bool not = false;
            if(boolVarName.StartsWith("!"))
            {
                boolVarName = boolVarName.Substring(1);
                not = true;
            }

            if(RuntimeEvalRegistry.Variables.ContainsKey(boolVarName))
            {
                variable = new EvalVariable()
                {
                    variable = boolVarName,
                    condition = EEvalCondition.EQUAL_TO,
                    compareAgainstValue = RuntimeEvalRegistry.GetVariableType<bool>().ToString(!not)
                };
            }

            return variable;
        }

        private bool IsConditional()
        {
            return this.falseEval != null && this.trueEval != null;
        }

        private void ParseConditions()
        {
            string expr = this.expression;

            if(expr.StartsWith("{"))
                expr = expr.Substring(1);

            if (expr.EndsWith("}"))
                expr = expr.Substring(0, this.expression.Length - 2);

            Log("IsCondition {0}", expr);

            int occurances = FindOccurancesOf(expr, '&');

            int lastIndex = 0;
            for(int i = 0; i < occurances + 1; i++)
            {
                char endChar = i == occurances ? '?' : '&';
                int endIndex = expr.IndexOf(endChar);

                if (endIndex == -1) // Not conditional.
                    break;

                string exprSub = expr.Substring(lastIndex, endIndex - lastIndex);
                Log("ExprSub: {0}", exprSub);

                EvalVariable? variable = FindConditionalVariable(exprSub);
                if(variable.HasValue)
                {
                    variables.Add(variable.Value);
                }
                else
                {
                    Warning("Conditional variable {0} was not correctly formatted. Will be skipped.", exprSub);
                }

                lastIndex = endIndex;
            }

            if (variables.Count == 0)
                return;

            Log("Pre");
            int endTrueIndex = expr.IndexOf(':');
            bool hasFalse = true;

            if (endTrueIndex == -1)
            {
                endTrueIndex = expr.Length;
                hasFalse = false;
            }

            string trueEvalExpr = expr.Substring(lastIndex+1, endTrueIndex - lastIndex - 1);
            Log("trueeval {0}", trueEvalExpr);

            RuntimeEval<T> trueEval = Parse(trueEvalExpr, this.parser);
            this.trueEval = trueEval;

            if (hasFalse)
            {
                Log("Falseevaling");
                string falseEvalExpr = expr.Substring(endTrueIndex + 1);
                Log("falseeval {0}", falseEvalExpr);

                RuntimeEval<T> falseEval = Parse(falseEvalExpr, this.parser);
                this.falseEval = falseEval;
            }
        }

        private static int FindOccurancesOf(string expr, char c)
        {
            int count = 0;

            for(var i = 0; i < expr.Length; i++)
            {
                if (expr[i] == c)
                    count++;
            }

            return count;
        }

        struct EvalVariable
        {
            public string variable;
            public EEvalCondition condition;
            public string compareAgainstValue;
        }
    }

    public enum EEvalCondition
    {
        LESS_THAN,
        LESS_THAN_OR_EQUAL_TO,
        GREATER_THAN,
        GREATER_THAN_OR_EQUAL_TO,
        EQUAL_TO,
        NOT_EQUAL_TO
    }

    public sealed class RuntimeVar
    {
        public readonly RuntimeVarTypeBase type;
        public readonly Func<string> fetcher;
        
        public RuntimeVar(RuntimeVarTypeBase type, Func<string> fetcher)
        {
            this.type = type;
            this.fetcher = fetcher;
        }

        public bool Compare(EEvalCondition condition, string rhs)
        {
            return type.Compare(condition, this.fetcher.Invoke(), rhs);
        }
    }

    public abstract class RuntimeVarTypeBase
    {
        public abstract bool Compare(EEvalCondition condition, string lhs, string rhs);
    }

    public abstract class RuntimeVarType<T> : RuntimeVarTypeBase
    {
        public override bool Compare(EEvalCondition condition, string lhs, string rhs)
        {
            return Compare(condition, Parse(lhs), Parse(rhs));
        }

        public abstract bool Compare(EEvalCondition condition, T lhs, T rhs);
        public abstract T Parse(string str);

        public abstract string ToString(T value);
    }

    public sealed class RuntimeVarPredefinedTypes
    {
        public static readonly RuntimeVarType<uint> UINT32 = new RuntimeUInt32VarType();
        public static readonly RuntimeVarType<int> INT32 = new RuntimeInt32VarType();
        public static readonly RuntimeVarType<bool> BOOL = new RuntimeBoolVarType();
        public static readonly RuntimeVarType<float> FLOAT = new RuntimeFloatVarType();

        public sealed class RuntimeUInt32VarType : RuntimeVarType<uint>
        {
            public override bool Compare(EEvalCondition condition, uint lhs, uint rhs)
            {
                switch (condition)
                {
                    case EEvalCondition.LESS_THAN:
                        return lhs < rhs;
                    case EEvalCondition.LESS_THAN_OR_EQUAL_TO:
                        return lhs <= rhs;
                    case EEvalCondition.GREATER_THAN:
                        return lhs > rhs;
                    case EEvalCondition.GREATER_THAN_OR_EQUAL_TO:
                        return lhs >= rhs;
                    case EEvalCondition.EQUAL_TO:
                        return lhs == rhs;
                    case EEvalCondition.NOT_EQUAL_TO:
                        return lhs != rhs;
                }

                return false;
            }

            public override uint Parse(string str)
            {
                return StringParsers.ParseUInt32(str);
            }

            public override string ToString(uint value)
            {
                return value.ToString();
            }
        }

        public sealed class RuntimeFloatVarType : RuntimeVarType<float>
        {
            public override bool Compare(EEvalCondition condition, float lhs, float rhs)
            {
                switch(condition)
                {
                    case EEvalCondition.LESS_THAN:
                        return lhs < rhs;
                    case EEvalCondition.LESS_THAN_OR_EQUAL_TO:
                        return lhs <= rhs;
                    case EEvalCondition.GREATER_THAN:
                        return lhs > rhs;
                    case EEvalCondition.GREATER_THAN_OR_EQUAL_TO:
                        return lhs >= rhs;
                    case EEvalCondition.EQUAL_TO:
                        return lhs == rhs;
                    case EEvalCondition.NOT_EQUAL_TO:
                        return lhs != rhs;
                }

                return false;
            }

            public override float Parse(string str)
            {
                return StringParsers.ParseFloat(str);
            }

            public override string ToString(float value)
            {
                return value.ToString();
            }
        }

        public sealed class RuntimeInt32VarType : RuntimeVarType<int>
        {
            public override bool Compare(EEvalCondition condition, int lhs, int rhs)
            {
                switch (condition)
                {
                    case EEvalCondition.LESS_THAN:
                        return lhs < rhs;
                    case EEvalCondition.LESS_THAN_OR_EQUAL_TO:
                        return lhs <= rhs;
                    case EEvalCondition.GREATER_THAN:
                        return lhs > rhs;
                    case EEvalCondition.GREATER_THAN_OR_EQUAL_TO:
                        return lhs >= rhs;
                    case EEvalCondition.EQUAL_TO:
                        return lhs == rhs;
                    case EEvalCondition.NOT_EQUAL_TO:
                        return lhs != rhs;
                }

                return false;
            }

            public override int Parse(string str)
            {
                return StringParsers.ParseSInt32(str);
            }

            public override string ToString(int value)
            {
                return value.ToString();
            }
        }

        public sealed class RuntimeBoolVarType : RuntimeVarType<bool>
        {
            public override bool Compare(EEvalCondition condition, bool lhs, bool rhs)
            {
                switch (condition)
                {
                    case EEvalCondition.EQUAL_TO:
                        return lhs == rhs;
                    case EEvalCondition.NOT_EQUAL_TO:
                        return lhs != rhs;
                }

                return false;
            }

            public override bool Parse(string str)
            {
                return StringParsers.ParseBool(str);
            }

            public override string ToString(bool value)
            {
                return value.ToString();
            }
        }
    }
}
