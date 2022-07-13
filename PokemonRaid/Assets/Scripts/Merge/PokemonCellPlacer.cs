using System.Collections.Generic;
using DG.Tweening;
using InputPlayer;
using Pokemon;
using Pokemon.PokemonHolder;
using Pokemon.PokemonHolder.Cell;
using UnityEngine;

namespace Merge
{
    public class PokemonCellPlacer
    {
        private Ray _ray;
        private PokemonViewBase _targetPokemon;
        private InputView _inputView;
        private FieldView _fieldView;
        private List<CellView> _cellViews;
        private PokemonHolderModel _pokemonHolderModel;
        private CellView _fixedCell;

        public PokemonCellPlacer(InputView inputView, FieldView fieldView, PokemonHolderModel pokemonHolderModel)
        {
            _fieldView = fieldView;
            _pokemonHolderModel = pokemonHolderModel;
            _inputView = inputView;
        }

        public void Initialize()
        {
            _inputView.ButtonMouseHold += OnButtonMouseHold;
            _inputView.ButtonMousePressed += OnButtonMousePressed;
            _inputView.ButtonMouseReleased += OnButtonMouseReleased;
            _cellViews = _fieldView.GetCellViews();
        }

        private void OnButtonMousePressed()
        {
            _ray = _inputView.Camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            Physics.Raycast(_ray, out hit);

            if (hit.collider.gameObject.TryGetComponent(out PokemonViewBase pokemon))
            {
                _targetPokemon = pokemon;
                _fixedCell = GetCurrentCell(_targetPokemon.transform.position);
            }
        }

        private void OnButtonMouseHold()
        {
            if (_targetPokemon != null)
            {
                _ray = _inputView.Camera.ScreenPointToRay(Input.mousePosition);
                RaycastHit[] hits = Physics.RaycastAll(_ray, 400f);


                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider.TryGetComponent(out PlaneView plane))
                    {
                        _targetPokemon.transform.position = new Vector3(
                            Mathf.Clamp(hits[i].point.x, _inputView.LeftBorderForMerge, _inputView.RightBorderForMerge),
                            _targetPokemon.gameObject.transform.position.y,
                            Mathf.Clamp(hits[i].point.z, _inputView.DownBorderForMerge, _inputView.UpBorderForMerge));
                    }
                }
            }
        }

        private void OnButtonMouseReleased()
        {
            if (_targetPokemon != null)
            {
                var nearestCell = GetNearestEmptyCell(_targetPokemon.transform.position);

                _targetPokemon.transform.DOMoveX(nearestCell.transform.position.x, 0.2f);
                _targetPokemon.transform.DOMoveZ(nearestCell.transform.position.z, 0.2f);
                
                _targetPokemon = null;
            }
        }

        private CellView GetNearestEmptyCell(Vector3 pokemonPosition)
        {
            float distance = Vector3.Distance(pokemonPosition, _pokemonHolderModel.GetCellData(0).Position);
            float tempDistance;
            int index = 0;
            CellData cellData;

            for (int i = 1; i < _cellViews.Count; i++)
            {
                cellData = _pokemonHolderModel.GetCellData(i);
                
                tempDistance = Vector3.Distance(pokemonPosition,
                    cellData.Position);
                
                if (cellData.EmptyState && tempDistance < distance)
                {
                    index = i;
                    distance = tempDistance;
                }
            }
            
            _pokemonHolderModel.SetValueCellData(index, false);
            return _cellViews[index];
        }
        
        private CellView GetCurrentCell(Vector3 pokemonPosition)
        {
            float distance = Vector3.Distance(pokemonPosition, _pokemonHolderModel.GetCellData(0).Position);
            float tempDistance;
            int index = 0;
            CellData cellData;

            for (int i = 1; i < _cellViews.Count; i++)
            {
                cellData = _pokemonHolderModel.GetCellData(i);
                
                tempDistance = Vector3.Distance(pokemonPosition,
                    cellData.Position);
                
                if (tempDistance < distance)
                {
                    index = i;
                    distance = tempDistance;
                }
            }
            
            _pokemonHolderModel.SetValueCellData(index, true);
            return _cellViews[index];
        }


        private void Swap()
        {
            
        }
    }
}