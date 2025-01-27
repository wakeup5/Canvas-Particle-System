using UnityEngine;

namespace Waker
{
    public partial class CanvasParticleSystem
    {
        [System.Serializable]
        public class SizeOverLifeTimeModule
        {
            [SerializeField] 
            private bool _enabled;
            
            [SerializeField] 
            private AnimationCurve _curve = AnimationCurve.Linear(0, 1, 1, 1);

            public bool Enabled
            {
                get => _enabled;
                set => _enabled = value;
            }

            public AnimationCurve Curve
            {
                get => _curve;
                set => _curve = value;
            }

            public void UpdateParticle(ref Particle particle)
            {
                if (!Enabled)
                {
                    return;
                }

                particle.finalSize = particle.size * _curve.Evaluate(1f - particle.remainLifeTime / particle.lifeTime);
            }
        }
    }
}
