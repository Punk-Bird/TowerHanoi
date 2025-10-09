using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HanoiTowerSolver
{
    public class MainForm : Form
    {
        private HanoiTower hanoiTower;
        private Button initButton, solveButton, nextButton, autoButton;
        private TrackBar diskTrackBar;
        private ListBox movesList;
        private Label infoLabel;
        private Panel drawingPanel;

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

        // Переменные для улучшенной анимации
        private List<PointF> animationPath;
        private int currentPathIndex = 0;
        private float animationSpeed = 3.5f; // Увеличил скорость
        private float progressToNextPoint = 0f;

        // Буфер для отрисовки
        private Bitmap backBuffer;
        private Graphics backBufferGraphics;

        public MainForm()
        {
            this.components = new System.ComponentModel.Container();
            this.hanoiTower = new HanoiTower();
            this.InitializeMyComponents();

            this.DoubleBuffered = true;
        }

        private void InitializeMyComponents()
        {
            this.Text = "Ханойская башня - Плавная анимация по стержням";
            this.Size = new Size(900, 650);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Таймер для анимации
            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 16; // Вернул 60 FPS
            animationTimer.Tick += AnimationTimer_Tick;

            // Панель для рисования башни
            drawingPanel = new Panel();
            drawingPanel.Location = new Point(300, 50);
            drawingPanel.Size = new Size(500, 450);
            drawingPanel.BackColor = Color.White;
            drawingPanel.BorderStyle = BorderStyle.FixedSingle;
            drawingPanel.Paint += new PaintEventHandler(this.DrawingPanel_Paint);

            SetDoubleBuffered(drawingPanel);
            this.Controls.Add(drawingPanel);

            InitializeBackBuffer();

            // Остальные элементы управления (без изменений)
            diskTrackBar = new TrackBar();
            diskTrackBar.Location = new Point(20, 20);
            diskTrackBar.Size = new Size(200, 45);
            diskTrackBar.Minimum = 3;
            diskTrackBar.Maximum = 8;
            diskTrackBar.Value = 3;
            diskTrackBar.TickFrequency = 1;
            this.Controls.Add(diskTrackBar);

            Label diskLabel = new Label();
            diskLabel.Text = "Количество дисков:";
            diskLabel.Location = new Point(20, 60);
            diskLabel.Size = new Size(150, 20);
            this.Controls.Add(diskLabel);

            Label diskValueLabel = new Label();
            diskValueLabel.Text = diskTrackBar.Value.ToString();
            diskValueLabel.Location = new Point(230, 60);
            diskValueLabel.Size = new Size(30, 20);
            this.Controls.Add(diskValueLabel);

            diskTrackBar.Scroll += (sender, e) => {
                diskValueLabel.Text = diskTrackBar.Value.ToString();
            };

            initButton = new Button();
            initButton.Text = "Создать башню";
            initButton.Location = new Point(20, 100);
            initButton.Size = new Size(100, 30);
            initButton.Click += new EventHandler(this.InitButton_Click);
            this.Controls.Add(initButton);

            solveButton = new Button();
            solveButton.Text = "Найти решение";
            solveButton.Location = new Point(130, 100);
            solveButton.Size = new Size(100, 30);
            solveButton.Enabled = false;
            solveButton.Click += new EventHandler(this.SolveButton_Click);
            this.Controls.Add(solveButton);

            nextButton = new Button();
            nextButton.Text = "Следующий ход";
            nextButton.Location = new Point(20, 140);
            nextButton.Size = new Size(100, 30);
            nextButton.Enabled = false;
            nextButton.Click += new EventHandler(this.NextButton_Click);
            this.Controls.Add(nextButton);

            autoButton = new Button();
            autoButton.Text = "Автопрохождение";
            autoButton.Location = new Point(130, 140);
            autoButton.Size = new Size(100, 30);
            autoButton.Enabled = false;
            autoButton.Click += new EventHandler(this.AutoButton_Click);
            this.Controls.Add(autoButton);

            movesList = new ListBox();
            movesList.Location = new Point(20, 180);
            movesList.Size = new Size(250, 250);
            movesList.HorizontalScrollbar = true;
            this.Controls.Add(movesList);

            infoLabel = new Label();
            infoLabel.Text = "Ходов: 0";
            infoLabel.Location = new Point(20, 440);
            infoLabel.Size = new Size(200, 20);
            this.Controls.Add(infoLabel);

            // Подписка на события
            hanoiTower.StateChanged += (state) => {
                RedrawScene();
            };

            hanoiTower.MoveMade += (move) => {
                movesList.Items.Add(move);
                if (movesList.Items.Count > 0)
                    movesList.SelectedIndex = movesList.Items.Count - 1;
                infoLabel.Text = $"Ход: {movesList.Items.Count}";
            };

            hanoiTower.DiskMoveStarted += (disk, from, to) => {
                StartDiskAnimation(disk, from, to);
            };

            hanoiTower.DiskMoveCompleted += () => {
                CompleteDiskAnimation();
            };

            this.SizeChanged += (s, e) => {
                InitializeBackBuffer();
                RedrawScene();
            };
        }

        private void SetDoubleBuffered(Control control)
        {
            if (SystemInformation.TerminalServerSession)
                return;

            System.Reflection.PropertyInfo prop = typeof(Control).GetProperty(
                "DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            prop?.SetValue(control, true, null);
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

        private void RedrawScene()
        {
            if (backBufferGraphics == null) return;

            backBufferGraphics.Clear(Color.White);
            DrawTowers(backBufferGraphics, drawingPanel.Width, drawingPanel.Height);

            if (isAnimating)
            {
                DrawAnimatedDisk(backBufferGraphics);
            }

            drawingPanel.Invalidate();
        }

        private void StartDiskAnimation(int disk, int from, int to)
        {
            if (isAnimating) return;

            animatingDisk = disk;
            animatingFrom = from;
            animatingTo = to;
            isAnimating = true;
            currentPathIndex = 0;
            progressToNextPoint = 0f;

            // Создаем путь анимации
            animationPath = CreateAnimationPath(from, to, disk);
            currentAnimationPos = animationPath[0];

            animationTimer.Start();
        }

        private List<PointF> CreateAnimationPath(int from, int to, int disk)
        {
            var path = new List<PointF>();
            var towers = hanoiTower.GetCurrentState();

            // 1. Текущая позиция диска
            int startDiskIndex = towers[from].Count - 1;
            PointF startPos = CalculateDiskPosition(from, startDiskIndex, disk);
            path.Add(startPos);

            // 2. Подъем до верха стержня (немного выше)
            PointF topFromPos = new PointF(startPos.X, 60f);
            path.Add(topFromPos);

            // 3. Горизонтальное движение к целевому стержню
            PointF topToPos = new PointF(CalculateDiskPosition(to, 0, disk).X, 60f);
            path.Add(topToPos);

            // 4. Спуск до конечной позиции (точно на свое место)
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
                    // Гарантируем, что диск достиг конечной позиции
                    currentAnimationPos = animationPath[animationPath.Count - 1];
                    RedrawScene();
                }
                CompleteDiskAnimation();
                return;
            }

            // Плавное движение между точками с использованием интерполяции
            PointF currentPoint = animationPath[currentPathIndex];
            PointF nextPoint = animationPath[currentPathIndex + 1];

            // Увеличиваем прогресс
            progressToNextPoint += 0.08f; // Увеличил скорость прогресса

            if (progressToNextPoint >= 1.0f)
            {
                // Переходим к следующей точке
                currentAnimationPos = nextPoint;
                currentPathIndex++;
                progressToNextPoint = 0f;

                // Если это последняя точка, завершаем анимацию
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
                // Интерполируем позицию между текущей и следующей точкой
                currentAnimationPos.X = currentPoint.X + (nextPoint.X - currentPoint.X) * progressToNextPoint;
                currentAnimationPos.Y = currentPoint.Y + (nextPoint.Y - currentPoint.Y) * progressToNextPoint;
            }

            RedrawScene();
        }

        private PointF CalculateDiskPosition(int towerIndex, int diskIndex, int diskSize)
        {
            int towerWidth = drawingPanel.Width / 4;
            int baseY = drawingPanel.Height - 50;
            int diskHeight = 20;
            int maxDiskWidth = towerWidth - 20;

            int diskWidth = maxDiskWidth * diskSize / hanoiTower.DiskCount;
            float x = (towerIndex + 1) * towerWidth;
            float y = baseY - (diskIndex + 1) * diskHeight; // Исправил расчет Y

            return new PointF(x, y);
        }

        private void InitButton_Click(object sender, EventArgs e)
        {
            CompleteDiskAnimation();
            hanoiTower.Initialize(diskTrackBar.Value);
            movesList.Items.Clear();
            infoLabel.Text = "Ходов: 0";
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
            infoLabel.Text = $"Всего ходов: {solutionSteps.Count}";
        }

        private async void NextButton_Click(object sender, EventArgs e)
        {
            if (solutionSteps != null && currentStep < solutionSteps.Count && !isAnimating)
            {
                nextButton.Enabled = false;
                autoButton.Enabled = false;

                await hanoiTower.ExecuteMoveWithAnimation(solutionSteps[currentStep], 800); // Уменьшил задержку
                currentStep++;

                infoLabel.Text = $"Ход {currentStep} из {solutionSteps.Count}";

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
                    await hanoiTower.ExecuteMoveWithAnimation(solutionSteps[currentStep], 500); // Уменьшил задержку
                    currentStep++;
                    infoLabel.Text = $"Ход {currentStep} из {solutionSteps.Count}";
                    Application.DoEvents();
                }

                if (currentStep >= solutionSteps.Count)
                {
                    MessageBox.Show("Решение завершено!", "Готово",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                initButton.Enabled = true;
                solveButton.Enabled = true;
            }
        }

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

                if (isAnimating)
                {
                    DrawAnimatedDisk(e.Graphics);
                }
            }
        }

        private void DrawAnimatedDisk(Graphics g)
        {
            int maxDiskWidth = (drawingPanel.Width / 4) - 20;
            int diskWidth = maxDiskWidth * animatingDisk / hanoiTower.DiskCount;
            int diskHeight = 20;

            Color diskColor = GetDiskColor(animatingDisk);
            using (Brush brush = new SolidBrush(diskColor))
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

            // Рисуем основания и стержни башен
            for (int i = 0; i < towerCount; i++)
            {
                int x = (i + 1) * towerWidth;

                // Основание башни
                g.FillRectangle(Brushes.Brown, x - 5, baseY, 10, 10);

                // Стержень башни (до самого верха)
                g.FillRectangle(Brushes.Gray, x - 2, 50, 4, baseY - 50);

                // Подпись башни
                g.DrawString($"{(char)('A' + i)}", new Font("Arial", 12), Brushes.Black, x - 8, baseY + 15);
            }

            // Рисуем диски
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

                    Color diskColor = GetDiskColor(diskSize);
                    using (Brush brush = new SolidBrush(diskColor))
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