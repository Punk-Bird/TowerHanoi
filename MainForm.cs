using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace HanoiTowerSolver
{
    public class MainForm : Form
    {
        private HanoiTower hanoiTower;
        private Button initButton, solveButton, nextButton;
        private TrackBar diskTrackBar;
        private ListBox movesList;
        private Label infoLabel;
        private Panel drawingPanel;

        private System.ComponentModel.IContainer components;
        private int currentStep = 0;
        private List<string> solutionSteps;

        public MainForm()
        {
            this.components = new System.ComponentModel.Container();
            this.hanoiTower = new HanoiTower();
            this.InitializeMyComponents();
        }

        private void InitializeMyComponents()
        {
            // Настройка основной формы
            this.Text = "Ханойская башня - Рекурсивное решение";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Панель для рисования башни
            drawingPanel = new Panel();
            drawingPanel.Location = new Point(300, 50);
            drawingPanel.Size = new Size(450, 400);
            drawingPanel.BackColor = Color.White;
            drawingPanel.BorderStyle = BorderStyle.FixedSingle;
            drawingPanel.Paint += new PaintEventHandler(this.DrawingPanel_Paint);
            this.Controls.Add(drawingPanel);

            // TrackBar для выбора количества дисков
            diskTrackBar = new TrackBar();
            diskTrackBar.Location = new Point(20, 20);
            diskTrackBar.Size = new Size(200, 45);
            diskTrackBar.Minimum = 3;
            diskTrackBar.Maximum = 8;
            diskTrackBar.Value = 3;
            diskTrackBar.TickFrequency = 1;
            this.Controls.Add(diskTrackBar);

            // Label для TrackBar
            Label diskLabel = new Label();
            diskLabel.Text = "Количество дисков:";
            diskLabel.Location = new Point(20, 60);
            diskLabel.Size = new Size(150, 20);
            this.Controls.Add(diskLabel);

            // Label для значения TrackBar
            Label diskValueLabel = new Label();
            diskValueLabel.Text = diskTrackBar.Value.ToString();
            diskValueLabel.Location = new Point(230, 60);
            diskValueLabel.Size = new Size(30, 20);
            this.Controls.Add(diskValueLabel);

            diskTrackBar.Scroll += (sender, e) => {
                diskValueLabel.Text = diskTrackBar.Value.ToString();
            };

            // Кнопка инициализации
            initButton = new Button();
            initButton.Text = "Создать башню";
            initButton.Location = new Point(20, 100);
            initButton.Size = new Size(100, 30);
            initButton.Click += new EventHandler(this.InitButton_Click);
            this.Controls.Add(initButton);

            // Кнопка решения
            solveButton = new Button();
            solveButton.Text = "Найти решение";
            solveButton.Location = new Point(130, 100);
            solveButton.Size = new Size(100, 30);
            solveButton.Enabled = false;
            solveButton.Click += new EventHandler(this.SolveButton_Click);
            this.Controls.Add(solveButton);

            // Кнопка следующего шага
            nextButton = new Button();
            nextButton.Text = "Следующий ход";
            nextButton.Location = new Point(20, 140);
            nextButton.Size = new Size(100, 30);
            nextButton.Enabled = false;
            nextButton.Click += new EventHandler(this.NextButton_Click);
            this.Controls.Add(nextButton);

            // ListBox для отображения ходов
            movesList = new ListBox();
            movesList.Location = new Point(20, 180);
            movesList.Size = new Size(250, 200);
            movesList.HorizontalScrollbar = true;
            this.Controls.Add(movesList);

            // Info label
            infoLabel = new Label();
            infoLabel.Text = "Ходов: 0";
            infoLabel.Location = new Point(130, 145);
            infoLabel.Size = new Size(100, 20);
            this.Controls.Add(infoLabel);

            // Подписка на события HanoiTower
            hanoiTower.StateChanged += (state) => {
                drawingPanel.Invalidate();
            };

            hanoiTower.MoveMade += (move) => {
                movesList.Items.Add(move);
                if (movesList.Items.Count > 0)
                    movesList.SelectedIndex = movesList.Items.Count - 1;
                infoLabel.Text = $"Ход: {movesList.Items.Count}";
            };
        }

        private void InitButton_Click(object sender, EventArgs e)
        {
            hanoiTower.Initialize(diskTrackBar.Value);
            movesList.Items.Clear();
            infoLabel.Text = "Ходов: 0";
            solveButton.Enabled = true;
            nextButton.Enabled = false;
            currentStep = 0;
            solutionSteps = null;
        }

        private void SolveButton_Click(object sender, EventArgs e)
        {
            // Только генерируем решение, но не выполняем ходы
            solutionSteps = hanoiTower.GenerateSolution();
            solveButton.Enabled = false;
            nextButton.Enabled = true;
            movesList.Items.Clear();
            infoLabel.Text = $"Всего ходов: {solutionSteps.Count}";

        }

        private void NextButton_Click(object sender, EventArgs e)
        {
            if (solutionSteps != null && currentStep < solutionSteps.Count)
            {
                // Выполняем следующий ход
                hanoiTower.ExecuteMove(solutionSteps[currentStep]);
                currentStep++;

                infoLabel.Text = $"Ход {currentStep} из {solutionSteps.Count}";

                if (currentStep >= solutionSteps.Count)
                {
                    nextButton.Enabled = false;
                    MessageBox.Show("Решение завершено!", "Готово",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void DrawingPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var towers = hanoiTower.GetCurrentState();
            DrawTowers(g, towers, drawingPanel.Width, drawingPanel.Height);
        }

        private void DrawTowers(Graphics g, List<List<int>> towers, int width, int height)
        {
            int towerCount = towers.Count;
            int towerWidth = width / (towerCount + 1);
            int baseY = height - 50;
            int diskHeight = 20;
            int maxDiskWidth = towerWidth - 20;

            // Рисуем основания башен
            for (int i = 0; i < towerCount; i++)
            {
                int x = (i + 1) * towerWidth;

                // Основание башни
                g.FillRectangle(Brushes.Brown, x - 5, baseY, 10, 10);

                // Стержень башни
                g.FillRectangle(Brushes.Gray, x - 2, baseY - 200, 4, 200);

                // Подпись башни
                g.DrawString($"{(char)('A' + i)}", new Font("Arial", 12), Brushes.Black, x - 8, baseY + 15);
            }

            // Рисуем диски
            for (int towerIndex = 0; towerIndex < towers.Count; towerIndex++)
            {
                var tower = towers[towerIndex];
                int towerX = (towerIndex + 1) * towerWidth;

                for (int diskIndex = 0; diskIndex < tower.Count; diskIndex++)
                {
                    int diskSize = tower[diskIndex];
                    int diskWidth = maxDiskWidth * diskSize / hanoiTower.DiskCount;
                    int diskY = baseY - (diskIndex + 1) * diskHeight;

                    Color diskColor = GetDiskColor(diskSize);
                    using (Brush brush = new SolidBrush(diskColor))
                    {
                        g.FillRectangle(brush, towerX - diskWidth / 2, diskY, diskWidth, diskHeight);
                        g.DrawRectangle(Pens.Black, towerX - diskWidth / 2, diskY, diskWidth, diskHeight);
                    }

                    // Номер диска
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
    }
}