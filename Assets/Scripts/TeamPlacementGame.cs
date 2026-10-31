using System.Collections.Generic;
using UnityEngine;

namespace AlienAllIn
{
    public enum GamePhase
    {
        Placement,
        Running
    }

    public sealed class TeamPlacementGame
    {
        readonly List<GameCharacter> team = new List<GameCharacter>();
        public BoardGrid Board { get; } = new BoardGrid();
        public IReadOnlyList<GameCharacter> Team => team;
        public GameCharacter SelectedCharacter { get; private set; }
        public GamePhase Phase { get; private set; } = GamePhase.Placement;
        public string StatusMessage { get; private set; } = "Choose an alien and place your team.";

        public TeamPlacementGame()
        {
            team.Add(new GameCharacter(CharacterType.Seal, "Bouncer"));
            team.Add(new GameCharacter(CharacterType.Pufferfish, "Puff Daddy"));
            team.Add(new GameCharacter(CharacterType.Cat, "Meow-Tron"));
            team.Add(new GameCharacter(CharacterType.Duck, "Quack Ops"));
            SelectedCharacter = team[0];
        }

        public void SelectCharacter(CharacterType type)
        {
            if (Phase != GamePhase.Placement) return;
            foreach (GameCharacter character in team)
            {
                if (character.Type != type) continue;
                SelectedCharacter = character;
                StatusMessage = "Place " + character.DisplayName + " on the board.";
                return;
            }
        }

        public bool TryPlaceSelectedCharacter(Vector2Int position)
        {
            if (Phase != GamePhase.Placement) return false;
            if (!Board.TryPlace(SelectedCharacter, position))
            {
                StatusMessage = "That tile is unavailable. Choose an empty tile.";
                return false;
            }

            StatusMessage = SelectedCharacter.DisplayName + " placed. Choose the next team member.";
            return true;
        }

        public bool StartShift()
        {
            foreach (GameCharacter character in team)
            {
                if (character.IsPlaced) continue;
                StatusMessage = "Place all four characters before starting the shift.";
                return false;
            }

            Phase = GamePhase.Running;
            StatusMessage = "Shift started. The team is ready.";
            return true;
        }
    }
}
