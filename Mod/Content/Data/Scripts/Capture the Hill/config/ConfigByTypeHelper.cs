using CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.constants;

namespace CaptureTheHill.Content.Data.Scripts.Capture_the_Hill.config
{
    public class ConfigByTypeHelper
    {
        public static int GetDiscoveryRadiusByBaseType(CaptureBaseType baseType)
        {
            switch (baseType)
            {
                case CaptureBaseType.Ground:
                    return ModConfiguration.Instance.GroundBaseDiscoveryRadius;
                case CaptureBaseType.Atmosphere:
                    return ModConfiguration.Instance.AtmosphereBaseDiscoveryRadius;
                case CaptureBaseType.Space:
                    return ModConfiguration.Instance.SpaceBaseDiscoveryRadius;
                default:
                    return ModConfiguration.Instance.SpaceBaseDiscoveryRadius;
            }
        }

        public static int GetCaptureRadiusByBaseType(CaptureBaseType baseType)
        {
            switch (baseType)
            {
                case CaptureBaseType.Ground:
                    return ModConfiguration.Instance.GroundBaseCaptureRadius;
                case CaptureBaseType.Atmosphere:
                    return ModConfiguration.Instance.AtmosphereBaseCaptureRadius;
                case CaptureBaseType.Space:
                    return ModConfiguration.Instance.SpaceBaseCaptureRadius;
                default:
                    return ModConfiguration.Instance.SpaceBaseCaptureRadius;
            }
        }

        public static int GetCaptureTimeByBaseType(CaptureBaseType baseType)
        {
            switch (baseType)
            {
                case CaptureBaseType.Ground:
                    return ModConfiguration.Instance.GroundBaseCaptureTimeInSeconds;
                case CaptureBaseType.Atmosphere:
                    return ModConfiguration.Instance.AtmosphereBaseCaptureTimeInSeconds;
                case CaptureBaseType.Space:
                    return ModConfiguration.Instance.SpaceBaseCaptureTimeInSeconds;
                default:
                    return ModConfiguration.Instance.SpaceBaseCaptureTimeInSeconds;
            }
        }
    }
}