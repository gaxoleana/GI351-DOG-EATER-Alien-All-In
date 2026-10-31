using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace AlienAllIn
{
    public sealed class MainSceneBootstrap : MonoBehaviour
    {
        TeamPlacementGame game;
        PlacementScreen screen;

        void Start()
        {
            game = new TeamPlacementGame();
            CreateEventSystem();
            screen = new PlacementScreen(game, transform);
        }

        static void CreateEventSystem()
        {
            var eventSystem = new GameObject("UI Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}
