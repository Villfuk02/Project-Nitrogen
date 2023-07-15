using System;

namespace Utils
{
    public static class TimingUtils
    {
        public static readonly StepType[] STEP_TYPES = (StepType[])Enum.GetValues(typeof(StepType));
        public enum StepType { None, Phase, Step, MicroStep }
    }
}
