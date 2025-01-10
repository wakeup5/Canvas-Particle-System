using UnityEngine;

namespace Waker
{
    public partial class CanvasParticleSystem
    {
        public enum SharpType
        {
            Point,
            Circle,
            // Rectangle,
            // Edge,
        }

        public enum SharpSpawnMode
        {
            Random,
            Sequence,
        }

        [System.Serializable]
        public class SharpModule
        {
            [SerializeField] 
            private bool _enabled = true;
            
            [SerializeField] 
            private SharpType _sharpType = SharpType.Point;
            
            [SerializeField] 
            private SharpSpawnMode _sharpSpawnMode = SharpSpawnMode.Random;
            
            [SerializeField] 
            private int _sharpSpawnSpeed = 1;

            // Circle
            [SerializeField] 
            private float _circleRadius = 1f;

            // etc...
            [SerializeField] 
            private bool _randomDirection = false;

            public bool Enabled 
            {
                get => _enabled; 
                set => _enabled = value; 
            }

            public SharpType SharpType 
            { 
                get => _sharpType; 
                set => _sharpType = value; 
            }

            public SharpSpawnMode SharpSpawnMode 
            { 
                get => _sharpSpawnMode; 
                set => _sharpSpawnMode = value; 
            }

            public int SharpSpawnSpeed 
            { 
                get => _sharpSpawnSpeed; 
                set => _sharpSpawnSpeed = value; 
            }

            public float CircleRadius 
            { 
                get => _circleRadius; 
                set => _circleRadius = value; 
            }

            public bool RandomDirection 
            { 
                get => _randomDirection; 
                set => _randomDirection = value; 
            }
            
            // x, y: position, z: angle
            public Vector4 GetStartPositionAndRotation(float time)
            {
                if (!Enabled)
                {
                    return Vector4.zero;
                }

                if (SharpType == SharpType.Point)
                {
                    float directionAngle = GetDirection(Vector2.zero);
                    return new Vector4(0, 0, directionAngle, 0);
                }

                if (SharpType == SharpType.Circle)
                {
                    return GetCirclePosition(time);
                }

                return Vector4.zero;
            }

            private Vector4 GetCirclePosition(float time)
            {
                if (SharpSpawnMode == SharpSpawnMode.Random)
                {
                    Vector2 position = Random.insideUnitCircle * CircleRadius;
                    float directionAngle = GetDirection(position);

                    return new Vector4(position.x, position.y, directionAngle, 0);
                }
                else
                {
                    float angle = time * SharpSpawnSpeed * Mathf.PI * 2f;
                    float direction = GetDirection(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));

                    Vector2 position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * CircleRadius;
                    float directionAngle = GetDirection(position);

                    return new Vector4(position.x, position.y, directionAngle, direction);
                }
            }

            private float GetDirection(Vector2 position)
            {
                if (RandomDirection)
                {
                    return Random.Range(0f, 360f);
                }
                else if (position.sqrMagnitude < 0.0001f)
                {
                    return 0f;
                }
                else
                {
                    return Mathf.Atan2(position.y, position.x) * Mathf.Rad2Deg;
                }
            }
        }
    }
}
