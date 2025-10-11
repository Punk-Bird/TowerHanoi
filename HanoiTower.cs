using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HanoiTowerSolver
{
    public class HanoiTower
    {
        private List<List<int>> towers;
        private List<string> moves;
        public int DiskCount { get; private set; }

        // Для измерения времени
        private Stopwatch stopwatch;
        public long LastSolutionTimeMs { get; private set; }

        // События для анимации
        public event Action<string> MoveMade;
        public event Action<List<List<int>>> StateChanged;
        public event Action<int, int, int> DiskMoveStarted;
        public event Action DiskMoveCompleted;

        public HanoiTower()
        {
            towers = new List<List<int>>();
            moves = new List<string>();
            stopwatch = new Stopwatch();
        }

        public void Initialize(int diskCount)
        {
            DiskCount = diskCount;
            towers.Clear();
            moves.Clear();

            for (int i = 0; i < 3; i++)
            {
                towers.Add(new List<int>());
            }

            for (int i = diskCount; i >= 1; i--)
            {
                towers[0].Add(i);
            }

            StateChanged?.Invoke(GetCurrentState());
        }

        public List<string> GenerateSolution()
        {
            moves.Clear();
            stopwatch.Restart();
            GenerateMoves(DiskCount, 0, 2, 1);
            stopwatch.Stop();
            LastSolutionTimeMs = stopwatch.ElapsedMilliseconds;
            return new List<string>(moves);
        }

        // Метод для быстрого измерения времени без генерации ходов
        public long MeasureSolutionTime(int diskCount)
        {
            stopwatch.Restart();
            CountMoves(diskCount, 0, 2, 1);
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private void GenerateMoves(int n, int from, int to, int usingRod)
        {
            if (n <= 0) return;

            GenerateMoves(n - 1, from, usingRod, to);

            string move = $"Переместить диск {n} с {RodName(from)} на {RodName(to)}";
            moves.Add(move);

            GenerateMoves(n - 1, usingRod, to, from);
        }

        // Вспомогательный метод только для подсчета ходов и времени
        private void CountMoves(int n, int from, int to, int usingRod)
        {
            if (n <= 0) return;

            CountMoves(n - 1, from, usingRod, to);
            CountMoves(n - 1, usingRod, to, from);
        }

        // Асинхронный метод для анимированного выполнения хода
        public async Task ExecuteMoveWithAnimation(string move, int animationDelay = 800)
        {
            var parts = move.Split(' ');
            int disk = int.Parse(parts[2]);
            int from = RodIndex(parts[4]);
            int to = RodIndex(parts[6]);

            if (towers[from].Count > 0 && towers[from][towers[from].Count - 1] == disk)
            {
                DiskMoveStarted?.Invoke(disk, from, to);
                await Task.Delay(animationDelay);

                towers[from].RemoveAt(towers[from].Count - 1);
                towers[to].Add(disk);

                DiskMoveCompleted?.Invoke();
                MoveMade?.Invoke(move);
                StateChanged?.Invoke(GetCurrentState());
            }
        }

        // Старый синхронный метод (для обратной совместимости)
        public void ExecuteMove(string move)
        {
            var parts = move.Split(' ');
            int disk = int.Parse(parts[2]);
            int from = RodIndex(parts[4]);
            int to = RodIndex(parts[6]);

            if (towers[from].Count > 0 && towers[from][towers[from].Count - 1] == disk)
            {
                towers[from].RemoveAt(towers[from].Count - 1);
                towers[to].Add(disk);

                MoveMade?.Invoke(move);
                StateChanged?.Invoke(GetCurrentState());
            }
        }

        private int RodIndex(string rodName)
        {
            return rodName switch
            {
                "A" => 0,
                "B" => 1,
                "C" => 2,
                _ => 0
            };
        }

        private string RodName(int rodIndex)
        {
            return rodIndex switch
            {
                0 => "A",
                1 => "B",
                2 => "C",
                _ => "?"
            };
        }

        public List<List<int>> GetCurrentState()
        {
            var copy = new List<List<int>>();
            foreach (var tower in towers)
            {
                copy.Add(new List<int>(tower));
            }
            return copy;
        }

        public int GetMoveCount()
        {
            return moves.Count;
        }
    }
}