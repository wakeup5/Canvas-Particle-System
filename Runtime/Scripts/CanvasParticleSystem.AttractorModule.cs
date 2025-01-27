using UnityEngine;

namespace Waker
{
    public partial class CanvasParticleSystem
    {
        [System.Serializable]
        public class AttractorModule
        {
            [SerializeField] 
            private bool _enabled;
            
            [SerializeField] 
            private RectTransform _attractor;

            [SerializeField]
            private Camera _camera;
            
            [SerializeField] 
            private AnimationCurve _attractorCurve = AnimationCurve.Linear(0, 0, 1, 1);

            private bool Enabled
            {
                get => _enabled;
                set => _enabled = value;
            }

            private RectTransform Attractor
            {
                get => _attractor;
                set => _attractor = value;
            }

            private AnimationCurve AttractorCurve
            {
                get => _attractorCurve;
                set => _attractorCurve = value;
            }

            private Vector2 _destination;

            public void UpdateParticle(ref Particle particle, RectTransform origin)
            {
                if (!Enabled)
                {
                    return;
                }

                if (Attractor == null)
                {
                    return;
                }

                UpdateDestination(origin);

                float t = AttractorCurve.Evaluate(1f - particle.remainLifeTime / particle.lifeTime);
                particle.finalPosition = ExtendedLerp(particle.position, _destination, t);
            }

            private float ExtendedLerp(float a, float b, float t)
            {
                return a + (b - a) * t;
            }

            private Vector2 ExtendedLerp(Vector2 a, Vector2 b, float t)
            {
                return a + (b - a) * t;
            }

            private void UpdateDestination(RectTransform origin)
            {
                Vector2 position = Attractor.position;

                RectTransformUtility.ScreenPointToLocalPointInRectangle(origin, position, _camera, out Vector2 localPosition);

                _destination = localPosition;
            }
        }
    }
}