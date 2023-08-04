using System;

namespace Runtime.Data.ValueObject
{
    [Serializable]
    public class InputData
    {
        public float HorizontalInputSpeed = 2f;
        public float HorizontalInputClampNegativeSide = -3f, HorizontalInputClampPositiveSide = 3f;
        public float HorizontalInputClampStopValue = 0.07f;
    }
}