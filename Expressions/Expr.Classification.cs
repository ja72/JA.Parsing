using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JA.Expressions
{
    public partial record Expr
    {
        #region Classification
        public bool IsAssign(out Expr left, out Expr right)
        {
            if (this is AssignExpr assignExpr)
            {
                left = assignExpr.Left;
                right = assignExpr.Right;
                return true;
            }
            if (IsArray(out var arrayExpr))
            {
                if (MakeAssignment(arrayExpr, out var lhsExpr, out var rhsExpr))
                {
                    left = Array(lhsExpr);
                    right = Array(rhsExpr);
                    return true;
                }
            }
            left = null;
            right = null;
            return false;
        }
        public static bool MakeAssignment(Expr expr, out Expr lhsExpr, out Expr rhsExpr)
        {
            if (expr.IsAssign(out lhsExpr, out rhsExpr))
            {
                return true;
            }
            if (expr.IsArray(out var arrayExpr))
            {
                var ok = MakeAssignment(arrayExpr, out var lhsArray, out var rhsArray);
                lhsExpr = Array(lhsArray);
                rhsExpr = Array(rhsArray);
                return ok;
            }
            lhsExpr = expr;
            rhsExpr = 0;
            return true;
        }
        public static bool MakeAssignment(Expr[] arrayExpr, out Expr[] lhsExpr, out Expr[] rhsExpr)
        {
            lhsExpr = new Expr[arrayExpr.Length];
            rhsExpr = new Expr[arrayExpr.Length];
            bool ok = false;
            for (int i = 0; i < arrayExpr.Length; i++)
            {
                if (arrayExpr[i].IsAssign(out var lhs, out var rhs))
                {
                    ok = true;
                    lhsExpr[i] = lhs;
                    rhsExpr[i] = rhs;
                }
                else if (MakeAssignment(arrayExpr[i], out lhs, out rhs))
                {
                    lhsExpr[i] = lhs;
                    rhsExpr[i] = rhs;
                }
            }

            return ok;
        }
        public bool IsConstant(out double value, bool includeNamedConst = false)
        {
            if (this is NamedConstExpr nConstEx && includeNamedConst)
            {
                value = nConstEx.Value;
                return true;
            }
            if (this is ConstExpr vExpr && !(this is NamedConstExpr))
            {
                value = vExpr.Value;
                return true;
            }
            if (this is VariableExpr symExpr)
            {
                if (ConstOp.IsConst(symExpr.Name, out var op))
                {
                    value = op.Value;
                    return true;
                }
                if (Parameters.ContainsKey(symExpr.Name))
                {
                    value = Parameters[symExpr.Name];
                    return true;
                }
            }
            if (IsUnary(out var unaryName, out var unaryEx))
            {
                if (unaryEx.IsConstant(out var negX, includeNamedConst))
                {
                    if (UnaryOp.IsUnary(unaryName, out var op))
                    {
                        value = op.Function(negX);
                        return true;
                    }
                }
            }
            value = 0;
            return false;
        }
        public bool IsNamedConstant(out string symbol, out double value)
        {
            if (this is NamedConstExpr namedExpr && !string.IsNullOrEmpty(namedExpr.Name))
            {
                symbol = namedExpr.Name;
                value = namedExpr.Value;
                return true;
            }
            if (this is VariableExpr symExpr)
            {
                if (ConstOp.IsConst(symExpr.Name, out var op))
                {
                    symbol = symExpr.Name;
                    value = op.Value;
                    return true;
                }
                if (Parameters.ContainsKey(symExpr.Name))
                {
                    symbol = symExpr.Name;
                    value = Parameters[symExpr.Name];
                    return true;
                }
            }
            symbol = string.Empty;
            value = 0;
            return false;
        }
        public bool IsNamedConstant(string symbol, out double value)
        {
            if (this is NamedConstExpr namedExpr && namedExpr.Name == symbol)
            {
                value = namedExpr.Value;
                return true;
            }
            if (this is VariableExpr symExpr && symExpr.Name == symbol)
            {
                if (ConstOp.IsConst(symExpr.Name, out var op))
                {
                    value = op.Value;
                    return true;
                }
                if (Parameters.ContainsKey(symExpr.Name))
                {
                    value = Parameters[symExpr.Name];
                    return true;
                }
            }
            value = 0;
            return false;
        }
        public bool IsSymbol(out string symbol)
        {
            if (this is VariableExpr symExpr)
            {
                symbol = symExpr.Name;
                return true;
            }
            if (this is ConstExpr valExpr)
            {
                foreach (var item in KnownConstDictionary.Defined)
                {
                    if (item.Value == valExpr.Value)
                    {
                        symbol = item.Identifier;
                        return true;
                    }
                }
            }
            symbol = string.Empty;
            return false;
        }
        public bool IsSymbol(string symbol)
        {
            if (this is VariableExpr symExpr && symExpr.Name == symbol)
            {
                return true;
            }
            if (this is ConstExpr valExpr)
            {
                foreach (var item in KnownConstDictionary.Defined)
                {
                    if (item.Value == valExpr.Value)
                    {
                        return symbol == item.Identifier;
                    }
                }
            }
            return false;
        }
        public bool IsUnary(out string operation, out Expr argument)
        {
            if (this is UnaryExpr unaryExpr)
            {
                operation = unaryExpr.Op.Identifier;
                argument = unaryExpr.Argument;
                return true;
            }
            operation = string.Empty;
            argument = null;
            return false;
        }
        public bool IsUnary(string operation, out Expr argument)
        {
            if (this is UnaryExpr unaryExpr && unaryExpr.Op.Identifier == operation)
            {
                argument = unaryExpr.Argument;
                return true;
            }
            argument = null;
            return false;
        }
        public bool IsBinary(out string operation, out Expr left, out Expr right)
        {
            if (this is BinaryExpr binaryExpr)
            {
                operation = binaryExpr.Op.Identifier;
                left = binaryExpr.Left;
                right = binaryExpr.Right;
                return true;
            }
            operation = string.Empty;
            left = null;
            right = null;
            return false;
        }
        public bool IsBinary(string operation, out Expr left, out Expr right)
        {
            if (this is BinaryExpr binaryExpr && binaryExpr.Op.Identifier == operation)
            {
                left = binaryExpr.Left;
                right = binaryExpr.Right;
                return true;
            }
            left = null;
            right = null;
            return false;
        }
        public bool IsFactor(out double factor, out Expr argument)
        {
            if (IsBinary("*", out var mul_left, out var mul_arg))
            {
                if (mul_left.IsConstant(out factor, true))
                {
                    argument = mul_arg;
                    return true;
                }
            }
            if (IsBinary("/", out var div_left, out var div_arg))
            {
                if (div_left.IsConstant(out factor, true))
                {
                    argument = 1/div_arg;
                    return true;
                }
            }
            if (IsUnary("-", out var neg_arg))
            {
                factor = -1;
                argument= neg_arg;
                return true;
            }
            if (IsUnary("+", out var pos_arg))
            {
                factor = 1;
                argument= pos_arg;
                return true;
            }
            if (IsUnary(out var _, out var _))
            {
                factor = 1;
                argument= this;
                return true;
            }
            factor = 0;
            argument = null;
            return false;
        }

        public bool IsScalar()
        {
            return !(this is ArrayExpr);
        }

        /// <summary>Checks if expression is an array expression</summary>
        /// <param name="elements">Returns the elements of the array</param>
        /// <param name="orMatrix">Check if it could be a matrix also (default)
        /// or exclusively a vector (all elements are scalar).</param>
        /// <returns>
        /// Returns true if expression is an array, false otherwise.
        /// </returns>
        public bool IsVector(out Expr[] elements, bool orMatrix = false)
        {
            if (this is ArrayExpr arrayExpr)
            {
                elements = arrayExpr.Elements;
                return orMatrix || !elements.Any((item) => item.IsArray(out _));
            }
            elements = null;
            return false;
        }
        public bool IsArray(out Expr[] elements)
        {
            if (this is ArrayExpr arrayExpr)
            {
                elements = arrayExpr.Elements;
                return true;
            }
            elements = null;
            return false;
        }
        public bool IsMatrix(out Expr[][] elements)
        {
            if (IsArray(out var rows))
            {
                elements = new Expr[rows.Length][];
                for (int i = 0; i < elements.Length; i++)
                {
                    if (rows[i].IsArray(out var items))
                    {
                        elements[i] = items;
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
            elements = null;
            return false;
        }

        static bool ExtractVector(Expr[] exprVector, out double[] elements)
        {
            elements = new double[exprVector.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                if (exprVector[i].IsConstant(out var x, true))
                {
                    elements[i] =x;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsConstVector(out Vector vector)
        {
            vector  = null;
            if (IsVector(out var exprVector))
            {
                if (ExtractVector(exprVector, out var row))
                {
                    vector = new Vector(row);
                }
                return true;
            }
            return false;
        }

        public bool IsConstMatrix(out Matrix matrix)
        {
            matrix = null;
            if (IsMatrix(out var exprMatrix))
            {
                double[][] elements = new double[exprMatrix.Length][];
                for (int i = 0; i < elements.Length; i++)
                {
                    if (ExtractVector(exprMatrix[i], out var row))
                    {
                        elements[i] = row;
                    }
                    else
                    {
                        return false;
                    }
                }
                matrix = new Matrix(elements);
                return true;
            }
            return false;
        }

        #endregion

        #region Vectorization
        internal static bool IsVectorizable(Expr[] leftArray, Expr[] rightArray)
        {
            return leftArray.Length == rightArray.Length;
        }
        internal static bool MakeVectorizable(ref Expr[] leftArray, ref Expr[] rightArray, out int count)
        {
            int lcount = leftArray.Length;
            int rcount = rightArray.Length;
            if (lcount < rcount)
            {
                var temp = new Expr[rcount];
                int index = 0;
                while (index<temp.Length)
                {
                    System.Array.Copy(leftArray, 0, temp, index, Math.Min(lcount, temp.Length-index));
                    index += lcount;
                }
                leftArray = temp;
                lcount = temp.Length;
            }
            if (lcount > rcount)
            {
                var temp = new Expr[lcount];
                var index = 0;
                while (index<temp.Length)
                {
                    System.Array.Copy(rightArray, 0, temp, index, Math.Min(rcount, temp.Length-index));
                    index += rcount;
                }
                rightArray = temp;
                rcount = temp.Length;
            }
            count = Math.Max(lcount, rcount);
            return true;
        }
        internal static bool IsVectorizable(Expr left, Expr right, out Expr[] leftArray, out Expr[] rightArray, bool makeConformal = false)
        {
            if (left.IsArray(out leftArray) && right.IsArray(out rightArray))
            {
                return IsVectorizable(leftArray, rightArray);
            }
            else if (left.IsArray(out leftArray) && makeConformal)
            {
                rightArray = new Expr[leftArray.Length];
                System.Array.Fill(rightArray, right);
                return MakeVectorizable(ref leftArray, ref rightArray, out _);
            }
            else if (right.IsArray(out rightArray) && makeConformal)
            {
                leftArray = new Expr[rightArray.Length];
                System.Array.Fill(leftArray, left);
                return MakeVectorizable(ref leftArray, ref rightArray, out _);
            }
            // both scalar
            leftArray = null;
            rightArray = null;
            return false;
        }
        static Expr[] UnaryVectorOp(UnaryOp op, Expr[] a_array)
        {
            Expr[] result = new Expr[a_array.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = Unary(op, a_array[i]);
            }
            return result;
        }

        static Expr[] UnaryVectorOp(Func<Expr, Expr> op, Expr[] a_array)
        {
            Expr[] result = new Expr[a_array.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = op(a_array[i]);
            }
            return result;
        }
        static Expr[] BinaryVectorOp(BinaryOp op, Expr[] a_array, Expr[] b_array)
        {
            if (a_array.Length != b_array.Length)
            {
                throw new ArgumentException("Unequal lengths for vectors", nameof(b_array));
            }
            Expr[] result = new Expr[a_array.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = Binary(op, a_array[i], b_array[i]);
            }
            return result;
        }
        static Expr[] BinaryVectorOp(Func<Expr, Expr, Expr> op, Expr[] a_array, Expr[] b_array)
        {
            if (a_array.Length != b_array.Length)
            {
                throw new ArgumentException("Unequal lengths for vectors", nameof(b_array));
            }
            Expr[] result = new Expr[a_array.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = op(a_array[i], b_array[i]);
            }
            return result;
        }
        #endregion

    }
}
