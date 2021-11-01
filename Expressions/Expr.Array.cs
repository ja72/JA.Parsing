using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace JA.Expressions
{
    public partial record Expr
    {
        #region Static
        static Expr[] CreateVector(int size)
        {
            var result = new Expr[size];
            System.Array.Fill(result, Zero);
            return result;
        }
        static Expr[] CreateVector(int size, Func<int, Expr> initializer)
        {
            var result = new Expr[size];
            if (initializer!=null)
            {
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = initializer(i);
                }
            }
            return result;
        }
        static Expr[] CreateVector(int size, Func<int, string> initializer)
        {
            var result = new Expr[size];
            if (initializer!=null)
            {
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = Parse(initializer(i));
                }
            }
            return result;
        }

        static Expr[][] CreateJagged(int rows, int columns)
        {
            var result = new Expr[rows][];
            for (int i = 0; i < rows; i++)
            {
                var row = new Expr[columns];
                System.Array.Fill(row, Zero);
                result[i] = row;
            }
            return result;
        }
        static Expr[][] CreateJagged(int rows, int columns, Func<int, int, string> initializer)
        {
            var result = new Expr[rows][];
            for (int i = 0; i < rows; i++)
            {
                var row = new Expr[columns];
                if (initializer!=null)
                {
                    for (int j = 0; j < row.Length; j++)
                    {
                        row[j] = Parse(initializer(i, j));
                    }
                }
                result[i] = row;
            }
            return result;
        }

        static Expr[][] CreateJagged(int rows, int columns, Func<int, int, Expr> initializer)
        {
            var result = new Expr[rows][];
            for (int i = 0; i < rows; i++)
            {
                var row = new Expr[columns];
                if (initializer!=null)
                {
                    for (int j = 0; j < row.Length; j++)
                    {
                        row[j] = initializer(i, j);
                    }
                }
                result[i] = row;
            }
            return result;
        }

        #endregion

        #region Factory

        public static Expr Vector(int size)
            => Array(CreateVector(size));
        public static Expr Vector(int size, Func<int, Expr> initializer)
            => Array(CreateVector(size, initializer));
        public static Expr Vector(int size, Func<int, string> initializer)
            => Array(CreateVector(size, initializer));

        public static Expr Vector(double[] elements)
            => Array(elements.Select((x) => Const(x)));
        public static Expr Vector(string[] elements)
            => Array(elements.Select((x) => Parse(x)));

        public static Expr Array(IEnumerable<Expr> list)
        {
            return Array(list.ToArray());
        }

        public static Expr Array(params Expr[] array)
        {
            if (array.Length==0)
            {
                return 0;
            }
            if (array.Length==1)
            {
                return array[0];
            }
            for (int k = 0; k < array.Length; k++)
            {
                if (array[k].IsArray(out var row))
                {
                    int cols = row.Length;
                    var matrix = new Expr[array.Length][];
                    for (int i = 0; i < matrix.Length; i++)
                    {
                        if (array[i].IsArray(out row))
                        {
                            matrix[i] = row;
                        }
                        else
                        {
                            row = new Expr[cols];
                            row[i] = array[i];
                            matrix[i] = row;
                        }
                    }
                    return Matrix(matrix);
                }
            }
            return new ArrayExpr(array);
        }
        public static Expr Matrix(int rows, int columns)
            => Matrix(CreateJagged(rows, columns));
        public static Expr Matrix(int rows, int columns, Func<int, int, Expr> initializer)
            => Matrix(CreateJagged(rows, columns, initializer));
        public static Expr Matrix(int rows, int columns, Func<int, int, string> initializer)
            => Matrix(CreateJagged(rows, columns, initializer));
        public static Expr Matrix(double[][] matrix)
        {
            var expr = new Expr[matrix.Length][];
            for (int i = 0; i < expr.Length; i++)
            {
                expr[i] = matrix[i].Select((item) => Const(item)).ToArray();
            }
            return Matrix(expr);
        }
        public static Expr Matrix(string[][] matrix)
        {
            var expr = new Expr[matrix.Length][];
            for (int i = 0; i < expr.Length; i++)
            {
                expr[i] = matrix[i].Select((item) => Parse(item)).ToArray();
            }
            return Matrix(expr);
        }

        public static Expr Matrix(Expr[][] matrix)
        {
            if (matrix.Length==0)
            {
                return 0;
            }
            if (matrix.Length==1)
            {
                return Array(matrix[0]);
            }
            return new ArrayExpr(matrix);
        }

        public static Expr[][] Zeros(int rows, int columns)
            => CreateJagged(rows, columns);
        public static Expr[][] Identity(int size)
            => Diagonal(size, One);
        public static Expr[][] Diagonal(int size, Expr value)
            => CreateJagged(size, size, (i, j) => i==j ? value : Zero);
        public static Expr[][] Diagonal(params Expr[] diagonals)
            => CreateJagged(diagonals.Length, diagonals.Length, (i, j) => i==j ? diagonals[i] : Zero);

        #endregion

        #region Vector Functions
        public static Expr Transpose(Expr matrix)
        {
            if (matrix.IsMatrix(out var array2))
            {
                return Matrix(Transpose(array2));
            }
            return matrix;
        }
        public static Expr[][] Transpose(Expr[][] array2)
        {
            int n = array2.Length;
            int m = n>0 ? array2[0].Length : 0;
            var result = new Expr[m][];
            for (int i = 0; i < result.Length; i++)
            {
                var row = new Expr[n];
                for (int j = 0; j < row.Length; j++)
                {
                    row[j] = array2[j][i];
                }
                result[i] = row;
            }
            return result;
        }
        public static Expr Distance(Expr A, Expr B) => Norm(B-A);
        public static Expr Hypot(Expr A, Expr B) => Norm(A, B);
        public static Expr Norm(Expr A)
        {
            if (A.IsArray(out var arrayA))
            {
                return Norm(arrayA);
            }
            return Abs(A);
        }
        public static Expr Norm(params Expr[] A) => Sqrt(Dot(A, A));
        public static Expr Dot(Expr A, Expr B)
        {
            if (IsVectorizable(A, B, out var arrayA, out var arrayB))
            {
                return Dot(arrayA, arrayB);
            }
            return Transpose(A)*B;
        }
        public static Expr Dot(Expr[] vectorA, Expr[] vectorB)
        {
            if (vectorA.Length==vectorB.Length)
            {
                Expr sum = 0;
                for (int i = 0; i < vectorA.Length; i++)
                {
                    sum += Transpose(vectorA[i])*vectorB[i];
                }
                return sum;
            }
            throw new ArgumentException("Vectors must be of equal length.", nameof(vectorB));
        }
        public static Expr Outer(Expr vectorA, Expr vectorB)
        {
            if (IsVectorizable(vectorA, vectorB, out var arrayA, out var arrayB))
            {
                return Matrix(Outer(arrayA, arrayB));
            }
            return vectorA*Transpose(vectorB);
        }
        public static Expr[][] Outer(Expr[] vectorA, Expr[] vectorB)
        {
            var result = new Expr[vectorA.Length][];
            for (int i = 0; i < result.Length; i++)
            {
                var row = new Expr[vectorB.Length];
                for (int j = 0; j < row.Length; j++)
                {
                    row[j] = vectorA[i]*Transpose(vectorB[j]);
                }
                result[i] = row;
            }
            return result;
        }
        public static Expr Cross(Expr vectorA, Expr vectorB)
        {
            if (IsVectorizable(vectorA, vectorB, out var arrayA, out var arrayB))
            {
                return Cross(arrayA, arrayB);
            }
            else if (vectorA.IsVector(out arrayA))
            {
                return Cross(arrayA, vectorB);
            }
            else if (vectorB.IsVector(out arrayB))
            {
                return Cross(vectorA, arrayB);
            }
            throw new NotSupportedException("Invalid cross product dimensions.");
        }
        public static Expr Cross(Expr A, Expr[] B)
        {
            if (B.Length==2)
            {
                return Array(-A*B[1], A*B[0]);
            }
            throw new NotSupportedException("Invalid cross product dimensions.");
        }
        public static Expr Cross(Expr[] A, Expr B)
        {
            if (A.Length==2)
            {
                return Array(A[1]*B, -A[0]*B);
            }
            throw new NotSupportedException("Invalid cross product dimensions.");
        }
        public static Expr Cross(Expr[] A, Expr[] B)
        {
            if (A.Length == 2 && B.Length == 2)
            {
                return A[0]*B[1] - A[1]*B[0];
            }
            else if (A.Length==1 && B.Length == 2)
            {
                return Cross(A[0], B);
            }
            else if (A.Length==2 && B.Length == 1)
            {
                return Cross(A, B[0]);
            }
            else if (A.Length==3 && B.Length==3)
            {
                return Array(
                    A[1]*B[2] - A[2]*B[1],
                    A[2]*B[0] - A[0]*B[2],
                    A[0]*B[1] - A[1]*B[0]);
            }
            throw new NotSupportedException("Invalid cross product dimensions.");
        }

        public static Expr Cross(Expr vector)
        {
            if (vector.IsVector(out var vectExpr))
            {
                return Cross(vectExpr);
            }
            throw new ArgumentException("Unknown cross product operator argument.", nameof(vector));
        }

        public static Expr Cross(Expr[] A)
        {
            if (A.Length==3)
            {
                return Matrix(new[] {
                    new [] { 0.0, -A[2], A[1] },
                    new [] { A[2], 0.0, -A[0] },
                    new [] { -A[1], A[0], 0.0 } });
            }
            throw new ArgumentOutOfRangeException(nameof(A), "Invalid cross product operator dimensions.");
        }

        public static Expr Vector(int size, string name)
        {
            var vector = new Expr[size];
            for (int i = 0; i < size; i++)
            {
                vector[i] = $"{name}_{i+1}";
            }
            return Array(vector);
        }


        public static Expr Product(Expr A, Expr B)
        {
            if (A.IsMatrix(out var matrixA) && B.IsMatrix(out var matrixB))
            {
                return Matrix(Product(matrixA, matrixB));
            }
            else if (A.IsMatrix(out matrixA) && B.IsVector(out var vectorB))
            {
                return Array(Product(matrixA, vectorB));
            }
            else if (A.IsVector(out var vectorA) && B.IsMatrix(out matrixB))
            {
                return Array(Product(vectorA, matrixB));
            }
            return A*B;
        }

        public static Expr[] Product(Expr[] vector, Expr[][] matrix)
            => Product(Transpose(matrix), vector);
        public static Expr[] Product(Expr[][] matrix, Expr[] vector)
        {
            var result = new Expr[matrix.Length];
            for (int i = 0; i < result.Length; i++)
            {
                Expr sum = 0;
                var Arow = matrix[i];
                for (int k = 0; k < vector.Length; k++)
                {
                    sum += Arow[k] * vector[k];
                }
                result[i] = sum;
            }
            return result;
        }
        public static Expr[][] Product(Expr[][] left, Expr[][] right)
        {
            int n = right.Length;
            int m = n>0 ? right[0].Length : 0;
            var result = new Expr[left.Length][];
            for (int i = 0; i < result.Length; i++)
            {
                var row = new Expr[m];
                var Arow = left[i];
                for (int j = 0; j < row.Length; j++)
                {
                    Expr sum = 0;
                    for (int k = 0; k < Arow.Length; k++)
                    {
                        sum += Arow[k] * right[k][j];
                    }
                    row[j] = sum;
                }
                result[i] = row;
            }
            return result;
        }

        public static Expr Solve(Expr A, Expr b)
        {
            if (A.IsMatrix(out var matrixA) && b.IsMatrix(out var matrixB))
            {
                return Matrix(Solve(matrixA, matrixB));
            }
            if (A.IsMatrix(out matrixA) && b.IsArray(out var vectorB))
            {
                return Array(Solve(matrixA, vectorB));
            }
            return b/A;
        }


        public static Expr[] Solve(Expr[][] Elements, Expr[] vector)
        {
            int n = Elements.Length;
            int m = n>0 ? Elements[0].Length : 0;
            if (n != vector.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(vector), "Mismatch between matrix rows and vector size.");
            }

            if (n==1 && m==1 && vector.Length==1)
            {
                return new Expr[] { vector[0] / Elements[0][0] };
            }
            if (GetBlock(Elements, out var A, out var b, out var c, out var d))
            {
                if (GetBlock(vector, out var u, out var y))
                {
                    var Au = Solve(A, u);
                    var Ab = Solve(A, b);

                    Expr x = (y - Dot(c, Au))/(d - Dot(c, Ab));
                    Expr[] v = (Array(Au) - x*Array(Ab)).ToArray();

                    var result = new Expr[n];
                    System.Array.Copy(v, result, result.Length-1);
                    result[^1] = x;

                    return result;
                }
            }
            throw new ArgumentException("Invalid inputs.", nameof(vector));
        }
        public static Expr[][] Solve(Expr[][] Elements, Expr[][] matrix)
        {
            int n = Elements.Length;
            int m = n>0 ? Elements[0].Length : 0;
            int n2 = matrix.Length;
            int m2 = n2>0 ? matrix[0].Length : 0;
            if (n != n2)
            {
                throw new ArgumentOutOfRangeException(nameof(matrix), "Mismatch between matrix rows.");
            }

            if (n==1 && m==1 && n2==1 && m2==1)
            {
                var result = CreateJagged(1,1);
                result[0][0] = matrix[0][0]/Elements[0][0];
                return result;
            }

            if (GetBlock(Elements, out var A, out var b, out var c, out var d))
            {
                if (GetBlock(matrix, out var U, out var u, out var h, out var y))
                {
                    var Au = Solve(A, u);
                    var Ab = Solve(A, b);
                    Expr x = (y - Dot(c, Au))/(d - Dot(c,Ab));

                    Expr[][] Abc = (Matrix(A) - Matrix(Outer(b, c))).ToMatrix();
                    Expr[][] V = Solve(Abc, (d*Matrix(U) - Matrix(Outer(b,h))).ToMatrix());
                    Expr[] v = Solve(A, (Array(u) - x*Array(b)).ToArray());
                    Expr[] g = ((Array(h) - Array(Product(c, V)))/d).ToArray();

                    return Block(V, v, g, x);
                }
            }
            throw new ArgumentException("Invalid inputs.", nameof(matrix));
        }


        #endregion

        #region Slicing
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Expr Slice(Expr[] vector, Index index)
            => vector[index];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Expr[] Slice(Expr[] vector, Range range)
            => vector[range];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Expr Slice(Expr[][] matrix, Index row, Index column)
            => matrix[row][column];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Expr[] Slice(Expr[][] matrix, Range rows, Index column)
        {
            var slice = matrix[rows];
            var result = new Expr[slice.Length];
            for (int i = 0; i < slice.Length; i++)
            {
                result[i] = slice[i][column];
            }
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Expr[] Slice(Expr[][] matrix, Index row, Range columns)
        {
            var slice = matrix[row];
            var result = slice[columns];
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Expr[][] Slice(Expr[][] matrix, Range rows, Range columns)
        {
            var slice = matrix[rows];
            for (int i = 0; i < slice.Length; i++)
            {
                slice[i] = slice[i][columns];
            }
            return slice;
        }

        #endregion        

        #region Block Vectors/Matrix
        public static Expr[] Block(Expr[] vector, Expr scalar)
        {
            var result = new Expr[vector.Length+1];
            System.Array.Copy(vector, result, vector.Length);
            result[^1] = scalar;
            return result;
        }
        public static Expr[][] Block(Expr[][] A, Expr[] b, Expr[] c, Expr d)
        {
            int n = A.Length;
            int m = n>0 ? A[0].Length : 0;
            var result = CreateJagged(n+1, m+1);
            for (int i = 0; i < n; i++)
            {
                System.Array.Copy(A[i], result[i], m);
                result[i][^1] = b[i];
            }
            System.Array.Copy(c, result[^0], m);
            result[^1][^1] = d;

            return result;
        }

        static bool GetBlock(Expr[] vector, out Expr[] V, out Expr y)
        {
            if (vector.Length>1)
            {
                V = Slice(vector, ..^1);
                y = Slice(vector, ^1);
                return true;
            }
            V = null;
            y = Zero;
            return false;
        }

        static bool GetBlock(Expr[][] matrix, out Expr[][] A, out Expr[] b, out Expr[] c, out Expr d)
        {
            int n = matrix.Length;
            int m = n>0 ? matrix[0].Length : 0;
            if (n>1 && m>1)
            {
                A = Slice(matrix, ..^1, ..^1);
                b = Slice(matrix, ..^1, ^1);
                c = Slice(matrix, ^1, ..^1);
                d = Slice(matrix, ^1, ^1);
                return true;
            }
            A = null;
            b = null;
            c = null;
            d = 0;
            return false;
        }


        #endregion

    }
}
