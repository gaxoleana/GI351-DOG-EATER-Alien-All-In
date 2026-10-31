using UnityEngine;

namespace AlienAllIn
{
    public sealed class BoardGrid
    {
        public const int BoardSize = 10;
        readonly GameCharacter[,] occupants = new GameCharacter[BoardSize, BoardSize];

        public GameCharacter GetCharacterAt(Vector2Int position)
        {
            return IsInside(position) ? occupants[position.x, position.y] : null;
        }

        public bool TryPlace(GameCharacter character, Vector2Int position)
        {
            if (character == null || !IsInside(position)) return false;

            GameCharacter occupant = occupants[position.x, position.y];
            if (occupant != null && occupant != character) return false;

            if (character.IsPlaced)
                occupants[character.BoardPosition.x, character.BoardPosition.y] = null;

            occupants[position.x, position.y] = character;
            character.SetPosition(position);
            return true;
        }

        static bool IsInside(Vector2Int position)
        {
            return position.x >= 0 && position.y >= 0
                && position.x < BoardSize && position.y < BoardSize;
        }
    }
}
