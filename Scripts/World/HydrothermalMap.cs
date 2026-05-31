using System;
using Godot;
using HydroventSim.Models;

namespace HydroventSim.World
{
    public class HydrothermalMap
    {
        public int Width { get; }
        public int Height { get; }
        public float CellSize { get; }

        private float[,] _temperatureField;
        private ChemicalCompound[,] _chemicalField;
        private Vector2[] _ventPositions;

        private Random _random;
        private int _ventCount;
        private float _maxTemperature;
        private float _ambientTemperature;

        public HydrothermalMap(int width, int height, float cellSize = 10f, int seed = -1)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;
            _random = seed >= 0 ? new Random(seed) : new Random();
            _ventCount = Math.Max(3, width / 20);
            _maxTemperature = 350f;
            _ambientTemperature = 2f;

            _temperatureField = new float[width, height];
            _chemicalField = new ChemicalCompound[width, height];

            InitializeFields();
        }

        private void InitializeFields()
        {
            _ventPositions = new Vector2[_ventCount];
            for (int i = 0; i < _ventCount; i++)
            {
                _ventPositions[i] = new Vector2(
                    _random.Next(Width / 4, Width * 3 / 4),
                    _random.Next(Height / 4, Height * 3 / 4)
                );
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _temperatureField[x, y] = _ambientTemperature;
                    _chemicalField[x, y] = new ChemicalCompound();
                }
            }

            foreach (var vent in _ventPositions)
            {
                AddVentEffect((int)vent.X, (int)vent.Y);
            }
        }

        private void AddVentEffect(int centerX, int centerY)
        {
            int effectRadius = 15;

            for (int x = Math.Max(0, centerX - effectRadius); x < Math.Min(Width, centerX + effectRadius); x++)
            {
                for (int y = Math.Max(0, centerY - effectRadius); y < Math.Min(Height, centerY + effectRadius); y++)
                {
                    float distance = MathF.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                    float falloff = 1f - (distance / effectRadius);
                    falloff = MathF.Max(0f, falloff);
                    falloff = falloff * falloff;

                    _temperatureField[x, y] += (_maxTemperature - _ambientTemperature) * falloff + _ambientTemperature;

                    _chemicalField[x, y].HydrogenSulfide += 100f * falloff;
                    _chemicalField[x, y].Methane += 50f * falloff;
                    _chemicalField[x, y].Ammonia += 30f * falloff;
                    _chemicalField[x, y].Oxygen += 10f * (1f - falloff * 0.5f);
                }
            }
        }

        public float GetTemperature(int x, int y)
        {
            x = Math.Clamp(x, 0, Width - 1);
            y = Math.Clamp(y, 0, Height - 1);
            return _temperatureField[x, y];
        }

        public float GetTemperature(Vector2 position)
        {
            return GetTemperature((int)(position.X / CellSize), (int)(position.Y / CellSize));
        }

        public ChemicalCompound GetChemicals(int x, int y)
        {
            x = Math.Clamp(x, 0, Width - 1);
            y = Math.Clamp(y, 0, Height - 1);
            return _chemicalField[x, y];
        }

        public ChemicalCompound GetChemicals(Vector2 position)
        {
            return GetChemicals((int)(position.X / CellSize), (int)(position.Y / CellSize));
        }

        public float GetChemicalEnergy(int x, int y)
        {
            return GetChemicals(x, y).GetTotalEnergy();
        }

        public float GetChemicalEnergy(Vector2 position)
        {
            return GetChemicals(position).GetTotalEnergy();
        }

        public void ConsumeChemicals(int x, int y, float amount)
        {
            x = Math.Clamp(x, 0, Width - 1);
            y = Math.Clamp(y, 0, Height - 1);
            var chemicals = _chemicalField[x, y];
            chemicals.HydrogenSulfide = Math.Max(0f, chemicals.HydrogenSulfide - amount * 0.5f);
            chemicals.Methane = Math.Max(0f, chemicals.Methane - amount * 0.3f);
            chemicals.Ammonia = Math.Max(0f, chemicals.Ammonia - amount * 0.2f);
        }

        public void Update(float deltaTime)
        {
            float regenRate = 2f * deltaTime;

            foreach (var vent in _ventPositions)
            {
                int centerX = (int)vent.X;
                int centerY = (int)vent.Y;
                int effectRadius = 15;

                for (int x = Math.Max(0, centerX - effectRadius); x < Math.Min(Width, centerX + effectRadius); x++)
                {
                    for (int y = Math.Max(0, centerY - effectRadius); y < Math.Min(Height, centerY + effectRadius); y++)
                    {
                        float distance = MathF.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                        float falloff = 1f - (distance / effectRadius);
                        falloff = MathF.Max(0f, falloff);

                        _chemicalField[x, y].HydrogenSulfide += regenRate * falloff * 5f;
                        _chemicalField[x, y].Methane += regenRate * falloff * 2f;
                    }
                }
            }
        }

        public Vector2[] GetVentPositions()
        {
            return _ventPositions;
        }

        public Vector2 GetRandomHighEnergyPosition()
        {
            float maxEnergy = 0f;
            Vector2 bestPos = Vector2.Zero;

            for (int i = 0; i < 20; i++)
            {
                int x = _random.Next(Width);
                int y = _random.Next(Height);
                float energy = GetChemicalEnergy(x, y);
                if (energy > maxEnergy)
                {
                    maxEnergy = energy;
                    bestPos = new Vector2(x * CellSize, y * CellSize);
                }
            }

            return bestPos;
        }

        public void TriggerEruption(Vector2 worldCenter, float worldRadius, float intensity)
        {
            int centerCellX = (int)(worldCenter.X / CellSize);
            int centerCellY = (int)(worldCenter.Y / CellSize);
            int cellRadius = (int)(worldRadius / CellSize);

            for (int x = Math.Max(0, centerCellX - cellRadius); x < Math.Min(Width, centerCellX + cellRadius); x++)
            {
                for (int y = Math.Max(0, centerCellY - cellRadius); y < Math.Min(Height, centerCellY + cellRadius); y++)
                {
                    float distance = MathF.Sqrt((x - centerCellX) * (x - centerCellX) + (y - centerCellY) * (y - centerCellY));
                    if (distance > cellRadius) continue;

                    float falloff = 1f - (distance / cellRadius);
                    falloff = MathF.Max(0f, falloff);

                    _temperatureField[x, y] = Math.Max(_temperatureField[x, y], _maxTemperature * falloff * intensity);

                    _chemicalField[x, y].HydrogenSulfide *= (1f - falloff * intensity);
                    _chemicalField[x, y].Methane *= (1f - falloff * intensity);
                    _chemicalField[x, y].Ammonia *= (1f - falloff * intensity);
                    _chemicalField[x, y].Oxygen *= (1f - falloff * intensity * 0.5f);
                }
            }
        }

        public Vector2 GetBestHabitatPosition(Vector2 currentWorldPos, float optimalTempMin, float optimalTempMax, float chemWeight, int sampleCount = 16)
        {
            Vector2 bestPos = currentWorldPos;
            float bestScore = EvaluateHabitatAt(currentWorldPos, optimalTempMin, optimalTempMax, chemWeight);

            for (int i = 0; i < sampleCount; i++)
            {
                int x = _random.Next(Width);
                int y = _random.Next(Height);
                Vector2 candidate = new Vector2(x * CellSize, y * CellSize);

                float score = EvaluateHabitatAt(candidate, optimalTempMin, optimalTempMax, chemWeight);
                float distancePenalty = currentWorldPos.DistanceTo(candidate) / (Width * CellSize) * 0.3f;
                score -= distancePenalty;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPos = candidate;
                }
            }

            return bestPos;
        }

        public float EvaluateHabitatAt(Vector2 worldPos, float optimalTempMin, float optimalTempMax, float chemWeight)
        {
            float temp = GetTemperature(worldPos);
            float tempScore = 0f;

            if (temp >= optimalTempMin && temp <= optimalTempMax)
            {
                float mid = (optimalTempMin + optimalTempMax) * 0.5f;
                float halfRange = (optimalTempMax - optimalTempMin) * 0.5f;
                tempScore = 1f - MathF.Abs(temp - mid) / halfRange;
            }
            else if (temp < optimalTempMin)
            {
                tempScore = MathF.Max(0f, 1f - (optimalTempMin - temp) / optimalTempMin);
            }
            else
            {
                tempScore = MathF.Max(0f, 1f - (temp - optimalTempMax) / _maxTemperature);
            }

            float chemScore = MathF.Min(1f, GetChemicalEnergy(worldPos) / 500f);

            return tempScore * (1f - chemWeight) + chemScore * chemWeight;
        }
    }
}
