﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DG.Tweening;
using Enemy;
using Pokemon.Animations;
using Pokemon.PokemonHolder;
using Pokemon.States;
using UnityEngine;
using UpdateHandlerFolder;

namespace Pokemon
{
    public abstract class PokemonLogicBase<TView, TEnemyView>
        where TView : PokemonViewBase
        where TEnemyView : BaseEnemyView
    {
        protected TView _view;
        protected PokemonDataBase _data;
        protected PokemonHolderModel _model;
        protected UpdateHandler _updateHandler;
        protected Dictionary<Type, BaseState<TView, TEnemyView>> _statesToType;
        protected List<TEnemyView> _enemies = new List<TEnemyView>();
        protected BaseState<TView, TEnemyView> _currentState;
        protected Collider[] _collidersInRange;
        protected Collider[] _boundsInRange = new Collider[2];
        protected BaseAnimation _attackAnimation;
        protected int _attackCount;
        protected float _rayCastDistance = 1f;
        protected RaycastHit[] _hit = new RaycastHit[1];
        protected CancellationTokenSource _source;
        protected readonly int _attack = Animator.StringToHash("Attack");

        public bool ShouldAttack { get; protected set; }

        public virtual void Initialize(TView view, PokemonDataBase data, PokemonHolderModel model,
            UpdateHandler updateHandler)
        {
            _view = view;
            _data = data;
            _model = model;
            _updateHandler = updateHandler;
            _updateHandler.UpdateTicked += Update;
            _view.ViewDestroyed += Dispose;
            _view.LevelRequested += GetPokemonLevel;
            _view.DamageTaken += OnDamageTaken;
            _view.IndexesSet += ChangeIndexes;
            _view.IndexesRequested += GetIndexes;
            _data.PokemonDied += OnPokemonDied;
            _data.HealthChanged += OnHealthChanged;
            _data.DirectionCorrectionRequested += CheckForBounds;
            _data.PositionSeted += GoToArena;
            _model.EnemyDataHolder.AllEnemiesDefeated += OnEnemyDefeated;
            _statesToType = new Dictionary<Type, BaseState<TView, TEnemyView>>
            {
                { typeof(IdleState<TView, TEnemyView>), new IdleState<TView, TEnemyView>(_view, this, _data) },
                { typeof(SpawnState<TView, TEnemyView>), new SpawnState<TView, TEnemyView>(_view, this, _data) },
                { typeof(DieState<TView, TEnemyView>), new DieState<TView, TEnemyView>(_view, this, _data) },
                {
                    typeof(MoveState<TView, TEnemyView>),
                    new MoveState<TView, TEnemyView>(_view, this, _data)
                },
            };
            _attackAnimation = _view.EventTranslator.GetAnimationInfo("Attack");
            _currentState = _statesToType[typeof(SpawnState<TView, TEnemyView>)];
            _currentState.OnEnter();
            _data.LookDirection = Vector3.forward;
        }

        public void SetMaxTargetsAmount(int amount)
        {
            _collidersInRange = new Collider[amount];
        }

        private CancellationTokenSource CreateCancellationTokenSource()
        {
            return _source = new CancellationTokenSource();
        }

        protected virtual void Update()
        {
            _currentState.Update();
            CheckForEnemies();
        }

        public T SwitchState<T>()
            where T : BaseState<TView, TEnemyView>
        {
            var type = typeof(T);

            if (_statesToType.TryGetValue(type, out var state))
            {
                _currentState.OnExit();
                _currentState = state;
                _currentState.OnEnter();
                return _currentState as T;
            }

            throw new KeyNotFoundException("There is no state of type " + type);
        }

        private int[] GetIndexes()
        {
            return _data.Indexes;
        }

        private void ChangeIndexes(int[] newIndexes)
        {
            _data.Indexes = newIndexes;
        }

        private int GetPokemonLevel()
        {
            return _data.Level;
        }

        protected virtual async void CheckForEnemies()
        {
            var collidersAmount = Physics.OverlapSphereNonAlloc(_view.Transform.position, _data.AttackRange,
                _collidersInRange, _view.EnemyLayer);

            if (Time.time < _data.AttackTime || collidersAmount == 0 || ShouldAttack)
            {
                return;
            }

            var token = _source?.Token ?? CreateCancellationTokenSource().Token;
            await Attack(_collidersInRange, token);
            //TODO: try to clear colliders in range when colliders amount == 0
        }

        protected virtual async Task Attack(Collider[] colliders, CancellationToken token)
        {
            if (!colliders[0].TryGetComponent<TEnemyView>(out var groundEnemy))
            {
                return;
            }

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
                if (collider.TryGetComponent<TEnemyView>(out var enemy))
                {
                    enemy.TakeDamage(_data.Damage, _view.PokemonType);
                }
            }

            var delay = (int)(_attackAnimation.Duration - _attackAnimation.ActionTime / _attackAnimation.FrameRate) *
                        1000;
            await Task.Delay(delay);
            _view.Animator.SetBool(_attack, false);
            _data.AttackTime = Time.time + _data.AttackSpeed;
            Array.Clear(_collidersInRange, 0, _collidersInRange.Length);
            ShouldAttack = false;
        }

        protected void OnDamageTaken(int damage)
        {
            if (damage < 0)
            {
                return;
            }

            _data.Health -= damage;
        }

        protected void OnPokemonDied()
        {
            SwitchState<DieState<TView, TEnemyView>>();
            _view.SetViewActive(false);
            Dispose();
        }

        protected void OnHealthChanged(int health, int maxHealth)
        {
            if (_data.Health < _data.MaxHealth)
                _view.HealthBarView.gameObject.SetActive(true);

            _view.SetHealth(_data.Health / (float)_data.MaxHealth);
        }

        public void RotateAt(Vector3 point)
        {
            var destinationRotation = Quaternion.LookRotation(point, Vector3.up);
            _view.Transform.rotation =
                Quaternion.RotateTowards(_view.Transform.rotation, destinationRotation, 720 * Time.deltaTime);
        }

        public Vector3 CheckForBounds()
        {
            var boundsAmount = Physics.OverlapSphereNonAlloc(_view.Transform.position, _rayCastDistance, _boundsInRange,
                _view.BoundsLayer);

            if (boundsAmount == 0)
            {
                return new Vector3(10f, 10f, 10f);
            }

            var direction = (Vector3)_data.LookDirection;
            var outDirection = (Vector3)_data.LookDirection;
            direction.Normalize();

            foreach (var boundCollider in _boundsInRange)
            {
                if (boundCollider == null)
                {
                    continue;
                }

                var position = _view.Transform.position;
                var positionDelta = boundCollider.transform.position - position;
                var ray = new Ray(position, positionDelta.normalized);

                if (Physics.RaycastNonAlloc(ray, _hit, positionDelta.magnitude, _view.BoundsLayer) > 0)
                {
                    var normal = new Vector3(
                        Mathf.Clamp(_hit[0].normal.x, -Mathf.Abs(direction.x), Mathf.Abs(direction.x)),
                        Mathf.Clamp(_hit[0].normal.y, -Mathf.Abs(direction.y), Mathf.Abs(direction.y)),
                        Mathf.Clamp(_hit[0].normal.z, -Mathf.Abs(direction.z), Mathf.Abs(direction.z)));

                    var xSign = direction.x == 0 ? 0f : Mathf.Sign(direction.x);
                    var ySign = direction.y == 0 ? 0f : Mathf.Sign(direction.y);
                    var zSign = direction.z == 0 ? 0f : Mathf.Sign(direction.z);

                    if (Vector3.Angle(normal, direction) <= 90)
                    {
                        continue;
                    }

                    outDirection -= new Vector3(Mathf.Abs(normal.x) * xSign, Mathf.Abs(normal.y) * ySign,
                        Mathf.Abs(normal.z) * zSign);
                }
            }

            Array.Clear(_boundsInRange, 0, _boundsInRange.Length);
            Array.Clear(_hit, 0, _hit.Length);
            return outDirection;
        }

        private void OnEnemyDefeated()
        {
            _view.MoveParticle.gameObject.SetActive(true);
            _view.MoveParticle.Play();
        }

        private void GoToArena(Vector3 newPosition)
        {
            _view.transform.DOMove(newPosition, 3);
        }

        protected virtual void Dispose()
        {
            _updateHandler.UpdateTicked -= Update;
            _view.ViewDestroyed -= Dispose;
            _view.LevelRequested -= GetPokemonLevel;
            _view.DamageTaken -= OnDamageTaken;
            _view.IndexesSet -= ChangeIndexes;
            _view.IndexesRequested -= GetIndexes;
            _data.PokemonDied -= OnPokemonDied;
            _data.HealthChanged -= OnHealthChanged;
            _data.DirectionCorrectionRequested -= CheckForBounds;
            _data.PositionSeted -= GoToArena;
            _model.EnemyDataHolder.AllEnemiesDefeated -= OnEnemyDefeated;

            _source?.Cancel();
            _source?.Dispose();
            _source = null;
        }
    }
}