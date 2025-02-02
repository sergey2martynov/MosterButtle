﻿using System;
using System.Collections.Generic;
using Enemy.Bosses;

namespace Enemy.EnemyModel
{
    public class EnemyDataHolder
    {
        private readonly List<BaseEnemyData> _enemiesData = new List<BaseEnemyData>();
        private float _coinsRewardPerEnemy;
        private int _countKilledEnemy;
        private int _totalEnemyAmount;

        public int CountKilledEnemy => _countKilledEnemy;

        public float CoinsRewardPerEnemy
        {
            get => _coinsRewardPerEnemy;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("Reward cannot be equal or less than zero");
                }

                _coinsRewardPerEnemy = value;
            }
        }

        public event Action<int> EnemyDefeated;
        public event Action AllEnemiesDefeated;
        
        public void AddEnemyData(BaseEnemyData data)
        {
            if (_enemiesData.Contains(data))
            {
                return;
            }
            
            _enemiesData.Add(data);
            _totalEnemyAmount++;
            data.EnemyDied += RemoveEnemyData;
        }

        private void RemoveEnemyData(BaseEnemyData data)
        {
            _enemiesData.Remove(data);
            _countKilledEnemy++;

            if (_countKilledEnemy == _totalEnemyAmount)
            {
                AllEnemiesDefeated?.Invoke();
            }
                
            data.EnemyDied -= RemoveEnemyData;
            OnEnemyDied();
        }

        private void OnEnemyDied()
        {
            var rewardCoins = Convert.ToInt32(_coinsRewardPerEnemy / 50);

            if (rewardCoins < 1)
                rewardCoins = 1;
            
            EnemyDefeated?.Invoke(rewardCoins);
        }
    }
}