using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Iterations
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Программа решения системы линейных уравнений" + Environment.NewLine +
                "методом последовательных (простых) итераций" + Environment.NewLine +
                Environment.NewLine +
                "Copyright 2022 Parshin A 3045", "О программе", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        int N = 0;
        private void Form1_Load(object sender, EventArgs e)
        {
            for (int k = 0; k < 3; k++) buttonPlus_Click(sender, e);
            newToolStripMenuItem_Click(sender, e);
        }

        //Установить ширину и заголовки
        void SetWidthAndHeaders()
        {
            for (int k = 0; k < N; k++)
            {
                dgv.Columns[k].HeaderText = (k + 1).ToString();
                dgv.Columns[k].Width = 50;
                dgvX.Columns[k].HeaderText = (k + 1).ToString();
                dgvX.Columns[k].Width = 50;
                dgvX.Columns[k].Visible = true;
            }
            dgv.Columns[N].Width = 60;
            dgv.Columns[N].HeaderText = "B";

            dgvX.Columns[N].Visible = false;

            for (int k = 0; k < N; k++)
                dgv.Rows[k].HeaderCell.Value = (k + 1).ToString();
            if (dgvX.RowCount == 0)
                dgvX.Rows.Add();
        }

        //Добавить 
        private void buttonPlus_Click(object sender, EventArgs e)
        {
            N++;
            while (dgv.ColumnCount < N + 1)
            {
                dgv.Columns.Add("", "");
                dgvX.Columns.Add("", "");
            }
            while (dgv.RowCount < N)
                dgv.Rows.Add();
            SetWidthAndHeaders();
        }

        private void buttonMinus_Click(object sender, EventArgs e)
        {
            if (N < 2) return;
            dgv.Columns.RemoveAt(N);
            dgvX.Columns.RemoveAt(N);
            dgv.Rows.RemoveAt(N - 1);
            N--;
            SetWidthAndHeaders();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() != DialogResult.OK) return;
            try
            {
                using (StreamReader stream = new StreamReader(openFileDialog.FileName))
                {
                    //Размер
                    int N0 = int.Parse(stream.ReadLine());
                    while (N0 < dgv.RowCount) buttonMinus_Click(sender, e);
                    while (N0 > dgv.RowCount) buttonPlus_Click(sender, e);

                    for (int r = 0; r < dgv.RowCount; r++)
                    {
                        string row = stream.ReadLine();
                        string[] data = row.Split('\t');
                        for (int c = 0; c < dgv.ColumnCount; c++)
                            dgv.Rows[r].Cells[c].Value = data[c];
                    }
                }
            }
            catch
            {
                MessageBox.Show("Неполадка с файлом");
            }
            ClearStyle();
        }

        void Save(string FileName)
        {
            using (StreamWriter stream = new StreamWriter(saveFileDialog.FileName))
            {
                stream.WriteLine(N);
                for (int r = 0; r < dgv.RowCount; r++)
                {
                    for (int c = 0; c < dgv.ColumnCount; c++)
                    {
                        if (dgv.Rows[r].Cells[c].Value == null)
                            stream.Write("0");
                        else
                            stream.Write(dgv.Rows[r].Cells[c].Value.ToString());
                        stream.Write("\t"); //Разделитель
                    }
                    stream.WriteLine();
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog() != DialogResult.OK) return;
            Save(saveFileDialog.FileName);
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int r = 0; r < dgv.RowCount; r++)
                for (int c = 0; c < dgv.ColumnCount; c++)
                    dgv.Rows[r].Cells[c].Value = 0;
            ClearStyle();
        }

        //Решить 
        double[,] M; //Матрица
        double[] B; //Вектор свободных членов
        double[] X; //Решение
        string ErrorString = ""; //Строка, поясняющая отказ

        void ReadMatrixAndVector()
        {
            M = new double[N, N];
            B = new double[N];
            for (int r = 0; r < N; r++)
            {
                //Прочитать матрицу
                for (int c = 0; c < N; c++)
                {
                    //Не числа - игнорировать, но подсвечивать красным цветом
                    dgv.Rows[r].Cells[c].Style.BackColor = Color.Red;
                    if (dgv.Rows[r].Cells[c].Value == null) M[r, c] = 0; else
                    if (double.TryParse(dgv.Rows[r].Cells[c].Value.ToString(), out M[r, c]))
                        dgv.Rows[r].Cells[c].Style.BackColor = Color.White;
                }
                //Прочитать свободные 
                dgv.Rows[r].Cells[N].Style.BackColor = Color.Red;
                if (dgv.Rows[r].Cells[N].Value == null) B[r] = 0; else
                if (double.TryParse(dgv.Rows[r].Cells[N].Value.ToString(), out B[r]))
                    dgv.Rows[r].Cells[N].Style.BackColor = Color.White;
            }
        }

        void ClearStyle()
        {
            for (int r = 0; r < dgv.RowCount; r++) MarkRow(r, Color.White);
        }

        private void solveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReadMatrixAndVector();
            //Попытка найти пропорцональные строки
            ClearStyle();
            if (ExistsProportionalRows())
            {
                labelErrorString.Text = "Обнаружены линейно зависимые строки";
                return;
            }
            //Решение
            bool Error;
            double epsilon;
            textBoxEpsilon.BackColor = Color.Red;
            if (double.TryParse(textBoxEpsilon.Text, out epsilon))
                textBoxEpsilon.BackColor = Color.White;
            else
                epsilon = 1E-3;
            X = Solve(M, B, N, epsilon, out Error);
            //Вывести результат (если он есть)
            for (int r = 0; r < N; r++)
                dgvX.Rows[0].Cells[r].Value = ((Error)?"?":X[r].ToString());
            labelErrorString.Text = ErrorString;
        }
        /*
        Найти строки, которые могут быть причиной линейной зависимости системы
        */
        bool ExistsProportionalRows()
        {
            int N = B.Length;

            //Проверить на пропорциональность
            for (int r1 = 0; r1 < N - 1; r1++)
            {
                for (int r2 = r1 + 1; r2 < N; r2++)
                    if (Proportional(r1, r2))
                    {
                        MarkRow(r1, Color.Yellow);
                        MarkRow(r2, Color.Yellow);
                        return true;
                    }
            }
            return false;
        }

        bool Proportional(int r1, int r2)
        {
            //Строки пропорциональны, если отношение элементов столбцов совпадает для всех столбцов 
            double K = M[r1,0] / M[r2,0];
            if (double.IsInfinity(K) || double.IsNaN(K)) K = 1E5;
            double epsilon = 1E-6;
            for (int c=0; c<B.Length; c++)
            {
                double Kc = M[r1, c] / M[r2, c];
                if (double.IsInfinity(Kc)) Kc = 1E5;
                if (double.IsNaN(Kc)) Kc = K;

                if (Math.Abs((Kc - K)) > epsilon)
                    return false;
            }
            return true;
        }

        void MarkRow(int r, Color color)
        {
            for (int c = 0; c < dgv.ColumnCount; c++)
                dgv.Rows[r].Cells[c].Style.BackColor = color;
        }

        double[] Solve(double[,] M, double[] B, int N, double epsilon, out bool Error)
        {
            ErrorString = "Решение найдено";
            double []X = new double[N];
            double D = Determinant(M, out Error); //Вычислить определитель
            if (Error)
            {
                ErrorString="Не вычисляется определитель";
                return X;
            }
            Error = Math.Abs (D)< 1E-9; //Определитель около 0 - решения не будет
            if (Error)
            {
                ErrorString = "Система линейно зависима";
                return X;
            }
            //Решение 
            int Iterations;
            Error = !SimpleIterations(M, B, ref X, epsilon, out Iterations);
            labelIteration.Text = "Итераций" + Iterations.ToString();
            return X;
        }

        //Решение СЛАУ методом простых итераций
        bool SimpleIterations(double[,] M, double[] B, ref double[] X, double epsilon, out int Iterations)
        {
            CreateLog();
            AppendLog("Начальная матрица");
            AppendLog(M);
            AppendLog("Вектор свободных членов");
            AppendLog(B);


            Iterations = 0;
            int N = B.Length;
            bool result = true;

            //Особый случай для "системы" 1 порядка
            if (N == 1)
            {
                X[0] = B[0] / M[0, 0];
                AppendLog("Система из одного уравнения");
                AppendLog(X);
                return true;
            }
            for (int k = 0; k < N; k++)
                if (M[k, k] == 0)
                {
                    ErrorString = "Нуль на главной диагонали";
                    return false; //Отказ от решения, так как на главной диагонали есть 0
                }

            for (int i = 0; i < N; i++)
                for (int j = 0; j < N; j++)
                    if (j != i)
                        if (Math.Abs(M[i, j]) > Math.Abs(M[i, i])) //Не обязательно "равно"
                        {
                            ErrorString = "Не преобладает главная диагональ";
                            return false; //Отказ от решения, так как главная диагональ не преобладает
                        }


            double[] X0 = new double[N];

            //Подготовить матрицы
            //b[i] = B[i] / M[i,i]

            for (int k = 0; k < N; k++)
            {
                B[k] = B[k] / M[k, k];
                //X[k] = X[k] / M[k, k];
                X[k] = B[k]; //Пусть будет такой, это не очень важно
            }
            //a[i,j] = -A[i][j] / A[i][i];

            for (int row = 0; row < N; row++)
            {
                double AII = M[row, row];
                for (int col = 0; col < N; col++)
                    M[row, col] = -M[row, col] / AII;
            }
            AppendLog("");
            AppendLog("Подготовленная матрица");
            AppendLog(M);
            AppendLog("Подготовленный вектор свободных членов");
            AppendLog(B);
            AppendLog("Стартовое значение решения");
            AppendLog(X);
            AppendLog("Итерации");

            for (int row = 0; row < N; row++) M[row, row] = 0;
            double Norma = 2 * epsilon;

            //Провести итерации
            int Limit = 2000;
            while (result && (Norma > epsilon) && (--Limit > 0))
            {
                Iterations++;
                //Запомнить предыдущее значение
                Array.Copy(X, X0, X.Length);
                //Итерация
                X = Mul(M, X);
                X = Add(X, B);
                //Оценка точности
                Norma = 0;
                for (int i = 0; i < N; i++) Norma += Math.Abs(X[i] - X0[i]);
                if (Double.IsInfinity(Norma) || Double.IsNaN(Norma))
                {
                    ErrorString = "Процесс расходится";
                    return false;
                }
                AppendLog("Итерация " + Iterations.ToString());
                AppendLog(X);
                AppendLog("Отклонение " + Norma.ToString());
            }
            if (Limit==0)
            {
                ErrorString = "Точность не достигнута";
            }
            result &= Limit > 0;
            return result;
        }

        //Сложение двух векторов
        double[] Add(double[] X, double[] Y)
        {
            int N = X.Length;
            double[] Result = new double[N];
            for (int row = 0; row < N; row++)
                Result[row] = X[row] + Y[row];
            return Result;
        }
        //Умножение матрицы на вектор
        double[] Mul(double[,] M, double[] X)
        {
            int N = X.Length;
            double[] Result = new double[N];
            for (int row = 0; row < N; row++)
            {
                double Sum = 0;
                for (int k = 0; k < N; k++) Sum += M[row,k] * X[k];
                Result[row] = Sum;
            }
            return Result;
        }

        //Определитель методом Гаусса
        double Determinant(double[,] A, out bool Error)
        {
            int N = A.GetLength(0);
            //Копия
            double[,] M = new double[N, N];
            for (int r = 0; r < N; r++)
                for (int c = 0; c < N; c++)
                    M[r, c] = A[r, c];

            //Прямой проход метода гаусса
            for (int k = 0; k < N; k++)
            {
                for (int row = k + 1; row < N; row++)
                {
                    if (M[k,k] == 0) M[k, k] = 1E-6;
                    double D = M[row, k] / M[k, k];
                    for (int col = 0; col < N; col++)
                        M[row, col] = M[row, col] - M[k, col] * D;
                }
            }
            double Det = 1;
            for (int k = 0; k < M.GetLength(0); k++)
                Det *= M[k, k];
            Error = double.IsNaN(Det);
            if (Error) return 0;
            return Det;
        }

        //Проверить решение (найти невязку)
        private void buttonControl_Click(object sender, EventArgs e)
        {
            if (X == null) return;
            //Проверить что Сумма(M[r,c] * X[c]) = B[r]
            ReadMatrixAndVector();
            //X содержит решение
            double Error = 0;
            for (int r = 0; r< N; r++)
            {
                double Sum = 0;
                for (int c = 0; c < N; c++)
                    Sum += M[r, c] * X[c];
                Error += Math.Abs(Sum - B[r]);
            }
            MessageBox.Show("Погрешность = "+Error.ToString(), "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        string LogName = "Log.txt";

        void CreateLog()
        {
            File.WriteAllText(LogName, "Решение методом простых итераций" + Environment.NewLine);
        }

        void AppendLog(string text)
        {
            File.AppendAllText(LogName, text + Environment.NewLine);
        }
        void AppendLog(double[,] M)
        {
            List<string> text = new List<string>();
            int N = B.Length;
            string line;
            for (int r = 0; r < N; r++)
            {
                line = "";
                for (int c = 0; c < N; c++)
                    line += M[r, c].ToString() + "\t";
                text.Add(line);
            }
            File.AppendAllLines("Log.txt", text.ToArray());
        }

        void AppendLog(double []X)
        {
            string text = "";
            foreach (double x in X)
                text += x.ToString() + "\t";
            text += Environment.NewLine;
            File.AppendAllText("Log.txt", text);
        }

    }
}


