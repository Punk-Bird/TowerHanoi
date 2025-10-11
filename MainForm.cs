using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;

namespace HanoiTowerSolver
{
    public class MainForm : Form
    {
        private HanoiTower hanoiTower;
        private Button initButton, solveButton, nextButton, autoButton, benchmarkButton;
        private TrackBar diskTrackBar;
        private ListBox movesList;
        private Label infoLabel;
        private Panel drawingPanel;
        private Panel chartPanel;

        private System.ComponentModel.IContainer components;
        private int currentStep = 0;
        private List<string> solutionSteps;

        // Переменные для анимации
        private int animatingDisk = 0;
        private int animatingFrom = -1;
        private int animatingTo = -1;
        private PointF currentAnimationPos;
        private System.Windows.Forms.Timer animationTimer;
        private bool isAnimating = false;
        private List<PointF> animationPath;
        private int currentPathIndex = 0;
        private float progressToNextPoint = 0f;

        // Буфер для отрисовки
        private Bitmap backBuffer;
        private Graphics backBufferGraphics;

        // Для графика и анализа времени
        private List<(int disks, long timeMs)> timeDataPoints;
        private NumericUpDown minDisksControl;
        private NumericUpDown maxDisksControl;
        private int minDisksForChart = 1;
        private int maxDisksForChart = 35;

        public MainForm()
        {
            this.components = new System.ComponentModel.Container();
            this.hanoiTower = new HanoiTower();
            this.timeDataPoints = new List<(int disks, long timeMs)>();
            this.InitializeComponents();
            this.DoubleBuffered = true;
        }

        private void InitializeComponents()
        {
            // Основные настройки формы
            this.Text = "Ханойская башня - Анализ производительности";
            this.Size = new Size(1200, 750);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Инициализация таймера анимации
            InitializeAnimationTimer();

            // Создание элементов управления
            CreateControls();

            // Подписка на события
            SubscribeToEvents();

            InitializeBackBuffer();
        }

        private void InitializeAnimationTimer()
        {
            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 16; // ~60 FPS
            animationTimer.Tick += AnimationTimer_Tick;
        }

        private void CreateControls()
        {
            // Левая панель - управление
            CreateControlPanel();

            // Центральная панель - визуализация башни
            CreateDrawingPanel();

            // Нижняя панель - график
            CreateChartPanel();
        }

        private void CreateControlPanel()
        {
            // Основные элементы управления
            diskTrackBar = new TrackBar
            {
                Location = new Point(20, 20),
                Size = new Size(200, 45),
                Minimum = 3,
                Maximum = 8,
                Value = 3,
                TickFrequency = 1
            };

            Label diskLabel = new Label
            {
                Text = "Количество дисков:",
                Location = new Point(20, 65),
                Size = new Size(150, 20)
            };

            Label diskValueLabel = new Label
            {
                Text = diskTrackBar.Value.ToString(),
                Location = new Point(230, 65),
                Size = new Size(30, 20)
            };

            diskTrackBar.Scroll += (sender, e) => diskValueLabel.Text = diskTrackBar.Value.ToString();

            // Кнопки управления
            initButton = CreateButton("Создать башню", 20, 100);
            solveButton = CreateButton("Найти решение", 130, 100);
            nextButton = CreateButton("Следующий ход", 20, 140);
            autoButton = CreateButton("Автопрохождение", 130, 140);
            benchmarkButton = CreateButton("Анализ времени", 20, 180);

            solveButton.Enabled = false;
            nextButton.Enabled = false;
            autoButton.Enabled = false;

            // Настройки анализа
            CreateAnalysisControls();

            // Список ходов
            movesList = new ListBox
            {
                Location = new Point(20, 280),
                Size = new Size(250, 300),
                HorizontalScrollbar = true
            };

            // ДОБАВЛЕНО: Инициализация infoLabel
            infoLabel = new Label
            {
                Text = "Ходов: 0",
                Location = new Point(20, 590),
                Size = new Size(250, 40),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            // Добавление элементов на форму
            var controls = new Control[]
            {
        diskTrackBar, diskLabel, diskValueLabel,
        initButton, solveButton, nextButton, autoButton, benchmarkButton,
        movesList, infoLabel
            };

            this.Controls.AddRange(controls);
        }

        private void CreateAnalysisControls()
        {
            Label rangeLabel = new Label
            {
                Text = "Диапазон для анализа:",
                Location = new Point(20, 220),
                Size = new Size(150, 20),
                Font = new Font("Arial", 9)
            };

            minDisksControl = new NumericUpDown
            {
                Location = new Point(20, 245),
                Size = new Size(45, 20),
                Minimum = 1,
                Maximum = 25,
                Value = minDisksForChart
            };

            Label toLabel = new Label
            {
                Text = "-",
                Location = new Point(70, 248),
                Size = new Size(10, 20)
            };

            maxDisksControl = new NumericUpDown
            {
                Location = new Point(85, 245),
                Size = new Size(45, 20),
                Minimum = 2,
                Maximum = 35,
                Value = maxDisksForChart
            };

            minDisksControl.ValueChanged += (s, e) =>
            {
                minDisksForChart = (int)minDisksControl.Value;
                if (minDisksForChart >= maxDisksForChart)
                {
                    maxDisksForChart = minDisksForChart + 1;
                    maxDisksControl.Value = maxDisksForChart;
                }
            };

            maxDisksControl.ValueChanged += (s, e) =>
            {
                maxDisksForChart = (int)maxDisksControl.Value;
                if (maxDisksForChart <= minDisksForChart)
                {
                    minDisksForChart = maxDisksForChart - 1;
                    minDisksControl.Value = minDisksForChart;
                }
            };

            this.Controls.Add(rangeLabel);
            this.Controls.Add(minDisksControl);
            this.Controls.Add(toLabel);
            this.Controls.Add(maxDisksControl);
        }

        private Button CreateButton(string text, int x, int y)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(100, 30)
            };
        }

        private void CreateDrawingPanel()
        {
            drawingPanel = new Panel
            {
                Location = new Point(300, 20),
                Size = new Size(550, 400),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            drawingPanel.Paint += DrawingPanel_Paint;
            SetDoubleBuffered(drawingPanel);
            this.Controls.Add(drawingPanel);
        }

        private void CreateChartPanel()
        {
            chartPanel = new Panel
            {
                Location = new Point(300, 440),
                Size = new Size(550, 250),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            chartPanel.Paint += ChartPanel_Paint;
            SetDoubleBuffered(chartPanel);
            this.Controls.Add(chartPanel);
        }

        private void SubscribeToEvents()
        {
            initButton.Click += InitButton_Click;
            solveButton.Click += SolveButton_Click;
            nextButton.Click += NextButton_Click;
            autoButton.Click += AutoButton_Click;
            benchmarkButton.Click += BenchmarkButton_Click;

            hanoiTower.StateChanged += (state) => RedrawScene();
            hanoiTower.MoveMade += (move) => UpdateMovesList(move);
            hanoiTower.DiskMoveStarted += (disk, from, to) => StartDiskAnimation(disk, from, to);
            hanoiTower.DiskMoveCompleted += () => CompleteDiskAnimation();

            this.SizeChanged += (s, e) =>
            {
                InitializeBackBuffer();
                RedrawScene();
                chartPanel.Invalidate();
            };
        }

        private void UpdateMovesList(string move)
        {
            movesList.Items.Add(move);
            if (movesList.Items.Count > 0)
                movesList.SelectedIndex = movesList.Items.Count - 1;
            UpdateInfoLabel();
        }

        private void UpdateInfoLabel()
        {
            int moveCount = movesList.Items.Count;
            long timeMs = hanoiTower.LastSolutionTimeMs;
            int totalMoves = solutionSteps?.Count ?? 0;

            string movesInfo = totalMoves > 0 ? $"Ходов: {moveCount}/{totalMoves}" : "Ходов: 0";

            infoLabel.Text = $"{movesInfo}";
        }

        private void InitializeBackBuffer()
        {
            backBuffer?.Dispose();
            backBufferGraphics?.Dispose();

            if (drawingPanel.Width > 0 && drawingPanel.Height > 0)
            {
                backBuffer = new Bitmap(drawingPanel.Width, drawingPanel.Height);
                backBufferGraphics = Graphics.FromImage(backBuffer);
                backBufferGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            }
        }

        // АНИМАЦИЯ
        private void StartDiskAnimation(int disk, int from, int to)
        {
            if (isAnimating) return;

            animatingDisk = disk;
            animatingFrom = from;
            animatingTo = to;
            isAnimating = true;
            currentPathIndex = 0;
            progressToNextPoint = 0f;

            animationPath = CreateAnimationPath(from, to, disk);
            currentAnimationPos = animationPath[0];
            animationTimer.Start();
        }

        private List<PointF> CreateAnimationPath(int from, int to, int disk)
        {
            var path = new List<PointF>();
            var towers = hanoiTower.GetCurrentState();

            int startDiskIndex = towers[from].Count - 1;
            PointF startPos = CalculateDiskPosition(from, startDiskIndex, disk);
            path.Add(startPos);

            PointF topFromPos = new PointF(startPos.X, 60f);
            path.Add(topFromPos);

            PointF topToPos = new PointF(CalculateDiskPosition(to, 0, disk).X, 60f);
            path.Add(topToPos);

            int targetDiskIndex = towers[to].Count;
            PointF endPos = CalculateDiskPosition(to, targetDiskIndex, disk);
            path.Add(endPos);

            return path;
        }

        private void CompleteDiskAnimation()
        {
            animationTimer.Stop();
            isAnimating = false;
            animatingFrom = -1;
            animatingTo = -1;
            animationPath = null;
            RedrawScene();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (!isAnimating || animationPath == null || currentPathIndex >= animationPath.Count - 1)
            {
                if (isAnimating && animationPath != null && currentPathIndex >= animationPath.Count - 1)
                {
                    currentAnimationPos = animationPath[animationPath.Count - 1];
                    RedrawScene();
                }
                CompleteDiskAnimation();
                return;
            }

            PointF currentPoint = animationPath[currentPathIndex];
            PointF nextPoint = animationPath[currentPathIndex + 1];

            progressToNextPoint += 0.08f;

            if (progressToNextPoint >= 1.0f)
            {
                currentAnimationPos = nextPoint;
                currentPathIndex++;
                progressToNextPoint = 0f;

                if (currentPathIndex >= animationPath.Count - 1)
                {
                    currentAnimationPos = animationPath[animationPath.Count - 1];
                    RedrawScene();
                    CompleteDiskAnimation();
                    return;
                }
            }
            else
            {
                currentAnimationPos.X = currentPoint.X + (nextPoint.X - currentPoint.X) * progressToNextPoint;
                currentAnimationPos.Y = currentPoint.Y + (nextPoint.Y - currentPoint.Y) * progressToNextPoint;
            }

            RedrawScene();
        }

        // ОСНОВНЫЕ МЕТОДЫ УПРАВЛЕНИЯ
        private void InitButton_Click(object sender, EventArgs e)
        {
            CompleteDiskAnimation();
            hanoiTower.Initialize(diskTrackBar.Value);
            movesList.Items.Clear();
            UpdateInfoLabel();
            solveButton.Enabled = true;
            nextButton.Enabled = false;
            autoButton.Enabled = false;
            currentStep = 0;
            solutionSteps = null;
            RedrawScene();
        }

        private void SolveButton_Click(object sender, EventArgs e)
        {
            solutionSteps = hanoiTower.GenerateSolution();
            solveButton.Enabled = false;
            nextButton.Enabled = true;
            autoButton.Enabled = true;
            movesList.Items.Clear();
            UpdateInfoLabel();
        }

        private async void NextButton_Click(object sender, EventArgs e)
        {
            if (solutionSteps != null && currentStep < solutionSteps.Count && !isAnimating)
            {
                nextButton.Enabled = false;
                autoButton.Enabled = false;

                await hanoiTower.ExecuteMoveWithAnimation(solutionSteps[currentStep], 800);
                currentStep++;
                UpdateInfoLabel();

                if (currentStep >= solutionSteps.Count)
                {
                    nextButton.Enabled = false;
                    autoButton.Enabled = false;
                    MessageBox.Show("Решение завершено!", "Готово",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    nextButton.Enabled = true;
                    autoButton.Enabled = true;
                }
            }
        }

        private async void AutoButton_Click(object sender, EventArgs e)
        {
            if (solutionSteps != null && currentStep < solutionSteps.Count && !isAnimating)
            {
                nextButton.Enabled = false;
                autoButton.Enabled = false;
                solveButton.Enabled = false;
                initButton.Enabled = false;

                while (currentStep < solutionSteps.Count)
                {
                    await hanoiTower.ExecuteMoveWithAnimation(solutionSteps[currentStep], 500);
                    currentStep++;
                    UpdateInfoLabel();
                    Application.DoEvents();
                }

                MessageBox.Show("Решение завершено!", "Готово",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                initButton.Enabled = true;
                solveButton.Enabled = true;
            }
        }

        // АНАЛИЗ ВРЕМЕНИ И ГРАФИК
        private void BenchmarkButton_Click(object sender, EventArgs e)
        {
            timeDataPoints.Clear();

            using (var progressForm = CreateProgressForm())
            {
                progressForm.Show();
                Application.DoEvents();

                var totalStopwatch = Stopwatch.StartNew();
                int totalDisks = maxDisksForChart - minDisksForChart + 1;
                int completed = 0;

                for (int disks = minDisksForChart; disks <= maxDisksForChart; disks++)
                {
                    if (totalStopwatch.Elapsed.TotalSeconds > 3000) // 3000 секунд лимит
                    {
                        MessageBox.Show($"Измерение остановлено после 3000 секунд.\nУспешно измерено {completed} из {totalDisks} вариантов.",
                            "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    }

                    completed++;
                    UpdateProgressForm(progressForm, disks, completed, totalDisks, totalStopwatch.Elapsed.TotalSeconds);
                    Application.DoEvents();

                    long timeMs = hanoiTower.MeasureSolutionTime(disks);
                    timeDataPoints.Add((disks, timeMs));

                    if (disks > 15) System.Threading.Thread.Sleep(100);
                    else if (disks > 10) System.Threading.Thread.Sleep(50);
                }

                progressForm.Close();
            }

            chartPanel.Invalidate();
            MessageBox.Show($"Анализ завершен!\nИзмерено время для {timeDataPoints.Count} вариантов (диски {minDisksForChart}-{timeDataPoints.Last().disks}).",
                "Анализ времени", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private Form CreateProgressForm()
        {
            var form = new Form
            {
                Text = "Измерение времени",
                Size = new Size(400, 130),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var progressLabel = new Label { Location = new Point(10, 10), Size = new Size(370, 20) };
            var progressBar = new ProgressBar { Location = new Point(10, 40), Size = new Size(370, 20) };
            var timeLabel = new Label { Location = new Point(10, 65), Size = new Size(250, 20) };

            form.Controls.Add(progressLabel);
            form.Controls.Add(progressBar);
            form.Controls.Add(timeLabel);

            return form;
        }

        private void UpdateProgressForm(Form progressForm, int disks, int completed, int totalDisks, double elapsedSeconds)
        {
            var progressLabel = (Label)progressForm.Controls[0];
            var progressBar = (ProgressBar)progressForm.Controls[1];
            var timeLabel = (Label)progressForm.Controls[2];

            progressBar.Minimum = minDisksForChart;
            progressBar.Maximum = maxDisksForChart;
            progressBar.Value = disks;

            progressLabel.Text = $"Измерение для {disks} дисков... ({completed}/{totalDisks})";
            timeLabel.Text = $"Прошло: {elapsedSeconds:0.##} сек";
        }

        private void ChartPanel_Paint(object sender, PaintEventArgs e)
        {
            if (timeDataPoints.Count == 0) return;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.White);

            int padding = 50;
            int width = chartPanel.Width - padding * 2;
            int height = chartPanel.Height - padding * 2;

            int maxDisks = timeDataPoints.Max(p => p.disks);
            int minDisks = timeDataPoints.Min(p => p.disks);
            long maxTime = timeDataPoints.Max(p => p.timeMs);
            if (maxTime == 0) maxTime = 1;

            // Оси
            using (Pen axisPen = new Pen(Color.Black, 2))
            {
                g.DrawLine(axisPen, padding, padding + height, padding + width, padding + height);
                g.DrawLine(axisPen, padding, padding, padding, padding + height);
            }

            // Подписи осей
            g.DrawString("Количество дисков", new Font("Arial", 10, FontStyle.Bold), Brushes.Black,
                padding + width / 2 - 50, padding + height + 20);

            

            // Сетка и подписи оси X
            int xStep = Math.Max(1, (maxDisks - minDisks) / 8);
            for (int disks = minDisks; disks <= maxDisks; disks += xStep)
            {
                int x = padding + (disks - minDisks) * width / (maxDisks - minDisks);
                g.DrawLine(Pens.LightGray, x, padding, x, padding + height);
                g.DrawString(disks.ToString(), new Font("Arial", 9), Brushes.Black, x - 8, padding + height + 5);
            }

            // Сетка и подписи оси Y
            int yStepCount = 6;
            for (int i = 0; i <= yStepCount; i++)
            {
                long timeValue = maxTime * i / yStepCount;
                int y = padding + height - (int)(height * i / yStepCount);

                g.DrawLine(Pens.LightGray, padding, y, padding + width, y);
                g.DrawString(FormatTimeLabel(timeValue), new Font("Arial", 9), Brushes.Black,
                    padding - 45, y - 10);
            }

            // График
            if (timeDataPoints.Count > 1)
            {
                using (Pen graphPen = new Pen(Color.Blue, 3))
                {
                    for (int i = 0; i < timeDataPoints.Count - 1; i++)
                    {
                        var p1 = timeDataPoints[i];
                        var p2 = timeDataPoints[i + 1];

                        int x1 = padding + (p1.disks - minDisks) * width / (maxDisks - minDisks);
                        int y1 = padding + height - (int)(p1.timeMs * height / maxTime);

                        int x2 = padding + (p2.disks - minDisks) * width / (maxDisks - minDisks);
                        int y2 = padding + height - (int)(p2.timeMs * height / maxTime);

                        g.DrawLine(graphPen, x1, y1, x2, y2);
                    }
                }

                // Точки
                foreach (var point in timeDataPoints)
                {
                    int x = padding + (point.disks - minDisks) * width / (maxDisks - minDisks);
                    int y = padding + height - (int)(point.timeMs * height / maxTime);

                    g.FillEllipse(Brushes.Red, x - 4, y - 4, 8, 8);
                    g.DrawEllipse(Pens.DarkRed, x - 4, y - 4, 8, 8);

                    if (point.timeMs > 0 && height > 150)
                    {
                        g.DrawString(FormatTimeLabel(point.timeMs), new Font("Arial", 8), Brushes.DarkBlue,
                            x - 20, y - 25);
                    }
                }
            }

            // Заголовок
            g.DrawString("Зависимость времени решения от количества дисков",
                new Font("Arial", 11, FontStyle.Bold), Brushes.Black,
                padding + 20, padding - 30);
        }

        private string FormatTimeLabel(long milliseconds)
        {
            if (milliseconds < 1000) return $"{milliseconds} мс";
            else if (milliseconds < 60000) return $"{(milliseconds / 1000.0):0.##} сек";
            else
            {
                long seconds = milliseconds / 1000;
                long minutes = seconds / 60;
                seconds = seconds % 60;
                return $"{minutes}:{seconds:D2}";
            }
        }

        // ОТРИСОВКА БАШНИ
        private void DrawingPanel_Paint(object sender, PaintEventArgs e)
        {
            if (backBuffer != null)
            {
                e.Graphics.DrawImage(backBuffer, 0, 0);
            }
            else
            {
                e.Graphics.Clear(Color.White);
                DrawTowers(e.Graphics, drawingPanel.Width, drawingPanel.Height);
                if (isAnimating) DrawAnimatedDisk(e.Graphics);
            }
        }

        private void RedrawScene()
        {
            if (backBufferGraphics == null) return;

            backBufferGraphics.Clear(Color.White);
            DrawTowers(backBufferGraphics, drawingPanel.Width, drawingPanel.Height);
            if (isAnimating) DrawAnimatedDisk(backBufferGraphics);
            drawingPanel.Invalidate();
        }

        private PointF CalculateDiskPosition(int towerIndex, int diskIndex, int diskSize)
        {
            int towerWidth = drawingPanel.Width / 4;
            int baseY = drawingPanel.Height - 50;
            int diskHeight = 20;
            int maxDiskWidth = towerWidth - 20;

            int diskWidth = maxDiskWidth * diskSize / hanoiTower.DiskCount;
            float x = (towerIndex + 1) * towerWidth;
            float y = baseY - (diskIndex + 1) * diskHeight;

            return new PointF(x, y);
        }

        private void DrawAnimatedDisk(Graphics g)
        {
            int maxDiskWidth = (drawingPanel.Width / 4) - 20;
            int diskWidth = maxDiskWidth * animatingDisk / hanoiTower.DiskCount;
            int diskHeight = 20;

            using (var brush = new SolidBrush(GetDiskColor(animatingDisk)))
            {
                g.FillRectangle(brush, currentAnimationPos.X - diskWidth / 2, currentAnimationPos.Y, diskWidth, diskHeight);
                g.DrawRectangle(Pens.Black, currentAnimationPos.X - diskWidth / 2, currentAnimationPos.Y, diskWidth, diskHeight);
            }

            g.DrawString(animatingDisk.ToString(), new Font("Arial", 8), Brushes.White,
                currentAnimationPos.X - 5, currentAnimationPos.Y + 5);
        }

        private void DrawTowers(Graphics g, int width, int height)
        {
            int towerCount = 3;
            int towerWidth = width / (towerCount + 1);
            int baseY = height - 50;
            int diskHeight = 20;
            int maxDiskWidth = towerWidth - 20;

            // Основания и стержни
            for (int i = 0; i < towerCount; i++)
            {
                int x = (i + 1) * towerWidth;
                g.FillRectangle(Brushes.Brown, x - 5, baseY, 10, 10);
                g.FillRectangle(Brushes.Gray, x - 2, 50, 4, baseY - 50);
                g.DrawString($"{(char)('A' + i)}", new Font("Arial", 12), Brushes.Black, x - 8, baseY + 15);
            }

            // Диски
            var towers = hanoiTower.GetCurrentState();
            for (int towerIndex = 0; towerIndex < towers.Count; towerIndex++)
            {
                var tower = towers[towerIndex];
                int towerX = (towerIndex + 1) * towerWidth;

                for (int diskIndex = 0; diskIndex < tower.Count; diskIndex++)
                {
                    int diskSize = tower[diskIndex];

                    if (isAnimating && towerIndex == animatingFrom && diskIndex == tower.Count - 1)
                        continue;

                    int diskWidth = maxDiskWidth * diskSize / hanoiTower.DiskCount;
                    int diskY = baseY - (diskIndex + 1) * diskHeight;

                    using (var brush = new SolidBrush(GetDiskColor(diskSize)))
                    {
                        g.FillRectangle(brush, towerX - diskWidth / 2, diskY, diskWidth, diskHeight);
                        g.DrawRectangle(Pens.Black, towerX - diskWidth / 2, diskY, diskWidth, diskHeight);
                    }

                    g.DrawString(diskSize.ToString(), new Font("Arial", 8), Brushes.White,
                        towerX - 5, diskY + 5);
                }
            }
        }

        private Color GetDiskColor(int diskSize)
        {
            return diskSize switch
            {
                1 => Color.Red,
                2 => Color.Orange,
                3 => Color.Yellow,
                4 => Color.Green,
                5 => Color.Blue,
                6 => Color.Indigo,
                7 => Color.Violet,
                8 => Color.Purple,
                _ => Color.Gray
            };
        }

        private void SetDoubleBuffered(Control control)
        {
            if (!SystemInformation.TerminalServerSession)
            {
                System.Reflection.PropertyInfo prop = typeof(Control).GetProperty(
                    "DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                prop?.SetValue(control, true, null);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                animationTimer?.Dispose();
                components?.Dispose();
                backBufferGraphics?.Dispose();
                backBuffer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}