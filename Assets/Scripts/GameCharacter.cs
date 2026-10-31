using UnityEngine;

namespace AlienAllIn
{
    public enum CharacterType
    {
        Seal,
        Pufferfish,
        Cat,
        Duck
    }

    public sealed class GameCharacter
    {
        public CharacterType Type { get; }
        public string DisplayName { get; }
        public bool IsPlaced { get; private set; }
        public Vector2Int BoardPosition { get; private set; }

        public GameCharacter(CharacterType type, string displayName)
        {
            Type = type;
            DisplayName = displayName;
        }

        internal void SetPosition(Vector2Int position)
        {
            BoardPosition = position;
            IsPlaced = true;
        }
    }
}
