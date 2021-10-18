using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes
{
    public class RuntimeEval
    {
        public static class Registry
        {
            public static Dictionary<Type, VariableTypeBase> VariableTypes = new Dictionary<Type, VariableTypeBase>();
            public static Dictionary<string, Variable> Variables = new Dictionary<string, Variable>();
            public static Dictionary<Type, ValueParserBase> ValueParsers = new Dictionary<Type, ValueParserBase>();

            static Registry()
            {
                PredefinedVariableTypes.Register();
                PredefinedValueParsers.Register();
            }

            public static void RegisterVariableType<T>(VariableType<T> type)
            {
                if (VariableTypes.ContainsKey(typeof(T)))
                    throw new Exception(String.Format("Variable type handler {0} is already registered. Registered type handler: {1}", typeof(T).FullName, VariableTypes[typeof(T)].GetType().FullName));

                VariableTypes.Add(typeof(T), type);
            }

            public static void RegisterValueParser<T>(ValueParser<T> parser)
            {
                if (ValueParsers.ContainsKey(typeof(T)))
                    throw new Exception(String.Format("Value parser {0} is already registered. Registered value parser: {1}", typeof(T).FullName, ValueParsers[typeof(T)].GetType().FullName));

                ValueParsers.Add(typeof(T), parser);
            }

            public static VariableType<T> GetVariableType<T>()
            {
                if (!VariableTypes.ContainsKey(typeof(T)))
                    throw new Exception(String.Format("Variable Type {0} has not been defined.", typeof(T).FullName));

                return VariableTypes[typeof(T)] as VariableType<T>;
            }

            public static VariableTypeBase GetVariableType(Type type)
            {
                if (!VariableTypes.ContainsKey(type))
                    throw new Exception(String.Format("Variable Type {0} has not been defined.", type.FullName));

                return VariableTypes[type];
            }

            public static ValueParser<T> GetValueParser<T>()
            {
                if (!ValueParsers.ContainsKey(typeof(T)))
                    throw new Exception(String.Format("Value parser {0} has not been defined.", typeof(T).FullName));

                return ValueParsers[typeof(T)] as ValueParser<T>;
            }

            public static void RegisterVariable<T>(string variableName, Func<T> fetcher)
            {
                if (Variables.ContainsKey(variableName))
                    throw new Exception(String.Format("Variable {0} is already defined.", variableName));

                if (!VariableTypes.ContainsKey(typeof(T)))
                    throw new Exception(String.Format("Variable {0} cannot be defined as the Variable Type {1} has not been defined.", variableName, typeof(T).FullName));

                VariableType<T> type = VariableTypes[typeof(T)] as VariableType<T>;

                Variable variable = new Variable(type, () =>
                {
                    return type.ToString(fetcher.Invoke());
                });

                Variables.Add(variableName, variable);
            }
        }

        public abstract class ValueParserBase
        {

        }

        public abstract class ValueParser<T> : ValueParserBase
        {
            public abstract Func<string, T> GetParser();
        }

        public class Value<T>
        {
            private readonly string expression;
            private readonly Func<string, T> parser;

            private bool conditional;
            private Value<T> ifTrue;
            private Value<T> ifFalse;

            private readonly List<EvalVariable> variables = new List<EvalVariable>();

            private Value(string expr, Func<string, T> parser)
            {
                this.expression = expr;
                this.parser = parser;
            }

            public static Value<T> Parse(string expr, Func<string, T> parser = null)
            {
                if(parser == null)
                {
                    ValueParser<T> valueParser = Registry.GetValueParser<T>();
                    parser = valueParser.GetParser();
                }

                Value<T> value = new Value<T>(expr, parser);

                if (value.IsValid())
                    value.ParseConditions();

                return value;
            }

            public string GetExpression()
            {
                return this.expression;
            }

            public T Evaluate()
            {
                return EvaluateWithArgs(ArgumentBuilder.Create());
            }

            public T EvaluateWithArgs(ArgumentBuilder arguments)
            {
                if (!this.IsValid())
                {
                    return parser.Invoke(this.expression);
                }

                // TODO: Nullable check.

                if (!this.IsConditional())
                {
                    string variable = this.expression.Substring(1, this.expression.Length - 2);
                    
                    if (!Registry.Variables.ContainsKey(variable))
                    {
                        if (arguments != null && arguments.HasArgument(variable))
                        {
                            return ((Argument<T>) arguments.GetArgument(variable)).value;
                        }
                        else
                        {
                            throw new NullReferenceException(String.Format("Variable/argument {0} has not been defined.", variable));
                        }
                    }

                    return parser.Invoke(Registry.Variables[variable].fetcher.Invoke());
                }

                // TODO: OR | operation
                bool success = true;
                foreach (var variable in variables)
                {
                    string variableRef = variable.variable;
                    EEvalCondition cond = variable.condition;
                    string toCompare = variable.compareAgainstValue;

                    if (arguments != null && arguments.HasArgument(variableRef))
                    {
                        ArgumentBuilder arg = arguments.GetArgument(variableRef);

                        VariableTypeBase type = Registry.GetVariableType(arg.GetArgumentType());
                        Variable runtimeArgument = new Variable(type);

                        if (!runtimeArgument.Compare(cond, arg.ValueToString(), toCompare))
                        {
                            success = false;
                        }

                    }
                    else
                    {
                        if (!Registry.Variables.ContainsKey(variableRef))
                        {

                            Warning(String.Format("Variable {0} has not been defined. Defaulting to false.", variableRef));
                            success = false;
                            continue;
                        }

                        Variable runtimeVariable = Registry.Variables[variableRef];
                        if (!runtimeVariable.Compare(cond, toCompare))
                        {
                            success = false;
                        }
                    }
                }

                if (success)
                    return this.ifTrue.Evaluate();
                else
                {
                    if (this.ifFalse != null)
                        return this.ifFalse.Evaluate();
                    else if (this.conditional)
                        throw new InvalidOperationException(String.Format("Cannot evaluate false expression for expression {0} as it does not exist.", expression));
                    else
                        return parser.Invoke(this.expression); // Last attempt.
                }
            }

            public bool Nullable()
            {
                return this.ifFalse == null;
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
            private EvalVariable FindConditionalVariable(string exprSub)
            {
                EvalVariable variable = null;

                foreach (var op in _operators)
                {
                    var opSymbol = op.Key;

                    if (exprSub.Contains(opSymbol))
                    {
                        variable = new EvalVariable()
                        {
                            variable = exprSub.Substring(0, exprSub.IndexOf(opSymbol)), // Name
                            condition = _operators[opSymbol], // Operator
                            compareAgainstValue = exprSub.Substring(exprSub.IndexOf(opSymbol) + 1) // Result
                        };

                        Log(variable.variable);
                        Log(opSymbol);
                        Log(variable.compareAgainstValue);

                        return variable;
                    }
                }

                string boolVarName = exprSub;
                bool not = false;
                if (boolVarName.StartsWith("!"))
                {
                    boolVarName = boolVarName.Substring(1);
                    not = true;
                }

                if (Registry.Variables.ContainsKey(boolVarName))
                {
                    variable = new EvalVariable()
                    {
                        variable = boolVarName,
                        condition = EEvalCondition.EQUAL_TO,
                        compareAgainstValue = Registry.GetVariableType<bool>().ToString(!not)
                    };
                }

                return variable;
            }

            private bool IsConditional()
            {
                return this.conditional && this.ifFalse != null && this.ifTrue != null;
            }

            private void ParseConditions()
            {
                string expr = this.expression;

                if (expr.StartsWith("{"))
                    expr = expr.Substring(1);

                if (expr.EndsWith("}"))
                    expr = expr.Substring(0, this.expression.Length - 2);

                Log("IsCondition {0}", expr);

                int occurances = FindOccurancesOf(expr, '&');

                int lastIndex = 0;
                for (int i = 0; i < occurances + 1; i++)
                {
                    char endChar = i == occurances ? '?' : '&';
                    int endIndex = expr.IndexOf(endChar);

                    if (endIndex == -1) // Not conditional.
                        break;

                    string exprSub = expr.Substring(lastIndex, endIndex - lastIndex);
                    Log("ExprSub: {0}", exprSub);

                    EvalVariable variable = FindConditionalVariable(exprSub);
                    if (variable != null)
                    {
                        variables.Add(variable);
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

                string trueEvalExpr = expr.Substring(lastIndex + 1, endTrueIndex - lastIndex - 1);
                Log("trueeval {0}", trueEvalExpr);

                Value<T> trueValue = Parse(trueEvalExpr, this.parser);
                this.ifTrue = trueValue;

                if (hasFalse)
                {
                    Log("Falseevaling");
                    string falseEvalExpr = expr.Substring(endTrueIndex + 1);
                    Log("falseeval {0}", falseEvalExpr);

                    Value<T> falseValue = Parse(falseEvalExpr, this.parser);
                    this.ifFalse = falseValue;
                }

                if (this.ifTrue != null && this.ifFalse != null)
                    this.conditional = true;
            }

            private static int FindOccurancesOf(string expr, char c)
            {
                int count = 0;

                for (var i = 0; i < expr.Length; i++)
                {
                    if (expr[i] == c)
                        count++;
                }

                return count;
            }

            class EvalVariable
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

        public sealed class Variable
        {
            public readonly VariableTypeBase type;
            public readonly Func<string> fetcher;

            public Variable(VariableTypeBase type, Func<string> fetcher)
            {
                this.type = type;
                this.fetcher = fetcher;
            }

            public Variable(VariableTypeBase type)
            {
                this.type = type;
            }

            public bool Compare(EEvalCondition condition, string lhs, string rhs)
            {
                return type.Compare(condition, lhs, rhs);
            }

            public bool Compare(EEvalCondition condition, string rhs)
            {
                if (this.fetcher == null)
                    throw new NullReferenceException("Cannot compare Variable with null fetcher, try using Compare(EEvalCondition, lhs, rhs).");

                return Compare(condition, this.fetcher.Invoke(), rhs);
            }
        }

        public class ArgumentBuilder
        {
            private Dictionary<string, ArgumentBuilder> arguments = new Dictionary<string, ArgumentBuilder>();

            protected ArgumentBuilder() { }

            public static ArgumentBuilder Create()
            {
                return new ArgumentBuilder();
            }

            public Argument<T> SetArgument<T>(string argumentName, T value)
            {
                Argument<T> argument = new Argument<T>(argumentName, value);

                arguments.Add(argumentName, argument);
                argument.arguments = arguments;

                return argument;
            }

            public Argument<T> GetArgument<T>(string argumentName)
            {
                if(HasArgument(argumentName))
                    return (Argument<T>) arguments[argumentName];

                return null;
            }

            public ArgumentBuilder GetArgument(string argumentName)
            {
                if(HasArgument(argumentName))
                    return arguments[argumentName];

                return null;
            }

            public Type GetArgumentType()
            {
                if(this.GetType().IsGenericType && this.GetType().IsSubclassOf(typeof(ArgumentBuilder)))
                {
                    return this.GetType().GetGenericArguments()[0];
                }

                throw new InvalidOperationException($"Cannot call GetArgumentType on type {this.GetType().FullName}");
            }

            public virtual string ValueToString() { throw new NotSupportedException("Cannot convert value to string as method must be implemented."); }

            public bool HasArgument(string argumentName)
            {
                return arguments.ContainsKey(argumentName);
            }
        }

        public sealed class Argument<T> : ArgumentBuilder
        {
            public string name;
            public T value;
            
            public Argument(string name, T value)
            {
                this.name = name;
                this.value = value;
            }

            public override string ValueToString()
            {
                return Registry.GetVariableType<T>().ToString(this.value);
            }
        }

        public abstract class VariableTypeBase
        {
            public abstract bool Compare(EEvalCondition condition, string lhs, string rhs);
        }

        public abstract class VariableType<T> : VariableTypeBase
        {
            public override bool Compare(EEvalCondition condition, string lhs, string rhs)
            {
                return Compare(condition, Parse(lhs), Parse(rhs));
            }

            public abstract bool Compare(EEvalCondition condition, T lhs, T rhs);
            public abstract T Parse(string str);

            public abstract string ToString(T value);
        }

        public sealed class PredefinedVariableTypes
        {
            private static readonly VariableType<uint> UINT32 = new UInt32VariableType();
            private static readonly VariableType<int> INT32 = new Int32VariableType();
            private static readonly VariableType<bool> BOOL = new BoolVariableType();
            private static readonly VariableType<float> FLOAT = new FloatVariableType();

            private static bool registered = false;
            public static void Register()
            {
                if (registered)
                    return;

                Registry.RegisterVariableType(UINT32);
                Registry.RegisterVariableType(INT32);
                Registry.RegisterVariableType(BOOL);
                Registry.RegisterVariableType(FLOAT);

                registered = true;
            }

            public sealed class UInt32VariableType : VariableType<uint>
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

            public sealed class FloatVariableType : VariableType<float>
            {
                public override bool Compare(EEvalCondition condition, float lhs, float rhs)
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

                public override float Parse(string str)
                {
                    return StringParsers.ParseFloat(str);
                }

                public override string ToString(float value)
                {
                    return value.ToString();
                }
            }

            public sealed class Int32VariableType : VariableType<int>
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

            public sealed class BoolVariableType : VariableType<bool>
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

        public sealed class PredefinedValueParsers
        {
            private static readonly ValueParser<float> FLOAT = new FloatValueParser();
            private static readonly ValueParser<uint> UINT32 = new UInt32ValueParser();
            private static readonly ValueParser<string> STRING = new StringValueParser();
            private static readonly ValueParser<int> INT32 = new Int32ValueParser();

            private static bool registered = false;

            public static void Register()
            {
                if (registered)
                    return;

                Registry.RegisterValueParser(FLOAT);
                Registry.RegisterValueParser(UINT32);
                Registry.RegisterValueParser(STRING);
                Registry.RegisterValueParser(INT32);

                registered = true;
            }

            public sealed class FloatValueParser : ValueParser<float>
            {
                public override Func<string, float> GetParser()
                {
                    return (str) => StringParsers.ParseFloat(str);
                }
            }

            public sealed class UInt32ValueParser : ValueParser<uint>
            {
                public override Func<string, uint> GetParser()
                {
                    return (str) => StringParsers.ParseUInt32(str);
                }
            }

            public sealed class StringValueParser : ValueParser<string>
            {
                public override Func<string, string> GetParser()
                {
                    return (str) => str;
                }
            }

            public sealed class Int32ValueParser : ValueParser<int>
            {
                public override Func<string, int> GetParser()
                {
                    return (str) => StringParsers.ParseSInt32(str);
                }
            }
        }
    }
}
