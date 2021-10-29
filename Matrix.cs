using System;
using System.Linq;

namespace JA
{
    public record Matrix(double[][] Elements) : IQuantity
    {        
        public static readonly Matrix Empty = Array.Empty<double[]>();
        public static implicit operator Matrix(double[][] matrix) => new(matrix);
        public static implicit operator double[][](Matrix matrix) => matrix.Elements;
        public Matrix(Vector[] matrix) :
            this(matrix.Select(row => row.Elements).ToArray())
        { }
        public Matrix(int rows, int columns)
            : this(CreateJagged(rows,columns))
        { }
        public Matrix(int rows, int columns, Func<int,int,double> initializer)
            : this(CreateJagged(rows, columns, initializer))
        { }
        static double[][] CreateJagged(int rows, int columns, Func<int,int,double> initializer = null)
        {
            var result = new double[rows][];
            for (int i = 0; i < rows; i++)
            {
                var row = new double[columns];
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

        public static Matrix Block(Matrix A, Vector b, Vector c, double d) 
        {
            var result = CreateJagged(A.Rows+1, A.Columns+1);
            for (int i = 0; i < A.Rows; i++)
            {
                Array.Copy(A.Elements[i], result[i], A.Columns);
                result[i][^1] = b.Elements[i];
            }
            Array.Copy(c.Elements, result[^0], A.Columns);
            result[^1][^1] = d;

            return new Matrix(result);
        }

        public bool GetBlock(out Matrix A, out Vector b, out Vector c, out double d) 
        {
            if (Rows>1 && Columns>1)
            {
                A = this[..^1, ..^1];
                b = this[..^1, ^1];
                c = this[^1, ..^1];
                d = this[^1, ^1];
                return true;
            }
            A = null;
            b = null;
            c = null;
            if (Rows==1 && Columns==1)
            {
                d = Elements[0][0];
            }
            else
            {
                d = 0;
            }
            return false;
        }

        public int Rank { get => 2; }
        public int Rows { get => Elements.Length; }
        public int Columns { get => Elements.Length>0 ? Elements[0].Length : 0; }
        double IQuantity.Value { get => 0; }
        double[] IQuantity.Array { get => null; }
        double[][] IQuantity.Array2 { get => Elements; }
        /// <summary>
        /// Reference an element of the matrix
        /// </summary>
        /// <param name="row">The row index.</param>
        /// <param name="column">The column index.</param>
        public ref double this[Index row, Index column] 
            => ref Elements[row][column];
        /// <summary>
        /// Extract a sub-matrix.
        /// </summary>
        /// <param name="rows">The range of rows.</param>
        /// <param name="columns">The range of columns.</param>
        public double[][] this[Range rows, Range columns]
        {
            get
            {
                var slice = Elements[rows];
                for (int i = 0; i < slice.Length; i++)
                {
                    slice[i] = slice[i][columns];
                }
                return slice;
            }
        }

        /// <summary>
        /// Extract a row sub-vector.
        /// </summary>
        /// <param name="row">The row index.</param>
        /// <param name="columns">The range of columns.</param>
        public double[] this[Index row, Range columns]
        {
            get
            {
                var slice = Elements[row];
                var result = slice[columns];
                return result;
            }
        }

        /// <summary>
        /// Extract a column sub-vector.
        /// </summary>
        /// <param name="rows">The range of rows.</param>
        /// <param name="column">The column index.</param>
        public double[] this[Range rows, Index column]
        {
            get
            {
                var slice = Elements[rows];
                var result = new double[slice.Length];
                for (int i = 0; i < slice.Length; i++)
                {
                    result[i] = slice[i][column];
                }
                return result;
            }
        }

        public ReadOnlySpan<double[]> AsSpan() => AsSpan(Range.All);
        public ReadOnlySpan<double[]> AsSpan(Range rows)
        {            
            var (offset, length)= rows.GetOffsetAndLength(Elements.Length);
            return new ReadOnlySpan<double[]>(Elements, offset, length);
        }


        #region Formatting
        public override string ToString() => ToString("g");
        public string ToString(string formatting) => ToString(formatting, null);
        public string ToString(string formatting, IFormatProvider formatProvider)
            => $"[{string.Join(",", Elements.Select(row => ((Vector)row).ToString(formatting, formatProvider)))}]";
        #endregion

        #region Algebra

        public static Matrix Zero(int rows, int columns)
        {
            return new Matrix(rows, columns);
        }
        public static Matrix Identity(int rows, int columns)
        {
            return new Matrix(rows, columns, (i,j)=> i==j ? 1 : 0);
        }
        public static Matrix Identity(int size) => Identity(size, size);

        public static Matrix Add(Matrix A, Matrix B)
        {
            var result = new double[A.Rows][];
            for (int i = 0; i < result.Length; i++)
            {
                var row = new double[A.Columns];
                var Arow = A.Elements[i];
                var Brow = B.Elements[i];
                for (int j = 0; j < row.Length; j++)
                {
                    row[j] = Arow[j] + Brow[j];
                }
                result[i] = row;
            }
            return new Matrix(result);
        }

        public static Matrix Subtract(Matrix A, Matrix B)
        {
            var result = new double[A.Rows][];
            for (int i = 0; i < result.Length; i++)
            {
                var row = new double[A.Columns];
                var Arow = A.Elements[i];
                var Brow = B.Elements[i];
                for (int j = 0; j < row.Length; j++)
                {
                    row[j] = Arow[j] - Brow[j];
                }
                result[i] = row;
            }
            return new Matrix(result);
        }

        public static Matrix Scale(double factor, Matrix A)
        {
            var result = new double[A.Rows][];
            for (int i = 0; i < result.Length; i++)
            {
                var row = new double[A.Columns];
                var Arow = A.Elements[i];
                for (int j = 0; j < row.Length; j++)
                {
                    row[j] = factor * Arow[j];
                }
                result[i] = row;
            }
            return new Matrix(result);
        }

        public static Vector Product(Vector a, Matrix B) => Product(B.Transpose(), a);
        public static Vector Product(Matrix A, Vector b)
        {
            var result = new double[A.Rows];
            for (int i = 0; i < result.Length; i++)
            {
                double sum = 0;
                var Arow = A.Elements[i];
                for (int k = 0; k < b.Elements.Length; k++)
                {
                    sum += Arow[k] * b.Elements[k];
                }
                result[i] = sum;
            }
            return new Vector(result);
        }

        public static Matrix Product(Matrix A, Matrix B)
        {
            var result = new double[A.Rows][];
            for (int i = 0; i < result.Length; i++)
            {
                var row = new double[B.Columns];
                var Arow = A.Elements[i];
                for (int j = 0; j < row.Length; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < Arow.Length; k++)
                    {
                        sum += Arow[k] * B.Elements[k][j];
                    }
                    row[j] = sum;
                }
                result[i] = row;
            }
            return new Matrix(result);
        }

        public Matrix Transpose()
        {
            int n = Rows;
            int m = Columns;
            var result = new double[m][];
            for (int i = 0; i < result.Length; i++)
            {
                var row = new double[n];
                for (int j = 0; j < row.Length; j++)
                {
                    row[j] = Elements[j][i];
                }
                result[i] = row;
            }
            return result;
        }

        public Matrix Inverse() => Solve(Identity(Rows, Columns));
        public Vector Solve(Vector vector)
        {
            if (Rows != vector.Size)
            {
                throw new ArgumentOutOfRangeException(nameof(vector), "Mismatch between matrix rows and vector size.");
            }

            if (Rows==1 && Columns==1 && vector.Size==1)
            {
                return new Vector(vector.Elements[0]/ Elements[0][0]);
            }
            if (GetBlock(out var A, out var b, out var c, out var d))
            {
                Vector u = vector[..^1];
                double y = vector[^1];

                var Au = A.Solve(u);
                var Ab = A.Solve(b);

                double x = (y - Vector.Dot(c, Au))/(d - Vector.Dot(c,Ab));
                Vector v = A.Solve(u - x*b);

                var result = new double[Rows];
                Array.Copy(v.Elements, result, result.Length-1);
                result[^1] = x;

                return result;
            }
            throw new ArgumentException("Invalid inputs.", nameof(vector));
        }

        public Matrix Solve(Matrix matrix)
        {
            if (Rows != matrix.Rows)
            {
                throw new ArgumentOutOfRangeException(nameof(matrix), "Mismatch between matrix rows.");
            }

            if (Rows==1 && Columns==1 && matrix.Rows==1 && matrix.Columns==1)
            {
                var result = CreateJagged(1,1);
                result[0][0] = matrix.Elements[0][0]/this.Elements[0][0];
                return new Matrix(result);
            }

            if (GetBlock(out var A, out var b, out var c, out var d))
            {
                if (matrix.GetBlock(out var U, out var u, out var h, out var y))
                {
                    var Au = A.Solve(u);
                    var Ab = A.Solve(b);
                    double x = (y - Vector.Dot(c, Au))/(d - Vector.Dot(c,Ab));

                    Matrix Abc = A - Vector.Outer(b, c);
                    Matrix V = Abc.Solve(d*U - Vector.Outer(b,h));
                    Vector v = A.Solve( u - x*b);
                    Vector g = (h - c*V)/d;

                    return Block(V, v, g, x);
                }
            }
            throw new ArgumentException("Invalid inputs.", nameof(matrix));
        }
        #endregion

        #region Operators
        public static Matrix operator +(Matrix A, Matrix B) => Add(A, B);
        public static Matrix operator -(Matrix A, Matrix B) => Subtract(A, B);
        public static Matrix operator -(Matrix A) => Scale(-1, A);
        public static Matrix operator *(double factor, Matrix B) => Scale(factor, B);
        public static Matrix operator *(Matrix A, double factor) => Scale(factor, A);
        public static Vector operator *(Matrix A, Vector b) => Product(A, b);
        public static Matrix operator *(Matrix A, Matrix B) => Product(A, B);
        public static Vector operator *(Vector a, Matrix B) => Product(a, B);
        public static Matrix operator /(Matrix A, double divisor) => Scale(1/divisor, A);
        public static Matrix operator ~(Matrix A) => A.Transpose();
        public static Matrix operator !(Matrix A) => A.Inverse();
        public static Vector operator /(Vector b, Matrix A) => A.Solve(b);
        public static Matrix operator /(Matrix B, Matrix A) => A.Solve(B);
        #endregion

    }

}
