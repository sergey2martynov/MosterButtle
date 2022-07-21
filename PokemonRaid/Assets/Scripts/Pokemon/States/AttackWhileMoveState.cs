﻿using Enemy;
using UnityEngine;

namespace Pokemon.States
{
    public class AttackWhileMoveState<TView, TEnemyView> : BaseState<TView, TEnemyView>
        where TView : PokemonViewBase
        where TEnemyView : BaseEnemyView
    {
        public AttackWhileMoveState(TView view, PokemonLogicBase<TView, TEnemyView> logic, PokemonDataBase data) : base(view, logic,
            data)
        {
        }

        public override void Update()
        {
            var moveDirection = (Vector3) _data.MoveDirection;
            var lookDirection = (Vector3) _data.LookDirection;
            _view.Transform.position += moveDirection * _data.MoveSpeed * Time.deltaTime;
            
            if (lookDirection.magnitude != 0)
            {
                _logic.RotateAt(_data.LookDirection);
            }
        }

        public override void SetNextState()
        {
            var moveDirection = (Vector3) _data.MoveDirection;
            
            if (moveDirection.magnitude != 0)
            {
                return;
            }
            
            _logic.SwitchState<IdleState<TView, TEnemyView>>();
        }
    }
}