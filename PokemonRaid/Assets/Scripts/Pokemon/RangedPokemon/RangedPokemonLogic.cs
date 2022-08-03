﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Enemy;
using Pokemon.PokemonHolder;
using Pool;
using Projectile;
using UnityEngine;
using UpdateHandlerFolder;

namespace Pokemon.RangedPokemon
{
    public class RangedPokemonLogic<TView, TEnemyView> : PokemonLogicBase<TView, TEnemyView>
        where TView : RangedPokemonView
        where TEnemyView : BaseEnemyView
    {
        private ObjectPool<ProjectileViewBase> _projectilePool;

        public override void Initialize(TView view, PokemonDataBase data, PokemonHolderModel model, UpdateHandler updateHandler)
        {
            base.Initialize(view, data, model, updateHandler);
            _projectilePool = new ObjectPool<ProjectileViewBase>(20, _view.ProjectilePrefab, _view.Transform);
        }

        protected override async Task Attack(Collider[] colliders, CancellationToken token)
        {
            ShouldAttack = true;
            var attackTime = Time.time + _attackAnimation.ActionTime / _attackAnimation.FrameRate;
            _view.Animator.SetBool(_attack, true);

            while (Time.time < attackTime)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                
                if (_collidersInRange[0] != null)
                {
                    RotateAt((_collidersInRange[0].transform.position - _view.Transform.position).normalized);
                }
                
                await Task.Yield();
            }

            foreach (var collider in colliders.Where(enemy => enemy != null))
            {
                var enemy = collider.GetComponent<BaseEnemyView>();
                var projectile = _projectilePool.TryPoolObject();
                StartMovingProjectile(projectile, enemy);
            }
            
            var delay = (int) (_attackAnimation.Duration - _attackAnimation.ActionTime / _attackAnimation.FrameRate) * 1000;
            await Task.Delay(delay);
            _view.Animator.SetBool(_attack, false);
            _data.AttackTime = Time.time + _data.AttackSpeed;
            Array.Clear(_collidersInRange, 0, _collidersInRange.Length);
            ShouldAttack = false;
        }

        private async void StartMovingProjectile(ProjectileViewBase projectileView, BaseEnemyView enemyView)
        {
            var token = _source?.Token ?? new CancellationTokenSource().Token;
            await MoveProjectile(token, projectileView, enemyView);
        }

        private async Task MoveProjectile(CancellationToken token, ProjectileViewBase projectileView,
            BaseEnemyView enemyView)
        {
            var startTime = Time.time;
            var projectileViewTransform = projectileView.transform;
            projectileViewTransform.position = _view.FirePoint;
            var initialPosition = projectileViewTransform.position;
            RotateAt(enemyView.Transform, projectileViewTransform, 2);

            while (Time.time <= startTime + 0.5f)
            {
                if (token.IsCancellationRequested)
                {
                    _projectilePool.ReturnToPool(projectileView);
                    return;
                }

                if (enemyView == null)
                {
                    _projectilePool.ReturnToPool(projectileView);
                    return;
                }
                
                RotateAt(enemyView.transform, projectileViewTransform, 2 / Time.deltaTime);
                projectileViewTransform.position = Vector3.Lerp(initialPosition, enemyView.transform.position
                    + new Vector3(0f, 0.5f, 0f), (Time.time - startTime) / 0.5f);

                await Task.Yield();
            }
            
            _projectilePool.ReturnToPool(projectileView);
            enemyView.TakeDamage(_data.Damage, _view.PokemonType);
        }

        private void RotateAt(Transform point, Transform obj, float divider)
        {
            var angle = CalculateAngle(point, obj) * Mathf.PI / 180;

            if (Mathf.Abs(angle) < 0.01f)
            {
                return;
            }
            
            var rotation = new Quaternion(0f, Mathf.Sin(angle / divider), 0f, Mathf.Cos(angle / divider));
            obj.rotation *= rotation;
        }

        private float CalculateAngle(Transform point, Transform obj)
        {
            if ((point.position - obj.position).magnitude >= 0.1f)
            {
                return Vector3.SignedAngle(obj.forward, point.position - obj.position, Vector3.up);
            }

            return 0;
        }
    }
}