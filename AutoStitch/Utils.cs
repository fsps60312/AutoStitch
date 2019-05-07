using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStitch
{
    public static class Utils
    {
        public static bool AllTheSame(params object[] objects)
        {
            for (int i = 1; i < objects.Length; i++) if (!objects[0].Equals(objects[i])) return false;
            return true;
        }
        static Random rand = new Random();
        public static double RandDouble() { lock (rand) return rand.NextDouble(); }
        /// <summary>
        /// return a number between [minValue, maxValue-1]
        /// </summary>
        /// <param name="minValue">inclusive</param>
        /// <param name="maxValue">exclusive</param>
        /// <returns></returns>
        public static int Rand(int minValue,int maxValue) { lock (rand) return rand.Next(minValue, maxValue); }
        public static void GetHeatColor(double v, out double r, out double g, out double b)
        {
            if (v < 0) r = g = b = 0;
            else if (v <= 0.25)
            {
                r = 0;
                g = v / 0.25;
                b = 1;
            }
            else if (v <= 0.5)
            {
                r = 0;
                g = 1;
                b = (0.5 - v) / 0.25;
            }
            else if (v <= 0.75)
            {
                r = ((v - 0.5) / 0.25 * 256).ClampByte();
                g = 1;
                b = 0;
            }
            else if (v <= 1)
            {
                r = 1;
                g = (1 - v) / 0.25;
                b = 0;
            }
            else r = g = b = 1;
        }
        static List<T>RandomlyPick<T>(List<T>s,int count)
        {
            List<int> selected = new List<int>();
            for(int i=0;i<count;i++)
            {
                int v;
                do { v = Rand(0, s.Count); } while (selected.Contains(v));
                selected.Add(v);
            }
            return selected.Select(i => s[i]).ToList();
        }
        /// <summary>
        /// let the points vote for the best average point without outliners
        /// </summary>
        /// <param name="points">the points</param>
        /// <param name="tolerance">maximal distance that a point will accept a point</param>
        /// <param name="tries"></param>
        /// <param name="accept_ratio">minimum propotion of acceptance that a point is selected</param>
        /// <param name="accept_threshold">mimumum number of acceptances that a point is selected</param>
        /// <returns></returns>
        public static List<int> VoteInliners(List<Tuple<double, double>> points, double tolerance, int tries = 100)
        {
            int max_num_inliners = 0;
            Tuple<double, double> candidate = null;
            var accepts = new Func<Tuple<double, double>, Tuple<double, double>, bool>((p, q) =>
                  {
                      double dx = p.Item1 - q.Item1;
                      double dy = p.Item2 - q.Item2;
                      double d = dx * dx + dy * dy;
                      return d <= tolerance * tolerance;
                  });
            for (int i = 0; i < tries; i++)
            {
                var point = RandomlyPick(points, 1)[0];
                int num_inliners = points.Sum(p => accepts(p, point) ? 1 : 0);
                if (num_inliners > max_num_inliners) { max_num_inliners = num_inliners; candidate = point; }
            }
            System.Diagnostics.Trace.Assert(candidate != null);
            List<Tuple<double, double>> inliners = points.Where(p => accepts(p, candidate)).ToList();
            var target_point = new Tuple<double, double>(inliners.Sum(p => p.Item1) / inliners.Count, inliners.Sum(p => p.Item2) / inliners.Count);
            List<int> ans = new List<int>();
            for (int i = 0; i < points.Count; i++) if (accepts(points[i], target_point)) ans.Add(i);
            return ans;
        }
        public static List<int> VoteInliners(List<Tuple<double, double, double, double>> points, double tolerance, int tries = 100)
        {
            var make_mat = new Func<List<Tuple<double, double, double, double>>, double[,]>(standard =>
            {
                // (ax+by+c)/(px+qy+1)=x'
                // (dx+ey+f)/(px+qy+1)=y'
                // (ax+by+c)=x'*(px+qy+1)
                // (dx+ey+f)=y'*(px+qy+1)
                // xa+yb-x'xp-x'yq+c = x'
                // xd+ye-y'xp-y'yq+f = y'
                // xa+yb+c        -x'xp-x'yq = x'
                //        +xd+ye+f-y'xp-y'yq = y'
                double[,] mat = new double[8, 8];
                double[,] val = new double[8, 1];
                System.Diagnostics.Trace.Assert(standard.Count == 4);
                for (int i = 0; i < 8;)
                {
                    // (x,y)->(a,b)
                    var std = standard[i / 2];
                    double x = std.Item1, y = std.Item2, a = std.Item3, b = std.Item4;
                    (mat[i, 0], mat[i, 1], mat[i, 2]) = (x, y, 1);
                    (mat[i, 6], mat[i, 7]) = (-a * x, -a * y);
                    val[i, 0] = a;
                    i++;
                    (mat[i, 3], mat[i, 4], mat[i, 5]) = (x, y, 1);
                    (mat[i, 6], mat[i, 7]) = (-b * x, -b * y);
                    val[i, 0] = b;
                    i++;
                }
                double[,] inverse = MatrixInverse(mat);
                if (inverse == null) return null;
                for (int i = 0; i < 64; i++) if (Math.Abs(inverse[i / 8, i % 8]) > 1e4) return null;
                {
                    double[,] test = MatrixProduct(mat, inverse);
                    for (int i = 0; i < test.GetLength(0); i++)
                    {
                        for (int j = 0; j < test.GetLength(1); j++)
                        {
                            double error = (i == j ? 1 : 0) - test[i, j];
                            if (error * error >= 1e-9) LogPanel.Log($"error = {error}\n{MatrixToString(mat)}\n{MatrixToString(inverse)}");
                            System.Diagnostics.Trace.Assert(error * error < 1e-9);
                        }
                    }
                }
                double[,] result = MatrixProduct(inverse, val);
                System.Diagnostics.Trace.Assert(result.GetLength(0) == 8 && result.GetLength(1) == 1);
                result = new double[3, 3]
                {
                    {result[0,0],result[1,0],result[2,0] },
                    {result[3,0],result[4,0],result[5,0]},
                    {result[6,0],result[7,0],1 }
                };
                if (Math.Sqrt(result[2, 0] * result[2, 0] + result[2, 1] * result[2, 1]) > 2e-1) return null; // too much perspective
                foreach (var s in standard)
                {
                    double[,] test = MatrixProduct(result, new double[3, 1] { { s.Item1 }, { s.Item2 }, { 1 } });
                    if (Math.Abs(test[2, 0]) < 1e-9) return null;
                    double error_x = s.Item3 - test[0, 0] / test[2, 0];
                    double error_y = s.Item4 - test[1, 0] / test[2, 0];
                    if (error_x * error_x + error_y * error_y >= 1e-5)
                    {
                        LogPanel.Log(MatrixToString(result));
                        foreach (var v in standard)
                        {
                            test = MatrixProduct(result, new double[3, 1] { { v.Item1 }, { v.Item2 }, { 1 } });
                            LogPanel.Log($"({v.Item1}, {v.Item2}) => ({v.Item3}, {v.Item4}) ({test[0, 0] / test[2, 0]}, {test[1, 0] / test[2, 0]}, {test[2, 0]})");
                        }
                        LogPanel.Log($"error_x: {error_x}, error_y: {error_y}");
                    }
                    System.Diagnostics.Trace.Assert(error_x * error_x + error_y * error_y < 1e-5);
                }
                return result;
            });
            var accepts = new Func<Tuple<double, double,double,double>, double[,], bool>((p, mat) =>
            {
                var r = MatrixProduct(mat, new double[3, 1] { { p.Item1 }, { p.Item2 }, { 1 } });
                double dx = p.Item3 - r[0, 0] / r[2, 0];
                double dy = p.Item4 - r[1, 0] / r[2, 0];
                double d = dx * dx + dy * dy;
                return d <= tolerance * tolerance;
            });
            if (points.Count <= 4) return points.Select((v, i) => i).ToList();
            double[,] result_mat = null;
            int max_accept_count = int.MinValue;
            for (int i = 0; i < tries; i++)
            {
                var mat = make_mat(RandomlyPick(points, 4));
                if (mat == null) continue;
                int accept_count = points.Count(p => accepts(p, mat));
                System.Diagnostics.Trace.Assert(accept_count >= 4);
                if (accept_count > max_accept_count) { max_accept_count = accept_count; result_mat = mat; }
            }
            if (result_mat == null) return new List<int>(0);
            List<int> ans = new List<int>();
            for (int i = 0; i < points.Count; i++) if (accepts(points[i], result_mat)) ans.Add(i);
            return ans;
        }
        public static double Mod2PI(double v)
        {
            v %= 2.0 * Math.PI;
            if (v < 0) v += 2.0 * Math.PI;
            return v;
        }
        public static string MatrixToString(double[,]matrix)
        {
            StringBuilder sb = new StringBuilder();
            for(int i=0;i<matrix.GetLength(0);i++)
            {
                for(int j=0;j<matrix.GetLength(1);j++)
                {
                    if (j > 0) sb.Append("\t");
                    sb.Append(matrix[i, j]);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
        public static double[,] MatrixCreate(int rows, int cols)
        {
            double[,] result = new double[rows,cols];
            return result;
        }

        public static double[,] MatrixIdentity(int n)
        {
            // return an n x n Identity matrix
            double[,] result = MatrixCreate(n, n);
            for (int i = 0; i < n; ++i)
                result[i,i] = 1.0;

            return result;
        }
        public static double[,] MatrixRandom(int rows, int cols, double minVal, double maxVal, int seed)
        {
            // return a matrix with random values
            Random ran = new Random(seed);
            double[,] result = MatrixCreate(rows, cols);
            for (int i = 0; i < rows; ++i)
                for (int j = 0; j < cols; ++j)
                    result[i, j] = (maxVal - minVal) * ran.NextDouble() + minVal;
            return result;
        }
        public static double[,] MatrixProduct(double[,] matrixA, double[,] matrixB)
        {
            int aRows = matrixA.GetLength(0); int aCols = matrixA.GetLength(1);
            int bRows = matrixB.GetLength(0); int bCols = matrixB.GetLength(1);
            if (aCols != bRows)
                throw new Exception("Non-conformable matrices in MatrixProduct");

            double[,] result = MatrixCreate(aRows, bCols);

            for (int i = 0; i < aRows; ++i) // each row of A
                for (int j = 0; j < bCols; ++j) // each col of B
                    for (int k = 0; k < aCols; ++k) // could use k less-than bRows
                        result[i,j] += matrixA[i,k] * matrixB[k,j];

            return result;
        }
        public static bool MatrixAreEqual(double[,] matrixA, double[,] matrixB, double epsilon)
        {
            // true if all values in matrixA == values in matrixB
            int aRows = matrixA.GetLength(0); int aCols = matrixA.GetLength(1);
            int bRows = matrixB.GetLength(0); int bCols = matrixB.GetLength(1);
            if (aRows != bRows || aCols != bCols)
                throw new Exception("Non-conformable matrices");

            for (int i = 0; i < aRows; ++i) // each row of A and B
                for (int j = 0; j < aCols; ++j) // each col of A and B
                                                //if (matrixA[i][j] != matrixB[i][j])
                    if (Math.Abs(matrixA[i, j] - matrixB[i, j]) > epsilon)
                        return false;
            return true;
        }
        public static double[,] MatrixInverse(double[,] matrix)
        {
            int n = matrix.GetLength(0);
            double[,] result = MatrixDuplicate(matrix);

            int[] perm;
            int toggle;
            double[,] lum = MatrixDecompose(matrix, out perm,
              out toggle);
            if (lum == null) return null;
                //throw new Exception("Unable to compute inverse");

            double[] b = new double[n];
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    if (i == perm[j])
                        b[j] = 1.0;
                    else
                        b[j] = 0.0;
                }

                double[] x = HelperSolve(lum, b);

                for (int j = 0; j < n; ++j)
                    result[j,i] = x[j];
            }
            return result;
        }

        static double[,] MatrixDuplicate(double[,] matrix)
        {
            // allocates/creates a duplicate of a matrix.
            double[,] result = MatrixCreate(matrix.GetLength(0), matrix.GetLength(1));
            for (int i = 0; i < matrix.GetLength(0); ++i) // copy the values
                for (int j = 0; j < matrix.GetLength(1); ++j)
                    result[i,j] = matrix[i,j];
            return result;
        }

        static double[] HelperSolve(double[,] luMatrix, double[] b)
        {
            // before calling this helper, permute b using the perm array
            // from MatrixDecompose that generated luMatrix
            int n = luMatrix.GetLength(0);
            double[] x = new double[n];
            b.CopyTo(x, 0);

            for (int i = 1; i < n; ++i)
            {
                double sum = x[i];
                for (int j = 0; j < i; ++j)
                    sum -= luMatrix[i,j] * x[j];
                x[i] = sum;
            }

            x[n - 1] /= luMatrix[n - 1,n - 1];
            for (int i = n - 2; i >= 0; --i)
            {
                double sum = x[i];
                for (int j = i + 1; j < n; ++j)
                    sum -= luMatrix[i,j] * x[j];
                x[i] = sum / luMatrix[i,i];
            }

            return x;
        }
        static void SwapRows(double[,]matrix,int row1,int row2)
        {
            if (row1 == row2) return;
            for(int i=0;i<matrix.GetLength(1);i++)
            {
                double v = matrix[row1,i];
                matrix[row1,i] = matrix[row2,i];
                matrix[row2,i] = v;
            }
        }
        static double[,] MatrixDecompose(double[,] matrix, out int[] perm, out int toggle)
        {
            // Doolittle LUP decomposition with partial pivoting.
            // rerturns: result is L (with 1s on diagonal) and U;
            // perm holds row permutations; toggle is +1 or -1 (even or odd)
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1); // assume square
            if (rows != cols)
                throw new Exception("Attempt to decompose a non-square m");

            int n = rows; // convenience

            double[,] result = MatrixDuplicate(matrix);

            perm = new int[n]; // set up row permutation result
            for (int i = 0; i < n; ++i) { perm[i] = i; }

            toggle = 1; // toggle tracks row swaps.
                        // +1 -greater-than even, -1 -greater-than odd. used by MatrixDeterminant

            for (int j = 0; j < n - 1; ++j) // each column
            {
                double colMax = Math.Abs(result[j,j]); // find largest val in col
                int pRow = j;
                //for (int i = j + 1; i less-than n; ++i)
                //{
                //  if (result[i][j] greater-than colMax)
                //  {
                //    colMax = result[i][j];
                //    pRow = i;
                //  }
                //}

                // reader Matt V needed this:
                for (int i = j + 1; i < n; ++i)
                {
                    if (Math.Abs(result[i,j]) > colMax)
                    {
                        colMax = Math.Abs(result[i,j]);
                        pRow = i;
                    }
                }
                // Not sure if this approach is needed always, or not.

                if (pRow != j) // if largest value not on pivot, swap rows
                {
                    SwapRows(result, pRow, j);
                    //double[] rowPtr = result[pRow];
                    //result[pRow] = result[j];
                    //result[j] = rowPtr;

                    int tmp = perm[pRow]; // and swap perm info
                    perm[pRow] = perm[j];
                    perm[j] = tmp;

                    toggle = -toggle; // adjust the row-swap toggle
                }

                // --------------------------------------------------
                // This part added later (not in original)
                // and replaces the 'return null' below.
                // if there is a 0 on the diagonal, find a good row
                // from i = j+1 down that doesn't have
                // a 0 in column j, and swap that good row with row j
                // --------------------------------------------------

                if (result[j,j] == 0.0)
                {
                    // find a good row to swap
                    int goodRow = -1;
                    for (int row = j + 1; row < n; ++row)
                    {
                        if (result[row,j] != 0.0)
                            goodRow = row;
                    }

                    if (goodRow == -1) return null;
                        //throw new Exception("Cannot use Doolittle's method");

                    // swap rows so 0.0 no longer on diagonal
                    SwapRows(result, goodRow, j);
                    //double[] rowPtr = result[goodRow];
                    //result[goodRow] = result[j];
                    //result[j] = rowPtr;

                    int tmp = perm[goodRow]; // and swap perm info
                    perm[goodRow] = perm[j];
                    perm[j] = tmp;

                    toggle = -toggle; // adjust the row-swap toggle
                }
                // --------------------------------------------------
                // if diagonal after swap is zero . .
                //if (Math.Abs(result[j][j]) less-than 1.0E-20) 
                //  return null; // consider a throw

                for (int i = j + 1; i < n; ++i)
                {
                    result[i,j] /= result[j,j];
                    for (int k = j + 1; k < n; ++k)
                    {
                        result[i,k] -= result[i,j] * result[j,k];
                    }
                }


            } // main j column loop

            return result;
        }
    }
}
