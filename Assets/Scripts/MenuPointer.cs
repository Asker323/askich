using UnityEngine;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Askich
{
    // IMGUI is used only for rendering. All menu input comes from Input System,
    // so Android does not depend on legacy mouse events synthesized from a touch.
    public sealed class MenuPointer
    {
        public Vector2 Position { get; private set; }
        Vector2 origin;
        bool held, released, dragged, dropped;
        public bool Held=>held;
        public bool Dragged=>dragged;
        public Vector2 Origin=>origin;
        public bool TakeDrop(){if(!dropped)return false;dropped=released=false;return true;}
        int finger = -1;

        public void Read()
        {
            released = dropped = false;
            foreach (var touch in Touch.activeTouches)
            {
                if (finger < 0 && touch.phase == TouchPhase.Began) finger = touch.touchId;
                if (touch.touchId != finger) continue;
                if (touch.phase == TouchPhase.Canceled) { Reset(); return; }
                Sample(ScreenLayout.ToUI(touch.screenPosition), touch.phase != TouchPhase.Ended);
                if (touch.phase == TouchPhase.Ended) finger = -1;
                return;
            }
            if (finger >= 0) { Reset(); return; }
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 position = ScreenLayout.ToUI(mouse.position.ReadValue());
                if(mouse.leftButton.wasPressedThisFrame && !held)Sample(position,true);
                Sample(position, mouse.leftButton.isPressed);
            }
            else Sample(Position, false);
        }

        public void Sample(Vector2 position, bool down)
        {
            released = dropped = false;
            Position = position;
            if (down && !held) { origin = position; dragged = false; }
            if (held && Vector2.Distance(origin, position) > 25) dragged = true;
            if (!down && held) {released = !dragged;dropped=true;}
            held = down;
        }

        public bool TakeClick(Rect area)
        {
            if (!released || !area.Contains(origin) || !area.Contains(Position)) return false;
            released = dropped = false;
            return true;
        }

        public float Slider(Rect area, float value)
        {
            if ((held || released) && area.Contains(origin))
                return Mathf.Clamp01((Position.x - area.x) / area.width);
            return value;
        }

        public void Reset() { held = released = dragged = dropped = false; finger = -1; }
    }
}
